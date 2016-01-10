using System;
using NAudio.Wave;
using System.Net;
using System.Threading;
using System.IO;
using System.Diagnostics;

using RadioLog.AudioProcessing.Providers;

namespace RadioLog.AudioProcessing
{
    public class WaveStreamProcessor
    {
        static readonly int DEFAULT_READ_TIMEOUT = (int)TimeSpan.FromSeconds(45).TotalMilliseconds;
        static readonly int FEED_NOT_ACTIVE_SLEEP_SECS = 60;
        static readonly int LONG_FEED_NOT_ACTIVE_SLEEP_SECS = FEED_NOT_ACTIVE_SLEEP_SECS * 5;
        static readonly int LONG_FEED_NOT_ACTIVE_CNT = 5;
        static readonly long FEED_INACTIVITY_TIMEOUT_TICKS = TimeSpan.FromMinutes(3).Ticks;

        enum StreamingPlaybackState
        {
            Stopped,
            Playing,
            Buffering,
            Paused
        }

        private string _streamURL = string.Empty;
        private string _streamName = string.Empty;
        private bool _feedActive = true;
        private int _feedNotActiveCnt = 0;
        private BufferedWaveProvider bufferedWaveProvider;
        private ProcessorWaveProvider processorWaveProvider;
        private IWavePlayer waveOut;
        private volatile StreamingPlaybackState playbackState;
        private volatile bool fullyDownloaded;
        private HttpWebRequest webRequest;
        private VolumeWaveProvider16 volumeProvider;
        private Common.ProcessRadioSignalingItemDelegate _sigDelegate;
        private bool _recordingEnabled;
        private Common.SignalRecordingType _recordingType;
        private int _recordingKickTime;
        private Action<bool> _propertyChangedAction;
        private float _initialVolume = 1.0f;
        private bool _streamShouldPlay = true;
        private Common.NoiseFloor _noiseFloor = Common.NoiseFloor.Normal;
        private int _customNoiseFloor = 10;
        private bool _removeNoise = false;
        private string _streamTitle = string.Empty;
        private string _lastValidStreamTitle = string.Empty;
        private bool _decodeMDC1200 = false;
        private bool _decodeGEStar = false;
        private bool _decodeFleetSync = false;
        private bool _decodeP25 = false;
        private string _waveOutDevName = string.Empty;
        private long _feedInactivityTimeout = 0;

        public string StreamURL { get { return _streamURL; } }
        public string StreamName
        {
            get { return _streamName; }
            set
            {
                if (_streamName != value)
                {
                    _streamName = value;
                    if (processorWaveProvider != null)
                    {
                        processorWaveProvider.UpdateStreamName(value);
                    }
                }
            }
        }
        public string WaveOutDeviceName { get { return _waveOutDevName; } }

        private void SetFeedActive(bool bActive, bool bClearCnt)
        {
            _feedActive = bActive;
            if (!_feedActive)
            {
                _feedNotActiveCnt++;
            }
            else if (bClearCnt)
            {
                _feedNotActiveCnt = 0;
            }

            FirePropertyChangedAction(false);

            if (!_feedActive)
            {
                if (_feedNotActiveCnt < LONG_FEED_NOT_ACTIVE_CNT)
                    LongSmartSleep(FEED_NOT_ACTIVE_SLEEP_SECS, "Inactive Stream");
                else
                    LongSmartSleep(LONG_FEED_NOT_ACTIVE_SLEEP_SECS, "Inactive Sleep (long delay)");
            }
        }

        public WaveStreamProcessor(string streamURL, string streamName, Common.ProcessRadioSignalingItemDelegate sigDelegate, Action<bool> propertyChangedAction, float initialVolume, bool recordingEnabled, Common.SignalRecordingType recordingType, int recordingKickTime, Common.NoiseFloor noiseFloor, int customNoiseFloor, bool removeNoise, bool decodeMDC1200, bool decodeGEStar, bool decodeFleetSync, bool decodeP25, string waveOutDevName)
        {
            _streamShouldPlay = true;
            _streamURL = streamURL;
            _streamName = streamName;
            _sigDelegate = sigDelegate;
            _propertyChangedAction = propertyChangedAction;
            playbackState = StreamingPlaybackState.Stopped;
            _initialVolume = Math.Max(initialVolume, 0.1f);
            _recordingEnabled = recordingEnabled;
            _recordingType = recordingType;
            _recordingKickTime = recordingKickTime;
            _noiseFloor = noiseFloor;
            _customNoiseFloor = customNoiseFloor;
            _removeNoise = removeNoise;
            _decodeMDC1200 = decodeMDC1200;
            _decodeGEStar = decodeGEStar;
            _decodeFleetSync = decodeFleetSync;
            _decodeP25 = decodeP25;
            _waveOutDevName = waveOutDevName;
            System.Threading.Thread tWave = new Thread(WaveStreamProc);
            tWave.Name = streamName;
            tWave.IsBackground = true;
            tWave.Start();
        }
        
        protected void FirePropertyChangedAction(bool bUpdateTitle)
        {
            if (_propertyChangedAction != null)
            {
                _propertyChangedAction(bUpdateTitle);
            }
        }

        protected string GetCorrectedStreamTitle(string streamTitle)
        {
            if (string.IsNullOrWhiteSpace(streamTitle))
                return string.Empty;
            if (streamTitle.ToLower().Contains("scanning"))
                return string.Empty;
            return streamTitle.Trim();
        }
        protected void UpdateStreamTitle(string streamTitle, bool logEvent)
        {
            _streamTitle = streamTitle;
            string correctedStreamTitle = GetCorrectedStreamTitle(streamTitle);
            if (!string.IsNullOrWhiteSpace(correctedStreamTitle) && !string.Equals(correctedStreamTitle, _lastValidStreamTitle, StringComparison.InvariantCultureIgnoreCase))
            {
                _lastValidStreamTitle = correctedStreamTitle;
                if (processorWaveProvider != null)
                {
                    processorWaveProvider.LastValidStreamTitle = _lastValidStreamTitle;
                }
                if (_sigDelegate != null && logEvent)
                {
                    string curFileName = (processorWaveProvider == null) ? string.Empty : processorWaveProvider.CurrentFileName;
                    RadioLog.Common.RadioSignalingItem sigItem = new Common.RadioSignalingItem(Common.SignalingSourceType.StreamingTag, _streamName, string.Empty, Common.SignalCode.Generic, string.Empty, _lastValidStreamTitle, DateTime.Now, curFileName);
                    _sigDelegate(sigItem);
                }
            }
           //FirePropertyChangedAction(true);
        }
        protected void ClearStreamTitle()
        {
            _streamTitle = string.Empty;
            _lastValidStreamTitle = string.Empty;
            FirePropertyChangedAction(true);
        }

        public void SetRecordingEnabled(bool bEnabled)
        {
            _recordingEnabled = bEnabled;
            if (processorWaveProvider != null)
            {
                processorWaveProvider.RecordingEnabled = bEnabled;
            }
        }

        private void InternalStopStream()
        {
            if (playbackState != StreamingPlaybackState.Stopped)
            {
                if (!fullyDownloaded)
                {
                    try
                    {
                        webRequest.Abort();
                    }
                    catch { }
                }

                playbackState = StreamingPlaybackState.Stopped;
                if (waveOut != null)
                {
                    try
                    {
                        waveOut.Stop();
                        waveOut.Dispose();
                    }
                    finally
                    {
                        waveOut = null;
                    }
                }
                if (processorWaveProvider != null)
                {
                    try
                    {
                        processorWaveProvider.Dispose();
                    }
                    finally
                    {
                        processorWaveProvider = null;
                    }
                }
                FirePropertyChangedAction(false);
                //SmartSleep(1500, "InternalStopStream");
            }
        }
        public void StopStream()
        {
            _streamShouldPlay = false;
            InternalStopStream();
        }
        public bool IsStreamActive { get { return playbackState != StreamingPlaybackState.Stopped; } }
        public bool FeedActive { get { return _feedActive; } }
        public string StreamTitle { get { return _streamTitle; } }
        public string LastValidStreamTitle { get { return _lastValidStreamTitle; } }

        public bool HasSound
        {
            get
            {
                if (IsStreamActive && processorWaveProvider != null)
                {
                    return processorWaveProvider.HasSound;
                }
                return false;
            }
        }

        private bool IsIcecastStream(HttpWebResponse resp, out int icecastInterval)
        {
            icecastInterval = -1;
            if (resp == null)
                return false;
            string[] keys = resp.Headers.AllKeys;
            if (keys != null && keys.Length > 0)
            {
                foreach (string key in keys)
                {
                    if (string.Equals(key, "icy-metaint", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string[] vals = resp.Headers.GetValues(key);
                        if (vals != null && vals.Length > 0)
                        {
                            foreach (string val in vals)
                            {
                                if (int.TryParse(val, out icecastInterval))
                                    return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private void PrintHttpWebResponseHeader(HttpWebResponse resp, string codeSection, out string streamName)
        {
            streamName = string.Empty;
            if (resp == null)
                return;
            if (Common.AppSettings.Instance.DiagnosticMode)
            {
                Common.DebugHelper.WriteLine("*** HTTP Headers - {0} - {1} - START ****", codeSection, streamName);
                Common.DebugHelper.WriteLine("** ContentEncoding: {0}", resp.ContentEncoding);
                Common.DebugHelper.WriteLine("** ContentType: {0}", resp.ContentType);
                Common.DebugHelper.WriteLine("** StatusCode: {0}", resp.StatusCode);
                Common.DebugHelper.WriteLine("** StatusDescription: {0}", resp.StatusDescription);
            }
            string[] keys = resp.Headers.AllKeys;
            foreach (string key in keys)
            {
                string[] values = resp.Headers.GetValues(key);
                foreach (string value in values)
                {
                    if (string.Equals(key, "icy-name", StringComparison.InvariantCultureIgnoreCase))
                    {
                        streamName = value;
                    }
                    if (Common.AppSettings.Instance.DiagnosticMode)
                    {
                        Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Magenta, "HTTP Header: {0} = {1}", key, value);
                    }
                }
            }
            if (Common.AppSettings.Instance.DiagnosticMode)
            {
                Common.DebugHelper.WriteLine("*** HTTP Headers - {0} - END ****", codeSection);
            }
        }
        private System.Xml.XmlNode FindChildNode(System.Xml.XmlNode rootNode, string childNodeName)
        {
            if (rootNode == null||string.IsNullOrWhiteSpace(childNodeName))
                return null;
            if (string.Equals(rootNode.Name, childNodeName, StringComparison.InvariantCultureIgnoreCase))
                return rootNode;
            foreach (System.Xml.XmlNode node in rootNode.ChildNodes)
            {
                System.Xml.XmlNode n = FindChildNode(node, childNodeName);
                if (n != null)
                    return n;
            }
            return null;
        }
        private bool ValidateResponse(HttpWebResponse resp, out string newURL, out string streamName)
        {
            streamName = string.Empty;
            PrintHttpWebResponseHeader(resp, "ValidateResponse", out streamName);
            newURL = string.Empty;
            if (resp == null)
                return false;
            if (resp.Headers == null)
            {
                return false;
            }
            string[] values = resp.Headers.GetValues("Content-Type");
            if (values == null || values.Length <= 0)
                return false;
            bool bFoundText = false;
            string txtFoundType = string.Empty;
            foreach (string strVal in values)
            {
                if (string.IsNullOrWhiteSpace(strVal))
                    continue;
                if (Common.AppSettings.Instance.DiagnosticMode)
                {
                    Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Yellow, "HTTP Content-Type: {0}", strVal);
                }
                string str = strVal.ToLower().Trim();
                if (str.Contains("mpegurl") || str.Contains("x-scpls") || str.Contains("video/x-ms-asf"))
                {
                    txtFoundType = str;
                    bFoundText = true;
                    break;
                }
                if (str.Contains("audio"))
                    return true;
                if (str.Contains("text") || str.Contains("xml"))
                {
                    txtFoundType = str;
                    bFoundText = true;
                    break;
                }
            }
            if (!bFoundText)
                return false;
            try
            {
                using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                {
                    if (txtFoundType == "text/xml")
                    {
                        System.Xml.XmlDocument xDoc = new System.Xml.XmlDocument();
                        xDoc.LoadXml(sr.ReadToEnd());
                        System.Xml.XmlNodeList locList = xDoc.GetElementsByTagName("location");
                        if (locList != null && locList.Count > 0)
                        {
                            newURL = locList[0].InnerText;
                            Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Green, "Stream {0}, redirect to URL {1}", _streamName, newURL);
                            return false;
                        }
                    }
                    else if(txtFoundType=="video/x-ms-asf")
                    {
                        System.Xml.XmlDocument xDoc = new System.Xml.XmlDocument();
                        xDoc.LoadXml(sr.ReadToEnd());
                        System.Xml.XmlNode refNode=FindChildNode(xDoc.DocumentElement, "REF");
                        if (refNode != null)
                        {
                            System.Xml.XmlAttribute hrefAttrib = refNode.Attributes["HREF"];
                            if(hrefAttrib!=null&&!string.IsNullOrWhiteSpace(hrefAttrib.Value))
                            {
                                newURL = hrefAttrib.Value;
                                Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Green, "Stream {0}, redirect to URL {1}", _streamName, newURL);
                                return false;
                            }
                        }
                    }
                    else if (txtFoundType.Contains("x-scpls"))
                    {
                        while (!sr.EndOfStream)
                        {
                            string strPossible = sr.ReadLine();
                            if (!string.IsNullOrWhiteSpace(strPossible))
                            {
                                strPossible = strPossible.Trim();
                                if(!string.IsNullOrWhiteSpace(strPossible)&&strPossible.StartsWith("File")&&strPossible.IndexOf('=')>0)
                                {
                                    strPossible = strPossible.Substring(strPossible.IndexOf('=') + 1).Trim();
                                    if (!string.IsNullOrWhiteSpace(strPossible))
                                    {
                                        Uri test;
                                        if (Uri.TryCreate(strPossible, UriKind.Absolute, out test))
                                        {

                                            Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Green, "Stream {0}, redirect to URL {1}", _streamName, strPossible);
                                            newURL = strPossible;
                                            return false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        while (!sr.EndOfStream)
                        {
                            string strPossible = sr.ReadLine();
                            if (!string.IsNullOrWhiteSpace(strPossible))
                            {
                                strPossible = strPossible.Trim();
                                if (!string.IsNullOrWhiteSpace(strPossible) && !strPossible.StartsWith("#"))
                                {
                                    Uri test;
                                    if (Uri.TryCreate(strPossible, UriKind.Absolute, out test))
                                    {
                                        Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Green, "Stream {0}, redirect to URL {1}", _streamName, strPossible);
                                        newURL = strPossible;
                                        return false;
                                    }
                                }
                                Common.ConsoleHelper.ColorWriteLine(ConsoleColor.DarkRed, strPossible);
                            }
                        }
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        
        public void UpdateRecordingKickTime(Common.SignalRecordingType recordingType, int recordingKickTime)
        {
            if (processorWaveProvider != null)
            {
                processorWaveProvider.UpdateRecordingKickTime(recordingType, recordingKickTime);
            }
        }

        public void UpdateNoiseFloor(Common.NoiseFloor noiseFloor, int customNoiseFloor)
        {
            if (processorWaveProvider != null)
            {
                processorWaveProvider.UpdateNoiseFloor(noiseFloor, customNoiseFloor);
            }
        }
        public void UpdateRemoveNoise(bool bRemove)
        {
            if (processorWaveProvider != null)
            {
                processorWaveProvider.UpdateRemoveNoise(bRemove);
            }
        }
        public void UpdateDecodeMDC1200(bool decode)
        {
            _decodeMDC1200 = decode;
            if (processorWaveProvider != null)
            {
                processorWaveProvider.UpdateDecodeMDC1200(decode);
            }
        }
        public void UpdateDecodeGEStar(bool decode)
        {
            _decodeGEStar = decode;
            if (processorWaveProvider != null)
            {
                processorWaveProvider.UpdateDecodeGEStar(decode);
            }
        }
        public void UpdateDecodeFleetSync(bool decode)
        {
            _decodeFleetSync = decode;
            if (processorWaveProvider != null)
            {
                processorWaveProvider.UpdateDecodeFleetSync(decode);
            }
        }
        public void UpdateDecodeP25(bool decode)
        {
            _decodeP25 = decode;
            if (processorWaveProvider != null)
            {
                processorWaveProvider.UpdateDecodeP25(decode);
            }
        }

        private void ProcessIcyMetadata(byte[] metaBytes)
        {
            try
            {
                System.Collections.Generic.Dictionary<string, string> metaDict = IcyStream.ParseIcyMeta(metaBytes);
                if (metaDict != null && metaDict.Count > 0)
                {
                    foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in metaDict)
                    {
                        if (string.Equals(kvp.Key, "StreamTitle", StringComparison.InvariantCultureIgnoreCase))
                        {
                            UpdateStreamTitle(kvp.Value, true);
                        }
                    }
                    if (Common.AppSettings.Instance.DiagnosticMode)
                    {
                        if (metaDict.Count > 1)
                        {
                            Common.DebugHelper.WriteLine("ICY-META MULTI-PACKET: {0}: {1}", StreamName, System.Text.Encoding.ASCII.GetString(metaBytes));
                        }
                        foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in metaDict)
                        {
                            Common.DebugHelper.WriteLine("ICY-META: {0}: {1} = {2}", StreamName, kvp.Key, kvp.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.DebugHelper.WriteExceptionToLog("WaveStreamProcessor.ProcessIcyMetadata", ex, false);
            }
        }

        private void LongSmartSleep(int sleepSec, string sleepRegion = null)
        {
            if (!string.IsNullOrWhiteSpace(sleepRegion))
            {
                Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Cyan, "WaveStreamProcessor:SmartSleep [{0}] {1}", _streamName, sleepRegion);
            }
            int iCnt = sleepSec;
            while (iCnt > 0 && _streamShouldPlay)
            {
                System.Threading.Thread.Sleep(1000);
                iCnt--;
            }
        }
        private void SmartSleep(int sleepMs, string sleepRegion = null)
        {
            if (!string.IsNullOrWhiteSpace(sleepRegion))
            {
                Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Cyan, "WaveStreamProcessor:SmartSleep [{0}] {1}", _streamName, sleepRegion);
            }
            int iCnt = (int)(sleepMs / 50);
            while (iCnt > 0 && _streamShouldPlay)
            {
                System.Threading.Thread.Sleep(50);
                iCnt--;
            }
        }

        private HttpWebResponse SmartGetWebResponse(WebRequest req)
        {
            try
            {
                if (req == null)
                    return null;

                IAsyncResult arGet = req.BeginGetResponse(null, null);
                if (!arGet.AsyncWaitHandle.WaitOne(DEFAULT_READ_TIMEOUT))
                {
                    return null;
                }
                return req.EndGetResponse(arGet) as HttpWebResponse;
            }
            catch (Exception ex)
            {
                bool bShouldLog = true;
                
                WebException exw = ex as WebException;
                if (exw != null)
                {
                    if (exw.Status == WebExceptionStatus.RequestCanceled)
                    {
                        bShouldLog = false;
                    }
                    else if (exw.Status == WebExceptionStatus.ProtocolError)
                    {
                        try
                        {
                            System.Net.HttpWebResponse httpRes = exw.Response as System.Net.HttpWebResponse;
                            if (httpRes != null && httpRes.StatusCode == HttpStatusCode.NotFound)
                            {
                                SetFeedActive(false, false);
                                FirePropertyChangedAction(false);
                                LongSmartSleep(FEED_NOT_ACTIVE_SLEEP_SECS, "SmartGetWebResponse - Inactive Stream (404)"); // sleeping for 15 minutes on this stream!
                            }
                        }
                        catch { }
                    }
                }
                if (bShouldLog)
                {
                    Common.DebugHelper.WriteExceptionToLog("WaveStreamProcessor.SmartGetWebResponse", ex, false, _streamURL);
                }
                return null;
            }
        }

        public static void SetupWebRequestSettings(HttpWebRequest req)
        {
            try
            {
                if (req == null)
                    return;
                req.KeepAlive = false;
                req.UserAgent = "RadioLog";
                req.ReadWriteTimeout = DEFAULT_READ_TIMEOUT;
                if (req.ServicePoint != null)
                {
                    req.ServicePoint.ConnectionLimit = 1000;
                    req.ServicePoint.ConnectionLeaseTimeout = 5000;
                    req.ServicePoint.MaxIdleTime = 5000;
                }
            }
            catch (Exception ex)
            {
                Common.DebugHelper.WriteExceptionToLog("SetupWebRequestSettings", ex, true);
            }
        }

        private void ResetFeedInactivityTimeout() { _feedInactivityTimeout = 0; }
        private void KickFeedInactivityTimeout() { _feedInactivityTimeout = DateTime.Now.Ticks + FEED_INACTIVITY_TIMEOUT_TICKS; }
        private bool IsFeedInactive() { if (_feedInactivityTimeout == 0)return false; else return _feedInactivityTimeout < DateTime.Now.Ticks; }

        private void WaveStreamProc()
        {
            string icyStreamName = string.Empty;
            while (_streamShouldPlay)
            {
                ResetFeedInactivityTimeout();
                SetFeedActive(true, false);
                System.Threading.Thread.Sleep(new System.Random().Next(100, 1500));
                try
                {
                    Common.ConsoleHelper.ColorWriteLine(ConsoleColor.DarkCyan, "Initiating connection to {0}", _streamName);
                    playbackState = StreamingPlaybackState.Buffering;
                    FirePropertyChangedAction(false);
                    bufferedWaveProvider = null;
                    fullyDownloaded = false;
                    webRequest = (HttpWebRequest)WebRequest.Create(Common.UrlHelper.GetCorrectedStreamURL(_streamURL));
                    //webRequest.Timeout = webRequest.Timeout * 20;
                    /*test - start*/
                    webRequest.Headers.Clear();
                    webRequest.Headers.Add("Icy-MetaData", "1");
                    SetupWebRequestSettings(webRequest);
                    /*test - end*/
                    HttpWebResponse resp;
                    try
                    {
                        resp = SmartGetWebResponse(webRequest);// (HttpWebResponse)webRequest.GetResponse();
                        string newUrl = string.Empty;
                        if (!ValidateResponse(resp, out newUrl, out icyStreamName))
                        {
                            if (!string.IsNullOrWhiteSpace(newUrl))
                            {
                                webRequest = (HttpWebRequest)WebRequest.Create(newUrl);
                                //webRequest.Timeout = webRequest.Timeout * 20;
                                /*test - start*/
                                webRequest.Headers.Clear();
                                webRequest.Headers.Add("Icy-MetaData", "1");
                                SetupWebRequestSettings(webRequest);
                                /*test - end*/
                                resp = SmartGetWebResponse(webRequest);// (HttpWebResponse)webRequest.GetResponse();
                                PrintHttpWebResponseHeader(resp, "WaveStreamProc.Redirected", out icyStreamName);
                            }
                        }
                    }
                    catch (WebException e)
                    {
                        if (e.Status == WebExceptionStatus.ProtocolError)
                        {
                            Common.DebugHelper.WriteExceptionToLog("WaveStreamProcessor.WebRequest.GetResponse", e, false, _streamURL);

                            try
                            {
                                System.Net.HttpWebResponse httpRes = e.Response as System.Net.HttpWebResponse;
                                if (httpRes != null && httpRes.StatusCode == HttpStatusCode.NotFound)
                                {
                                    SetFeedActive(false, false);
                                }
                            }
                            catch { }
                        }
                        if (e.Status != WebExceptionStatus.RequestCanceled)
                        {
                            //ConsoleHelper.ColorWriteLine(ConsoleColor.Red, "WaveStreamProcessor Error: {0}", e.Message);
                            //Common.DebugHelper.WriteExceptionToLog("WaveStreamProcessor Error", e, false);
                            InternalStopStream();
                            ClearStreamTitle();

                            SmartSleep(30 * 1000, string.Format("WebRequest.GetResponse: {0}", e.Status));
                        }
                        continue;
                    }
                    var buffer = new byte[16348 * 4];
                    IMp3FrameDecompressor decompressor = null;
                    try
                    {
                        int metaInt = -1;
                        bool bIsIcy = IsIcecastStream(resp, out metaInt);
                        if (!bIsIcy)
                        {
                            metaInt = -1;
                        }
                        using (var responseStream = resp.GetResponseStream())
                        {
                            Stream readFullyStream = null;
                            if (!string.IsNullOrWhiteSpace(icyStreamName))
                            {
                                UpdateStreamTitle(icyStreamName, false);
                                FirePropertyChangedAction(true);
                            }
                            if (bIsIcy && metaInt > 0)
                            {
                                readFullyStream = new ReadFullyStream(new IcyStream(responseStream, metaInt, ProcessIcyMetadata));
                            }
                            else
                            {
                                readFullyStream = new ReadFullyStream(responseStream);
                            }
                            do
                            {
                                if (IsBufferNearlyFull)
                                {
                                    SmartSleep(500);
                                    //ConsoleHelper.ColorWriteLine(ConsoleColor.DarkCyan, "Buffer is getting full, taking a break, {0}sec...", bufferedWaveProvider.BufferedDuration.TotalSeconds);
                                    //ConsoleHelper.ColorWriteLine(ConsoleColor.DarkCyan, "  {0}", _streamURL);
                                }
                                else
                                {
                                    Mp3Frame frame;
                                    try
                                    {
                                        frame = Mp3Frame.LoadFromStream(readFullyStream);
                                    }
                                    catch (EndOfStreamException eose)
                                    {
                                        fullyDownloaded = true;
                                        SmartSleep(1500, "EndOfStreamException");
                                        if (playbackState != StreamingPlaybackState.Stopped)
                                        {
                                            Common.DebugHelper.WriteExceptionToLog("Mp3Frame.LoadFromStream", eose, false, _streamURL);
                                        }
                                        continue;
                                    }
                                    catch (WebException wex)
                                    {
                                        InternalStopStream();
                                        SmartSleep(3 * 60 * 1000, "WebException");
                                        if (playbackState != StreamingPlaybackState.Stopped)
                                        {
                                            Common.DebugHelper.WriteExceptionToLog("Mp3Frame.LoadFromStream", wex, false, _streamURL);
                                        }
                                        continue;
                                    }
                                    if (frame != null)
                                    {
                                        KickFeedInactivityTimeout();
                                        SetFeedActive(true, true);
                                        if (decompressor == null)
                                        {
                                            try
                                            {
                                                Common.ConsoleHelper.ColorWriteLine(ConsoleColor.DarkGray, "Creating MP3 Decompressor for {0}...", _streamURL);
                                                decompressor = CreateFrameDecompressor(frame);
                                                bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
                                                bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(20);

                                                processorWaveProvider = new ProcessorWaveProvider(_streamName, bufferedWaveProvider, _sigDelegate, _propertyChangedAction, _recordingEnabled, _recordingType, _recordingKickTime, _noiseFloor, _customNoiseFloor, _removeNoise, _decodeMDC1200, _decodeGEStar, _decodeFleetSync, _decodeP25);

                                                //volumeProvider = new VolumeWaveProvider16(bufferedWaveProvider);
                                                volumeProvider = new VolumeWaveProvider16(processorWaveProvider);
                                                volumeProvider.Volume = _initialVolume;
                                                waveOut = CreateWaveOut();
                                                waveOut.Init(volumeProvider);
                                                FirePropertyChangedAction(false);
                                            }
                                            catch (Exception ex)
                                            {
                                                Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Red, "Excpetion in stream {0}: {1}", _streamURL, ex.Message);
                                                Common.DebugHelper.WriteExceptionToLog("WaveStreamProcessor", ex, false, string.Format("Exception in stream {0}", _streamURL));
                                                InternalStopStream();
                                                SmartSleep(3 * 60 * 1000, "Exception:CreateFrameDecompressor");
                                                continue;
                                            }
                                        }
                                        int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                                        bufferedWaveProvider.AddSamples(buffer, 0, decompressed);

                                        var bufferedSeconds = bufferedWaveProvider.BufferedDuration.TotalSeconds;
                                        if (bufferedSeconds < 0.5 && playbackState == StreamingPlaybackState.Playing && !fullyDownloaded)
                                        {
                                            playbackState = StreamingPlaybackState.Buffering;
                                            FirePropertyChangedAction(false);
                                            waveOut.Pause();
                                            Common.ConsoleHelper.ColorWriteLine(ConsoleColor.DarkRed, "Stream Paused... Buffering... {0}", _streamURL);
                                        }
                                        else if (bufferedSeconds > 4 && playbackState == StreamingPlaybackState.Buffering)
                                        {
                                            waveOut.Play();
                                            playbackState = StreamingPlaybackState.Playing;
                                            FirePropertyChangedAction(false);
                                            Common.ConsoleHelper.ColorWriteLine(ConsoleColor.DarkGreen, "Stream Playing... {0}", _streamURL);
                                        }
                                    }
                                    else
                                    {
                                        if(IsFeedInactive())
                                        {
                                            try
                                            {
                                                Common.ConsoleHelper.ColorWriteLine(ConsoleColor.DarkYellow, "Restarting {0} due to inactivity...", _streamName);
                                                InternalStopStream();
                                            }
                                            finally
                                            {
                                                LongSmartSleep(FEED_NOT_ACTIVE_SLEEP_SECS, "WaveStreamProc.IsFeedInactive");
                                            }
                                        }
                                    }
                                }
                            }
                            while (playbackState != StreamingPlaybackState.Stopped);
                            if (decompressor != null)
                            {
                                try
                                {
                                    decompressor.Dispose();
                                }
                                finally
                                {
                                    decompressor = null;
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (decompressor != null)
                        {
                            try
                            {
                                decompressor.Dispose();
                            }
                            finally
                            {
                                decompressor = null;
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
#if DEBUG
                    Common.DebugHelper.WriteExceptionToLog("WaveStreamProc", ex, false, "General Catch");
#endif
                    try
                    {
                        InternalStopStream();
                    }
                    finally
                    {
                        LongSmartSleep(15, "WaveStreamProc");
                    }
                }
            }
        }

        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                frame.FrameLength, frame.BitRate);
            return new AcmMp3FrameDecompressor(waveFormat);
        }

        private IWavePlayer CreateWaveOut()
        {
            if (!string.IsNullOrWhiteSpace(_waveOutDevName))
            {
                for (int iDev = 0; iDev < WaveOut.DeviceCount; iDev++)
                {
                    WaveOutCapabilities cap = WaveOut.GetCapabilities(iDev);
                    if (string.Equals(cap.ProductName, _waveOutDevName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        WaveOut rslt = new WaveOut();
                        rslt.DeviceNumber = iDev;
                        return rslt;
                    }
                }
            }
            return new WaveOut();
        }

        private bool IsBufferNearlyFull
        {
            get
            {
                return bufferedWaveProvider != null &&
                       bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes
                       < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4;
            }
        }

        public float CurrentVolume
        {
            get
            {
                if (volumeProvider != null)
                {
                    return volumeProvider.Volume;
                }
                else
                {
                    return _initialVolume;
                }
            }
            set
            {
                if (volumeProvider == null)
                    return;
                if (value > 1F)
                    value = 1F;
                if (value < 0F)
                    value = 0F;
                _initialVolume = value;
                if (volumeProvider.Volume != value)
                {
                    volumeProvider.Volume = value;
                }
            }
        }
    }
}

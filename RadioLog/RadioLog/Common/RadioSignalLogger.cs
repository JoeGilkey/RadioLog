using System;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;

namespace RadioLog.Common
{
    public class RadioSignalLogger
    {
        static readonly int MAX_LOG_FILE_SIZE = 1024 * 1024 * 50; //50mb

        private string _sourceName;
        private string _logDirectory;
        private string _logFilePrefix;
        private System.Threading.Thread _tLog = null;
        private ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private readonly object _logLock = new object();
        private bool _shouldRun = true;
        private bool _needsHeader = true;
        private static bool _shouldDoConsoleOutput = false;
        private static bool _shouldIncludeLatLon = false;

        static RadioSignalLogger()
        {
            _shouldIncludeLatLon = AppSettings.Instance.EnableGPS;
        }

        private string _logFileName = string.Empty;
        private string LogFileName
        {
            get
            {
                if (string.IsNullOrEmpty(_logFileName))
                {
                    _logFileName = System.IO.Path.Combine(_logDirectory, string.Format("{0}_{1}.txt", _logFilePrefix, DateTime.Now.ToString("yyyyMMdd_HHmmss")));
                    if (!System.IO.File.Exists(_logFileName))
                    {
                        _needsHeader = true;
                    }
                }
                return _logFileName;
            }
        }
        private void KickFileHeader()
        {
            string tmp = LogFileName;
            if (_needsHeader)
            {
                WriteLogHeader(tmp);
            }
        }

        public static void SetShouldDoConsoleOutput(bool bOn) { _shouldDoConsoleOutput = bOn; }
        public static void SetShouldIncludeLatLon(bool bOn) { _shouldIncludeLatLon = bOn; }

        public RadioSignalLogger(string sourceName, string logDirectory)
        {
            this._sourceName = sourceName;
            this._logDirectory = logDirectory;
            this._logFilePrefix = MakeSourceFilePrefix(_sourceName);

            CheckLogFileSize();
            _tLog = new System.Threading.Thread(() =>
            {
                _shouldRun = true;
                while (_shouldRun)
                {
                    int iCnt = _logQueue.Count;
                    if (iCnt > 0)
                    {
                        KickFileHeader();

                        System.Text.StringBuilder sb = new StringBuilder();
                        string str = string.Empty;
                        for (int i = 0; i < iCnt; i++)
                        {
                            if (_logQueue.TryDequeue(out str))
                            {
                                sb.AppendLine(str);
                            }
                        }
                        lock (_logLock)
                        {
                            System.IO.File.AppendAllText(LogFileName, sb.ToString());
                        }
                    }
                    System.Threading.Thread.Sleep(1500);
                }
            });

            _tLog.IsBackground = true;

            _tLog.Start();
        }
        ~RadioSignalLogger()
        {
            _shouldRun = false;
            if (_tLog != null && _tLog.ThreadState== System.Threading.ThreadState.Running)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }
        public void CheckLogFileSize()
        {
            try
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(LogFileName);
                if (fi.Exists && fi.Length > MAX_LOG_FILE_SIZE)
                {
                    _logFileName = string.Empty;
                }
            }
            catch { }
        }

        public static string MakeSourceFilePrefix(string sourceName)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                return "LOG_";
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            string rslt = sourceName.Trim().Replace(' ', '_');
            foreach (char c in invalidChars)
            {
                rslt = rslt.Replace(c, '_');
            }
            invalidChars = System.IO.Path.GetInvalidPathChars();
            foreach (char c in invalidChars)
            {
                rslt = rslt.Replace(c, '_');
            }
            rslt = rslt.Replace(System.IO.Path.DirectorySeparatorChar, '_');
            rslt = rslt.Replace(System.IO.Path.AltDirectorySeparatorChar, '_');
            rslt = rslt.Replace(System.IO.Path.PathSeparator, '_');
            rslt = rslt.Replace(System.IO.Path.VolumeSeparatorChar, '_');
            rslt = rslt.Replace("__", "_").Replace("__", "_");
            return rslt;
        }

        private void WriteLogHeader(string logFileName)
        {
            string strHdr = "Timestamp|Source Type|Source Name|Signal Code Type|Signaling Format|Unit Id|Agency Name|Unit Name|Radio Name|Radio Type|Personnel Name|Role Name|Description|Recording File";
            if (_shouldIncludeLatLon)
            {
                strHdr += "|Latitude|Longitude";
            }
            strHdr += Environment.NewLine;
            System.IO.File.AppendAllText(logFileName, strHdr);
            _needsHeader = false;
        }

        public void LogRadioSignal(RadioSignalingItem signalItem)
        {
            if (signalItem == null)
                return;
            string outLine = string.Format("{0} - {1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}|{13}|{14}", signalItem.Timestamp.ToShortDateString(), signalItem.Timestamp.ToLongTimeString(), DisplayFormatterUtils.SignalingSourceTypeToDisplayStr(signalItem.SourceType), signalItem.SourceName, DisplayFormatterUtils.SignalCodeToDisplayStr(signalItem.Code), signalItem.SignalingFormat, signalItem.UnitId, signalItem.AgencyName, signalItem.UnitName, signalItem.RadioName, DisplayFormatterUtils.RadioTypeCodeToDisplayStr(signalItem.RadioType), signalItem.AssignedPersonnel, signalItem.AssignedRole, signalItem.Description, signalItem.RecordingFileName);
            if (_shouldIncludeLatLon)
            {
                outLine += string.Format("|{0}|{1}", signalItem.Latitude, signalItem.Longitude);
            }
            if (_shouldDoConsoleOutput)
            {
                Console.WriteLine(outLine);
            }
            _logQueue.Enqueue(outLine);
        }
    }
}

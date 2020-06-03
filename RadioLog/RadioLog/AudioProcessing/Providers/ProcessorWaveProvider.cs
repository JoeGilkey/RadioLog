using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.Wave.Compression;

namespace RadioLog.AudioProcessing.Providers
{
    public class ProcessorWaveProvider : SavingWaveProvider
    {
        Decoders.MDC1200 _mdc;
        Decoders.STAR _star;
        Decoders.RootDecoder _rootDecoder;
        AcmStream _resampleStream;
        WaveFormatEncoding _encoding;
        bool _useResampler;
        string _sourceName = string.Empty;
        int _bytesPerSample = 2;
        WaveFormat _sourceFormat;
        WaveFormat _outFormat;
        SilenceHelper _silenceHelper;
        bool _hasAudio = false;
        AudioRecorder _recorder;
        Action<bool> _hasPropertyChanged = null;
        Common.ProcessRadioSignalingItemDelegate _sigDelegate;

        public ProcessorWaveProvider(string sourceName, IWaveProvider sourceWaveProvider, Common.ProcessRadioSignalingItemDelegate sigDelegate, Action<bool> hasPropertyChanged, bool recordEnabled, Common.SignalRecordingType recordType, int recordKickTime, Common.NoiseFloor noiseFloor, int customNoiseFloor, bool removeNoise, bool decodeMDC1200, bool decodeGEStar, bool decodeFleetSync, bool decodeP25) : this(sourceName, sourceWaveProvider, string.Empty, AudioProcessingGlobals.DefaultProcessingWaveFormat, sigDelegate, hasPropertyChanged, recordEnabled, recordType, recordKickTime, noiseFloor, customNoiseFloor, removeNoise, decodeMDC1200, decodeGEStar, decodeFleetSync, decodeP25) { }
        public ProcessorWaveProvider(string sourceName, IWaveProvider sourceWaveProvider, string waveFilePath, WaveFormat outFormat, Common.ProcessRadioSignalingItemDelegate sigDelegate, Action<bool> hasPropertyChanged, bool recordEnabled, Common.SignalRecordingType recordType, int recordKickTime, Common.NoiseFloor noiseFloor, int customNoiseFloor,bool removeNoise, bool decodeMDC1200, bool decodeGEStar, bool decodeFleetSync, bool decodeP25)
            : base(sourceWaveProvider, waveFilePath)
        {
            LastValidStreamTitle = string.Empty;
            _sourceName = sourceName;
            _sourceFormat = sourceWaveProvider.WaveFormat;
            _outFormat = outFormat;
            _hasPropertyChanged = hasPropertyChanged;

            _silenceHelper = new SilenceHelper(outFormat.AverageBytesPerSecond / (outFormat.BitsPerSample / 8), noiseFloor, removeNoise, customNoiseFloor);

            if (outFormat.Equals(sourceWaveProvider.WaveFormat))
            {
                _resampleStream = null;
                _useResampler = false;
            }
            else
            {
                if (Common.AppSettings.Instance.DiagnosticMode)
                {
                    Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Magenta, "{0}: Source Format <> Out Format [{1}] <> [{2}]", sourceName, sourceWaveProvider.WaveFormat, outFormat);
                }
                _resampleStream = new NAudio.Wave.Compression.AcmStream(sourceWaveProvider.WaveFormat, outFormat);
                _useResampler = true;
            }
            if (decodeMDC1200)
            {
                _mdc = new Decoders.MDC1200(outFormat.SampleRate, ProcessMDC1200, sourceName);
            }
            else
            {
                _mdc = null;
            }
            if (decodeGEStar)
            {
                _star = new Decoders.STAR(outFormat.SampleRate, ProcessSTAR, Decoders.STAR.star_format.star_format_1_16383, sourceName);
            }
            else
            {
                _star = null;
            }
            _rootDecoder = new Decoders.RootDecoder(outFormat.SampleRate, decodeFleetSync, decodeP25, ProcessRootDecoder);

            _recorder = new AudioRecorder(sourceName, recordType, recordKickTime, outFormat, AudioProcessingGlobals.DefaultSaveFileWaveFormat, recordEnabled);
            _bytesPerSample = outFormat.BitsPerSample / 8;
            _encoding = outFormat.Encoding;
            _sigDelegate = sigDelegate;
        }

        public void UpdateDecodeMDC1200(bool decode)
        {
            if (decode)
            {
                if (_mdc == null)
                {
                    _mdc = new Decoders.MDC1200(_outFormat.SampleRate, ProcessMDC1200, _sourceName);
                }
            }
            else
            {
                _mdc = null;
            }
        }
        public void UpdateDecodeGEStar(bool decode)
        {
            if (decode)
            {
                if (_star == null)
                {
                    _star = new Decoders.STAR(_outFormat.SampleRate, ProcessSTAR, Decoders.STAR.star_format.star_format_1_16383, _sourceName);
                }
            }
            else
            {
                _star = null;
            }
        }
        public void UpdateDecodeFleetSync(bool decode)
        {
            if (_rootDecoder != null)
            {
                _rootDecoder.UpdateDecodeFleetSync(decode);
            }
        }
        public void UpdateDecodeP25(bool decode)
        {
            if (_rootDecoder != null)
            {
                _rootDecoder.UpdateDecodeP25(decode);
            }
        }

        public void UpdateStreamName(string sourceName)
        {
            _sourceName = sourceName;
            if (_recorder != null)
            {
                _recorder.RecordingPrefix = Common.RadioSignalLogger.MakeSourceFilePrefix(sourceName);
            }
        }

        public string LastValidStreamTitle { get; set; }

        public void UpdateRecordingKickTime(Common.SignalRecordingType recordingType, int recordingKickTime)
        {
            if (_recorder != null)
            {
                _recorder.UpdateRecordingKickTime(recordingType, recordingKickTime);
            }
        }

        public void UpdateNoiseFloor(Common.NoiseFloor noisefloor, int customNoiseFloor)
        {
            if (_silenceHelper != null)
            {
                _silenceHelper.UpdateNoiseFloor(noisefloor, customNoiseFloor);
            }
        }
        public void UpdateRemoveNoise(bool bRemove)
        {
            if (_silenceHelper != null)
            {
                _silenceHelper.UpdateRemoveNoise(bRemove);
            }
        }

        private void FirePropertyChanged(bool bUpdateTitle)
        {
            if(_hasPropertyChanged!=null)
            {
                _hasPropertyChanged(bUpdateTitle);
            }
        }


        public bool RecordingEnabled { get { return _recorder.RecordingEnabled; } set { _recorder.RecordingEnabled = value; } }

        protected override void ProcessSamples(byte[] buffer, int offset, int count)
        {
            float[] samples = null;
            if (_silenceHelper != null)
            {
                samples = _silenceHelper.PreProcessSamplesToFloat(buffer, offset, count, _sourceFormat.BitsPerSample / 8, _sourceFormat.Encoding== WaveFormatEncoding.IeeeFloat);
            }

            base.ProcessSamples(buffer, offset, count);
            
            byte[] sampleBytes = null;

            if (_useResampler)
            {
                Buffer.BlockCopy(buffer, offset, _resampleStream.SourceBuffer, 0, count);
                int sourceBytesConverted = 0;
                int convertedBytes = _resampleStream.Convert(count, out sourceBytesConverted);
                if (sourceBytesConverted == count)
                {
                    byte[] convBytes = new byte[convertedBytes];
                    Buffer.BlockCopy(_resampleStream.DestBuffer, 0, convBytes, 0, convertedBytes);
                    sampleBytes = convBytes;
                }
                samples = null;
            }
            else
            {
                byte[] tmpBuffer;
                if (offset == 0)
                {
                    tmpBuffer = buffer;
                }
                else
                {
                    tmpBuffer = new byte[count];
                    Array.Copy(buffer, offset, tmpBuffer, 0, count);
                }
                sampleBytes = tmpBuffer;
            }

            bool bHasSound = _hasAudio;
            if (sampleBytes != null)
            {
                if (samples == null || samples.Length <= 0)
                {
                    samples = BytesToFloatSamples(sampleBytes);
                }
                if (samples != null && samples.Length > 0)
                {
                    bHasSound = _silenceHelper.HasSound;
                    _recorder.ProcessSamples(bHasSound, sampleBytes);
                    if (_mdc != null)
                    {
                        _mdc.ProcessSamples(samples, samples.Length, bHasSound);
                    }
                    if (_star != null)
                    {
                        _star.ProcessSamples(samples, samples.Length, bHasSound);
                    }
                    if (_rootDecoder != null)
                    {
                        _rootDecoder.ProcessSamples(samples, samples.Length, bHasSound);
                    }
                }
            }
            if (bHasSound != _hasAudio)
            {
                _hasAudio = bHasSound;
                FirePropertyChanged(false);
            }
        }

        private float[] BytesToFloatSamples(byte[] bytes)
        {
            return AudioProcessingGlobals.BytesToFloatSamples(bytes, bytes.Length, _bytesPerSample, _encoding);
        }

        protected override void DisposeProcessors()
        {
            base.DisposeProcessors();
            _mdc = null;
            _star = null;
            _rootDecoder = null;
        }

        private void ProcessRootDecoder(RadioLog.Common.SignalCode sigCode, string format, string unitId, string desc)
        {
            Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Magenta, "{0}: {1} on stream {2}", format, sigCode, _sourceName);

            _silenceHelper.ClearSilenceStats();

            if (_sigDelegate != null)
            {
                if (!string.IsNullOrWhiteSpace(LastValidStreamTitle))
                {
                    desc += ", Stream Title=" + LastValidStreamTitle;
                }

                RadioLog.Common.RadioSignalingItem sigItem = new Common.RadioSignalingItem(Common.SignalingSourceType.Streaming, _sourceName, format, sigCode, unitId, desc, DateTime.Now, _recorder.CurrentFileName);
                _sigDelegate(sigItem);
            }
        }
        private void ProcessMDC1200(Decoders.MDC1200 decoder, RadioLog.Common.SignalCode sigCode, int frameCount, byte op, byte arg, ushort unitID, byte extra0, byte extra1, byte extra2, byte extra3, string opMsg)
        {
            Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Magenta, "MDC: {0} on stream {1}", opMsg, _sourceName);

            _silenceHelper.ClearSilenceStats();

            if (_sigDelegate != null)
            {
                string desc = opMsg;
                if (!string.IsNullOrWhiteSpace(LastValidStreamTitle))
                {
                    desc += ", Stream Title=" + LastValidStreamTitle;
                }
                RadioLog.Common.RadioSignalingItem sigItem = new Common.RadioSignalingItem(Common.SignalingSourceType.Streaming, _sourceName, RadioLog.Common.SignalingNames.MDC, sigCode, string.Format("{0:X4}", unitID), desc, DateTime.Now, _recorder.CurrentFileName);
                _sigDelegate(sigItem);
            }
        }
        private void ProcessSTAR(Decoders.STAR decoder, RadioLog.Common.SignalCode sigCode, uint unitID, uint tag, uint status, uint message)
        {
            Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Magenta, "STAR: {0} on stream {1}", unitID, _sourceName);

            _silenceHelper.ClearSilenceStats();

            if (_sigDelegate != null)
            {
                string desc;
                if (!string.IsNullOrWhiteSpace(LastValidStreamTitle))
                {
                    desc = string.Format("Unit: {0}, Tag: {1}, Status: {2}, Message: {3}, Stream Title: {4}", unitID, tag, status, message, LastValidStreamTitle);
                }
                else
                {
                    desc = string.Format("Unit: {0}, Tag: {1}, Status: {2}, Message: {3}", unitID, tag, status, message);
                }
                RadioLog.Common.RadioSignalingItem sigItem = new Common.RadioSignalingItem(Common.SignalingSourceType.Streaming, _sourceName, RadioLog.Common.SignalingNames.STAR, sigCode, unitID.ToString(), desc, DateTime.Now, _recorder.CurrentFileName);
                _sigDelegate(sigItem);
            }
        }

        public bool HasSound { get { return _hasAudio; } }

        public string CurrentFileName
        {
            get
            {
                if (_recorder == null || !_recorder.RecordingEnabled)
                    return string.Empty;
                else
                    return _recorder.CurrentFileName;
            }
        }
    }
}

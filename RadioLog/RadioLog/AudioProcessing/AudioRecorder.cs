using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RadioLog.Common;
using NAudio.Wave;
using NAudio.Wave.Compression;
using NAudio.Utils;

namespace RadioLog.AudioProcessing
{
    public class AudioRecorder
    {
        private readonly long MAX_FORCE_ON_TIME = TimeSpan.FromMinutes(5).Ticks;

        private object _lockObj = new object();
        private bool _useResampler;
        private AcmStream _resampleStream;
        private bool _recordingEnabled = false;
        private long _endTicks = 0;
        private long _forceOnEndTicks = 0;
        private SignalRecordingType _recordingType = SignalRecordingType.VOX;
        private AudioKickState _currentKickState = AudioKickState.Kick;
        private WaveFileWriter _waveWriter = null;
        private WaveFormat _sourceWaveFormat = null;
        private WaveFormat _fileWaveFormat = null;
        private string _currentFileName = string.Empty;

        public long RecordingKickTimeTicks { get; private set; }
        public string RecordingPrefix { get; set; }

        public AudioRecorder(string streamSourceName, Common.SignalRecordingType recordingType, int recordingKickTime, WaveFormat sourceWaveFormat, WaveFormat fileWaveFormat, bool recordingEnabled)
        {
            _recordingType = recordingType;
            switch (_recordingType)
            {
                case SignalRecordingType.Fixed:
                    {
                        RecordingKickTimeTicks = TimeSpan.FromMinutes(recordingKickTime).Ticks;
                        break;
                    }
                default:
                    {
                        RecordingKickTimeTicks = TimeSpan.FromSeconds(recordingKickTime).Ticks;
                        break;
                    }
            }
            RecordingPrefix = RadioSignalLogger.MakeSourceFilePrefix(streamSourceName);
            _sourceWaveFormat = sourceWaveFormat;
            if (fileWaveFormat == null)
                _fileWaveFormat = sourceWaveFormat;
            else
                _fileWaveFormat = fileWaveFormat;
            _recordingEnabled = recordingEnabled;

            if (_sourceWaveFormat.Equals(_fileWaveFormat))
            {
                _resampleStream = null;
                _useResampler = false;
            }
            else
            {
                _resampleStream = new NAudio.Wave.Compression.AcmStream(_sourceWaveFormat, _fileWaveFormat);
                _useResampler = true;
            }
        }

        public void UpdateRecordingKickTime(Common.SignalRecordingType recordingType, int recordingKickTime)
        {
            _recordingType = recordingType;
            switch (_recordingType)
            {
                case SignalRecordingType.Fixed:
                    {
                        RecordingKickTimeTicks = TimeSpan.FromMinutes(recordingKickTime).Ticks;
                        break;
                    }
                default:
                    {
                        RecordingKickTimeTicks = TimeSpan.FromSeconds(recordingKickTime).Ticks;
                        break;
                    }
            }
        }

        private void StartRecording(AudioKickState kickType)
        {
            if (!_recordingEnabled)
                return;
            DateTime dt = DateTime.Now;
            string fileName;
            _currentKickState = kickType;
            lock (_lockObj)
            {
                _currentFileName = string.Format("{0}_{1}.wav", RecordingPrefix, dt.ToString("yyyyMMdd_HHmmss"));
                fileName = System.IO.Path.Combine(AppSettings.Instance.RecordFileDirectory, _currentFileName);
                _waveWriter = new WaveFileWriter(fileName, _fileWaveFormat);
            }
            _endTicks = dt.Ticks + RecordingKickTimeTicks;
        }
        private void EndRecording()
        {
            if (_waveWriter != null)
            {
                try
                {
                    _waveWriter.Close();
                }
                finally { _waveWriter = null; }
            }
        }

        public void KickRecording(AudioKickState kickType)
        {
            if (_recordingType == SignalRecordingType.Fixed)
            {
                if (_waveWriter == null)
                {
                    _currentKickState = AudioKickState.Kick;
                    _forceOnEndTicks = DateTime.Now.Ticks + RecordingKickTimeTicks;
                    StartRecording(AudioKickState.Kick);
                }
            }
            else
            {
                AudioKickState calcKickState = kickType;
                if (kickType == AudioKickState.Off)
                {
                    calcKickState = AudioKickState.Kick;
                    _forceOnEndTicks = 0;
                }
                else if (kickType == AudioKickState.On)
                {
                    _forceOnEndTicks = DateTime.Now.Ticks + MAX_FORCE_ON_TIME;
                }
                if (_waveWriter == null)
                {
                    StartRecording(kickType);
                }
                else if (_currentKickState == AudioKickState.On && kickType == AudioKickState.Kick)
                {
                    _forceOnEndTicks = DateTime.Now.Ticks + MAX_FORCE_ON_TIME;
                }
                else if (_currentKickState == AudioKickState.On && kickType == AudioKickState.Off)
                {
                    _currentKickState = AudioKickState.Kick;
                    _endTicks = DateTime.Now.Ticks + RecordingKickTimeTicks;
                }
                else
                {
                    _currentKickState = calcKickState;
                    _endTicks = DateTime.Now.Ticks + RecordingKickTimeTicks;
                }
            }
        }
        public void StopRecording()
        {
            _endTicks = 0;
            _forceOnEndTicks = 0;
            _currentKickState = AudioKickState.Off;
            EndRecording();
        }

        public void ProcessSamples(bool bHasAudio, byte[] samples)
        {
            if (samples == null || samples.Length <= 0)
                return;

            if (bHasAudio)
            {
                KickRecording(AudioKickState.Kick);
            }
            if (_recordingEnabled && _waveWriter != null)
            {
                if (_useResampler)
                {
                    Buffer.BlockCopy(samples, 0, _resampleStream.SourceBuffer, 0, samples.Length);
                    int sourceBytesConverted = 0;
                    int convertedBytes = _resampleStream.Convert(samples.Length, out sourceBytesConverted);
                    if (sourceBytesConverted == samples.Length)
                    {
                        byte[] convBytes = new byte[convertedBytes];
                        Buffer.BlockCopy(_resampleStream.DestBuffer, 0, convBytes, 0, convertedBytes);
                        _waveWriter.Write(convBytes, 0, convBytes.Length);
                    }
                }
                else
                {
                    _waveWriter.Write(samples, 0, samples.Length);
                }
            }

            long curTicks = DateTime.Now.Ticks;
            if (_currentKickState == AudioKickState.On)
            {
                if (_forceOnEndTicks == 0 || _forceOnEndTicks < curTicks)
                {
                    _forceOnEndTicks = 0;
                    _endTicks = curTicks + RecordingKickTimeTicks;
                    _currentKickState = AudioKickState.Kick;
                }
            }
            else if (!_recordingEnabled || (_endTicks == 0) || (_currentKickState == AudioKickState.Kick && _endTicks < DateTime.Now.Ticks))
            {
                EndRecording();
            }
        }

        public bool RecordingEnabled
        {
            get { return _recordingEnabled; }
            set
            {
                if (_recordingEnabled != value)
                {
                    _recordingEnabled = value;
                    if (!_recordingEnabled)
                    {
                        EndRecording();
                    }
                }
            }
        }

        public string CurrentFileName
        {
            get
            {
                if (!RecordingEnabled)
                    return string.Empty;
                if (_waveWriter == null)
                    return string.Empty;
                return _currentFileName;
            }
        }
    }
}

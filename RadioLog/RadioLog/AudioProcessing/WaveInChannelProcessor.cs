using System;
using NAudio.Wave;
using System.Net;
using System.Threading;
using System.IO;
using System.Diagnostics;

using RadioLog.AudioProcessing.Providers;

namespace RadioLog.AudioProcessing
{
    public class WaveInChannelProcessor : IWaveInManagerProcessor
    {
        private string _streamName = string.Empty;
        private string _waveInSourceName = string.Empty;
        private int _waveInChannel = 0;
        private BufferedWaveProvider bufferedWaveProvider;
        private ProcessorWaveProvider processorWaveProvider;
        private IWavePlayer waveOut;
        private volatile bool sourceActive;
        private VolumeWaveProvider16 volumeProvider;
        private Common.ProcessRadioSignalingItemDelegate _sigDelegate;
        private bool _recordingEnabled;
        private Common.SignalRecordingType _recordingType;
        private int _recordingKickTime;
        private Action<bool> _propertyChangedAction;
        private bool _streamShouldPlay = true;
        private Common.NoiseFloor _noiseFloor = Common.NoiseFloor.Normal;
        private int _customNoiseFloor = 10;
        private bool _removeNoise = false;
        private bool _decodeMDC1200 = false;
        private bool _decodeGEStar = false;
        private bool _decodeFleetSync = false;
        private bool _decodeP25 = false;
        private string _waveOutDevName = string.Empty;

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

        public WaveInChannelProcessor(string streamName, string waveInSourceName, int waveInChannel, Common.ProcessRadioSignalingItemDelegate sigDelegate, Action<bool> propertyChangedAction, float initialVolume, bool recordingEnabled, Common.SignalRecordingType recordingType, int recordingKickTime, Common.NoiseFloor noiseFloor, int customNoiseFloor, bool removeNoise, bool decodeMDC1200, bool decodeGEStar, bool decodeFleetSync, bool decodeP25, string waveOutDevName)
        {
            _streamShouldPlay = true;
            _streamName = streamName;
            _waveInSourceName = waveInSourceName;
            _waveInChannel = waveInChannel;
            _sigDelegate = sigDelegate;
            _propertyChangedAction = propertyChangedAction;
            sourceActive = false;
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

            bufferedWaveProvider = new BufferedWaveProvider(AudioProcessingGlobals.GetWaveFormatForChannels(1));
            bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(3);
            processorWaveProvider = new ProcessorWaveProvider(streamName, bufferedWaveProvider, ProcessRadioSignalingItem, propertyChangedAction, recordingEnabled, recordingType, recordingKickTime, noiseFloor, customNoiseFloor, removeNoise, decodeMDC1200, decodeGEStar, decodeFleetSync, decodeP25);

            volumeProvider = new VolumeWaveProvider16(processorWaveProvider);
            volumeProvider.Volume = initialVolume;

            FirePropertyChangedAction(true);

            WaveInManager.Instance.SetupForProcessor(this);

            sourceActive = true;

            waveOut = CreateWaveOut();
            waveOut.Init(volumeProvider);
            waveOut.Play();
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

        protected void FirePropertyChangedAction(bool bUpdateTitle)
        {
            if (_propertyChangedAction != null)
            {
                _propertyChangedAction(bUpdateTitle);
            }
        }

        protected void ProcessRadioSignalingItem(RadioLog.Common.RadioSignalingItem sigItem)
        {
            if (sigItem == null || _sigDelegate == null)
                return;
            _sigDelegate(sigItem);
        }

        public void SetRecordingEnabled(bool bEnabled)
        {
            _recordingEnabled = bEnabled;
            if (processorWaveProvider != null)
            {
                processorWaveProvider.RecordingEnabled = bEnabled;
            }
        }

        public bool IsStreamActive { get { return sourceActive; } }

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
                    return 0.0f;
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
                if (volumeProvider.Volume != value)
                {
                    volumeProvider.Volume = value;
                }
            }
        }

        public string WaveInName { get { return _waveInSourceName; } }
        public int WaveInChannelIndex { get { return _waveInChannel; } }
        public string WaveOutDeviceName { get { return _waveOutDevName; } }

        public void ProcessSamples(byte[] sampleBytes)
        {
            if (sampleBytes == null || sampleBytes.Length <= 0 || !sourceActive)
                return;
            bufferedWaveProvider.AddSamples(sampleBytes, 0, sampleBytes.Length);
        }

        public void Stop()
        {
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
            sourceActive = false;
            WaveInManager.Instance.TeardownForProcessor(this);
            FirePropertyChangedAction(false);
        }

        public void WaveInStopped()
        {
            Stop();
        }
    }
}

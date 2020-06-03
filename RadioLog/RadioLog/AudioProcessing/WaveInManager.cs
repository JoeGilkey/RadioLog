using System;
using System.Net;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

using RadioLog.AudioProcessing.Providers;

using NAudio.Wave;
using NAudio.Mixer;

namespace RadioLog.AudioProcessing
{
    public class WaveInManager
    {
        private static object _lockObj = new object();
        private static WaveInManager _instance = null;
        public static WaveInManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WaveInManager();
                }
                return _instance;
            }
        }

        private Dictionary<int, WaveInManagerDevice> _waveInDevices = new Dictionary<int, WaveInManagerDevice>();

        private int GetDeviceNumFromName(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
                return -1;
            int iDeviceNum = -1;
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                WaveInCapabilities capabilities = WaveInEvent.GetCapabilities(i);
                if (string.Equals(capabilities.ProductName, deviceName, StringComparison.InvariantCultureIgnoreCase))
                {
                    iDeviceNum = i;
                    break;
                }
            }
            return iDeviceNum;
        }

        public void SetupForProcessor(IWaveInManagerProcessor processor)
        {
            if (processor == null)
                return;
            int iDevNum = GetDeviceNumFromName(processor.WaveInName);
            if (iDevNum < 0)
                return;
            lock (_lockObj)
            {
                try
                {
                    if (!_waveInDevices.ContainsKey(iDevNum))
                    {
                        _waveInDevices.Add(iDevNum, new WaveInManagerDevice(processor.WaveInName));
                    }
                    _waveInDevices[iDevNum].UpdateProcessor(processor);
                }
                catch (Exception ex)
                {
                    Common.DebugHelper.WriteExceptionToLog("WaveInManager.SetupForProcessor", ex, false, string.Format("{0} [{1}]", processor.WaveInName, processor.WaveInChannelIndex));
                }
            }
        }
        public void TeardownForProcessor(IWaveInManagerProcessor processor)
        {
            if (processor == null)
                return;
            int iDevNum = GetDeviceNumFromName(processor.WaveInName);
            if (iDevNum < 0)
                return;
            lock (_lockObj)
            {
                if (_waveInDevices.ContainsKey(iDevNum))
                {
                    _waveInDevices[iDevNum].UnregisterProcessor(processor);
                }
            }
        }
    }

    public class WaveInManagerDevice
    {
        private bool _running = false;
        private WaveInEvent waveIn;
        private string _waveInDeviceName = string.Empty;
        private int _numberOfChannels = 0;
        private List<IWaveInManagerProcessor> processors = new List<IWaveInManagerProcessor>();

        public WaveInManagerDevice(string deviceName)
        {
            _running = false;
            _waveInDeviceName = deviceName;
            int iDeviceNum = 0;
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                WaveInCapabilities capabilities = WaveInEvent.GetCapabilities(i);
                if (capabilities.ProductName == _waveInDeviceName)
                {
                    iDeviceNum = i;
                    _numberOfChannels = capabilities.Channels;
                    break;
                }
            }

            waveIn = new WaveInEvent();
            waveIn.DeviceNumber = iDeviceNum;
            waveIn.DataAvailable += waveIn_DataAvailable;
            waveIn.RecordingStopped += waveIn_RecordingStopped;
            waveIn.WaveFormat = AudioProcessingGlobals.GetWaveFormatForChannels(_numberOfChannels);

            try
            {
                StartIfNeeded();
            }
            catch (Exception ex)
            {
                Common.DebugHelper.WriteExceptionToLog("MultiWaveInProcessor", ex, false, "waveIn.StartRecording();");
                waveIn = null;
            }
        }

        public void StartIfNeeded()
        {
            if (processors.Count <= 0 || waveIn == null || _running)
                return;
            _running = true;
            waveIn.StartRecording();
        }
        public void StopIfNeeded()
        {
            if (processors.Count > 0||waveIn==null||!_running)
                return;
            _running = false;
            waveIn.StopRecording();
        }

        public void UpdateProcessor(IWaveInManagerProcessor processor)
        {
            if (processor == null)
                return;
            if (string.Equals(processor.WaveInName, _waveInDeviceName, StringComparison.InvariantCultureIgnoreCase))
                RegisterProcessor(processor);
            else
                UnregisterProcessor(processor);
        }
        public void RegisterProcessor(IWaveInManagerProcessor processor)
        {
            if (processor == null || processors.Contains(processor) || processor.WaveInChannelIndex >= _numberOfChannels || processor.WaveInChannelIndex < 0)
                return;
            processors.Add(processor);
            StartIfNeeded();
        }
        public void UnregisterProcessor(IWaveInManagerProcessor processor)
        {
            if (processors == null || processor == null || !processors.Contains(processor))
                return;
            processors.Remove(processor);
            StopIfNeeded();
        }

        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (processors == null || processors.Count <= 0)
                return;
            byte[][] sampleBytes = AudioProcessingGlobals.BytesToSampleBytes(e.Buffer, e.BytesRecorded, AudioProcessingGlobals.DEFAULT_BYTES_PER_SAMPLE, AudioProcessingGlobals.DEFAULT_ENCODING, _numberOfChannels);
            if (sampleBytes == null)
                return;
            for (int i = 0; i < processors.Count; i++)
            {
                IWaveInManagerProcessor proc = processors[i];
                proc.ProcessSamples(sampleBytes[proc.WaveInChannelIndex]);
            }
        }
        void waveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            _running = false;
            for (int i = 0; i < processors.Count; i++)
            {
                IWaveInManagerProcessor proc = processors[i];
                proc.WaveInStopped();
            }
        }
    }

    public interface IWaveInManagerProcessor
    {
        string WaveInName { get; }
        int WaveInChannelIndex { get; }
        void ProcessSamples(byte[] sampleBytes);
        void WaveInStopped();
    }
}

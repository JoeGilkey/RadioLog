using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NAudio.Wave;

namespace RadioLog.AudioProcessing.Providers
{
    public class WaveOutMixingProvider : IWaveProvider
    {
        private WaveFormat _waveFormat=null;
        private WaveOut _waveOut = null;
        private int _numChannels=0;
        private int _devNum = 0;
        private List<IWaveProvider> _providers = new List<IWaveProvider>();
        private byte[] _readBuffer;
        private byte[] _outBuffer;

        public WaveOutMixingProvider(int devNum)
        {
            _devNum = devNum;
            WaveOutCapabilities devCaps = WaveOut.GetCapabilities(devNum);
            _numChannels = devCaps.Channels;
            _providers.Clear();
        }

        public void AddInputStream(IWaveProvider waveProvider)
        {
            if (waveProvider == null)
                return;
            if (_providers.Contains(waveProvider))
                return;
            _providers.Add(waveProvider);
            if (_waveFormat == null || _waveOut == null)
            {
                _waveFormat = new WaveFormat(waveProvider.WaveFormat.SampleRate, waveProvider.WaveFormat.BitsPerSample, _numChannels);
                _waveOut = new WaveOut();
                _waveOut.DeviceNumber = _devNum;
                _waveOut.Init(this);
                _waveOut.Play();
            }
            else if (_waveOut.PlaybackState == PlaybackState.Stopped)
            {
                _waveOut.Play();
            }
            else if (_waveOut.PlaybackState == PlaybackState.Paused)
            {
                _waveOut.Resume();
            }
        }
        public void RemoveInputStream(IWaveProvider waveProvider)
        {
            if (waveProvider == null || !_providers.Contains(waveProvider))
                return;
            _providers.Remove(waveProvider);
            if (_providers.Count <= 0 && _waveOut != null && _waveOut.PlaybackState != PlaybackState.Stopped)
            {
                _waveOut.Stop();
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            _readBuffer = NAudio.Utils.BufferHelpers.Ensure(_readBuffer, count);
            _outBuffer = NAudio.Utils.BufferHelpers.Ensure(_outBuffer, count);
            Array.Clear(_outBuffer, 0, _outBuffer.Length);
            WaveBuffer outBuffer = new WaveBuffer(_outBuffer);
            foreach (IWaveProvider prov in _providers)
            {
                int ReadCnt = (prov.Read(_readBuffer, 0, _outBuffer.Length) / 2);
                WaveBuffer readBuff = new WaveBuffer(_readBuffer);
                for (int i = 0; i < ReadCnt; i++)
                {
                    outBuffer.ShortBuffer[i] += readBuff.ShortBuffer[i];
                }
            }
            Array.Copy(_outBuffer, 0, buffer, offset, count);
            return count;
        }

        public WaveFormat WaveFormat { get { return _waveFormat; } }
    }
}

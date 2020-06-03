using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;

namespace RadioLog.AudioProcessing.Providers
{
    public class SingleChannelMuxProvider : IWaveProvider
    {
        private IWaveProvider _source;
        private WaveFormat _finalFormat;
        private int _channelNum;
        private int _totalChannels;
        private int _bytesPerSample;
        private byte[] _readBuffer;

        public SingleChannelMuxProvider(IWaveProvider sourceProvider, int channelNum, int totalChannels)
        {
            _source = sourceProvider;
            _channelNum = channelNum;
            _totalChannels = totalChannels;
            _bytesPerSample = sourceProvider.WaveFormat.BitsPerSample / 8;
            _finalFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, sourceProvider.WaveFormat.BitsPerSample, totalChannels);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int perChannelCount = count / _totalChannels;
            Array.Clear(buffer, offset, count);
            _readBuffer = NAudio.Utils.BufferHelpers.Ensure(_readBuffer, perChannelCount);
            int readCnt = _source.Read(_readBuffer, 0, perChannelCount);
            int readIndex = 0;
            int outIndex = offset;
            int i,c;
            while (readIndex < readCnt)
            {
                for (c = 0; c < _totalChannels; c++)
                {
                    for (i = 0; i < _bytesPerSample; i++)
                    {
                        if (c % _totalChannels == _channelNum)
                        {
                            buffer[outIndex] = _readBuffer[readIndex];
                            readIndex++;
                        }
                        outIndex++;
                    }
                }
            }
            return readCnt * _totalChannels;
        }

        public WaveFormat WaveFormat { get { return _finalFormat; } }
    }
}

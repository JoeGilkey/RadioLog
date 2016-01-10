using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NAudio.Wave;

namespace RadioLog.AudioProcessing
{
    public static class AudioProcessingGlobals
    {
        //public const int DEFAULT_SAMPLE_RATE = 16000;
        public const int DEFAULT_SAMPLE_RATE = 48000;
        public const int DEFAULT_BITS_PER_SAMPLE = 16;
        public const int DEFAULT_CHANNELS = 1;
        public const int DEFAULT_BYTES_PER_SAMPLE = DEFAULT_BITS_PER_SAMPLE / 8;

        private const int WAV_DEFAULT_SAMPLE_RATE = 22050;
        private const int WAV_DEFAULT_BITS_PER_SAMPLE = 16;
        private const int WAV_DEFAULT_CHANNELS = 1;
        //public const int WAV_DEFAULT_BYTES_PER_SAMPLE = WAV_DEFAULT_BITS_PER_SAMPLE / 8;

        public static void OutputDiagnostics()
        {
            try
            {
                Common.DebugHelper.WriteLine("{0} WaveIn Devices Detected!", WaveIn.DeviceCount);
                for (int iDev = 0; iDev < NAudio.Wave.WaveIn.DeviceCount; iDev++)
                {
                    WaveInCapabilities wavInCaps = WaveIn.GetCapabilities(iDev);
                    Common.DebugHelper.WriteLine("  WaveIn Device {0}: {1}, {2} channels", iDev, wavInCaps.ProductName, wavInCaps.Channels);
                }
                Common.DebugHelper.WriteLine("{0} WaveOut Devices Detected!", WaveOut.DeviceCount);
                for (int iDev = 0; iDev < NAudio.Wave.WaveOut.DeviceCount; iDev++)
                {
                    WaveOutCapabilities wavOutCaps = WaveOut.GetCapabilities(iDev);
                    Common.DebugHelper.WriteLine("  WaveOut Device {0}: {1}", iDev, wavOutCaps.ProductName);
                }
            }
            catch (Exception ex)
            {
                Common.DebugHelper.WriteExceptionToLog("AudioProcessingGlobals.OutputDiagnostics",  ex, true);
            }
        }

        private static NAudio.Wave.WaveFormat _saveFileWaveFormat = null;

        public static readonly NAudio.Wave.WaveFormat DefaultProcessingWaveFormat = new NAudio.Wave.WaveFormat(DEFAULT_SAMPLE_RATE, DEFAULT_BITS_PER_SAMPLE, DEFAULT_CHANNELS);
        //public static readonly NAudio.Wave.WaveFormat DefaultProcessingWaveFormat = NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(DEFAULT_SAMPLE_RATE, DEFAULT_CHANNELS);
        public static readonly NAudio.Wave.WaveFormatEncoding DEFAULT_ENCODING = DefaultProcessingWaveFormat.Encoding;

        public static int RecordingFileSampleRate
        {
            get
            {
                int iSampleRate = Common.AppSettings.Instance.RecordingFileSampleRate;
                if (iSampleRate <= 0)
                    return WAV_DEFAULT_SAMPLE_RATE;
                else
                    return iSampleRate;
            }
            set
            {
                if (value == WAV_DEFAULT_SAMPLE_RATE)
                    Common.AppSettings.Instance.RecordingFileSampleRate = -1;
                else
                    Common.AppSettings.Instance.RecordingFileSampleRate = value;
            }
        }
        public static int RecordingFileBitsPerSample
        {
            get
            {
                int iBitsPerSample = Common.AppSettings.Instance.RecordingFileBitsPerSample;
                if (iBitsPerSample <= 0)
                    return WAV_DEFAULT_BITS_PER_SAMPLE;
                else
                    return iBitsPerSample;
            }
            set
            {
                if (value == WAV_DEFAULT_BITS_PER_SAMPLE)
                    Common.AppSettings.Instance.RecordingFileBitsPerSample = -1;
                else
                    Common.AppSettings.Instance.RecordingFileBitsPerSample = value;
            }
        }

        public static NAudio.Wave.WaveFormat DefaultSaveFileWaveFormat
        {
            get
            {
                if (_saveFileWaveFormat == null)
                {
                    _saveFileWaveFormat = new NAudio.Wave.WaveFormat(RecordingFileSampleRate, RecordingFileBitsPerSample, WAV_DEFAULT_CHANNELS);
                }
                return _saveFileWaveFormat;
            }
        }
        public static readonly NAudio.Wave.WaveFormatEncoding WAV_DEFAULT_ENCODING = DefaultSaveFileWaveFormat.Encoding;

        public static NAudio.Wave.WaveFormat GetWaveFormatForChannels(int channelCount)
        {
            return new NAudio.Wave.WaveFormat(DEFAULT_SAMPLE_RATE, DEFAULT_BITS_PER_SAMPLE, channelCount);
            //return NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(DEFAULT_SAMPLE_RATE, channelCount);
        }
        
        public static float[] BytesToFloatSamples(byte[] buffer, int BytesRecorded)
        {
            if (buffer == null || BytesRecorded <= 0 || buffer.Length < BytesRecorded)
                return null;
            if (buffer.Length == BytesRecorded)
            {
                return BytesToFloatSamples(buffer, BytesRecorded, DEFAULT_BYTES_PER_SAMPLE, DEFAULT_ENCODING);
            }
            byte[] bytes = new byte[BytesRecorded];
            Array.Copy(buffer, bytes, BytesRecorded);
            return BytesToFloatSamples(bytes, BytesRecorded, DEFAULT_BYTES_PER_SAMPLE, DEFAULT_ENCODING);
        }
        public static float[] BytesToFloatSamples(byte[] bytes, int bytesRecorded, int bytesPerSample, WaveFormatEncoding encoding)
        {
            if (bytes == null || bytes.Length <= 0)
                return null;
            float[][] samples=BytesToFloatSamples(bytes, bytesRecorded,bytesPerSample,encoding,1);
            if (samples != null && samples.Length > 0)
                return samples[0];
            else
                return null;
        }
        public static byte[][] BytesToSampleBytes(byte[] bytes, int bytesRecorded, int bytesPerSample, WaveFormatEncoding encoding, int channelCount)
        {
            try
            {
                if (bytes == null || bytes.Length <= 0)
                    return null;
                int sampleCnt = (bytesRecorded / bytesPerSample) / channelCount;
                int offset = 0;
                int sampleNum = 0;
                List<byte[]> samples = new List<byte[]>();
                for (int i = 0; i < channelCount; i++)
                {
                    samples.Add(new byte[sampleCnt * bytesPerSample]);
                }
                while (offset < bytes.Length)
                {
                    for (int iChannel = 0; iChannel < channelCount; iChannel++)
                    {
                        switch (bytesPerSample)
                        {
                            case 2:
                                {
                                    samples[iChannel][sampleNum * 2] = bytes[offset];
                                    samples[iChannel][(sampleNum * 2) + 1] = bytes[offset + 1];
                                    offset += 2;
                                    break;
                                }
                            case 3:
                                {
                                    samples[iChannel][sampleNum * 3] = bytes[offset];
                                    samples[iChannel][(sampleNum * 3) + 1] = bytes[offset + 1];
                                    samples[iChannel][(sampleNum * 3) + 2] = bytes[offset + 2];
                                    offset += 3;
                                    break;
                                }
                            case 4:
                                {
                                    samples[iChannel][sampleNum * 4] = bytes[offset];
                                    samples[iChannel][(sampleNum * 4) + 1] = bytes[offset + 1];
                                    samples[iChannel][(sampleNum * 4) + 2] = bytes[offset + 2];
                                    samples[iChannel][(sampleNum * 4) + 3] = bytes[offset + 3];
                                    offset += 4;
                                    break;
                                }
                            default:
                                {
                                    offset += bytesPerSample;
                                    break;
                                }
                        }
                    }
                    sampleNum++;
                }
                return samples.ToArray();
            }
            catch { return null; }
        }
        public static float[][] BytesToFloatSamples(byte[] bytes, int bytesRecorded, int bytesPerSample, WaveFormatEncoding encoding, int channelCount)
        {
            try
            {
                if (bytes == null || bytes.Length <= 0)
                    return null;
                int sampleCnt = (bytesRecorded / bytesPerSample) / channelCount;
                int offset = 0;
                int sampleNum = 0;
                List<float[]> samples = new List<float[]>();
                for (int i = 0; i < channelCount; i++)
                {
                    samples.Add(new float[sampleCnt]);
                }
                while (offset < bytes.Length)
                {
                    for (int iChannel = 0; iChannel < channelCount; iChannel++)
                    {
                        switch (bytesPerSample)
                        {
                            case 2:
                                {
                                    samples[iChannel][sampleNum] = BitConverter.ToInt16(bytes, offset) / 32768f;
                                    offset += 2;
                                    break;
                                }
                            case 3:
                                {
                                    samples[iChannel][sampleNum] = (((sbyte)bytes[offset + 2] << 16) | (bytes[offset + 1] << 8) | bytes[offset]) / 8388608f;
                                    offset += 3;
                                    break;
                                }
                            case 4:
                                {
                                    if (encoding == WaveFormatEncoding.IeeeFloat)
                                    {
                                        samples[iChannel][sampleNum] = BitConverter.ToSingle(bytes, offset);
                                    }
                                    else
                                    {
                                        samples[iChannel][sampleNum] = BitConverter.ToInt32(bytes, offset) / (Int32.MaxValue + 1f);
                                    }
                                    offset += 4;
                                    break;
                                }
                            default:
                                {
                                    offset += bytesPerSample;
                                    break;
                                }
                        }
                    }
                    sampleNum++;
                }
                return samples.ToArray();
            }
            catch { return null; }
        }
    }
}

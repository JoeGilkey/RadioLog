using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace RadioLog.AudioProcessing
{
    public class SilenceHelper
    {
        private static readonly long MAX_NO_SOUND_TICKS = TimeSpan.FromSeconds(15).Ticks;
        private const byte MIN_BYTE_SOUND_VAL = 10;
        private const byte MAX_BYTE_SOUND_VAL = 240;
        private const int HAS_NOISE_DIVIDER = 64;

        private bool[] _soundPackets = null;
        private int _soundPacketPointer = 0;
        private bool _hasSound = false;
        private bool _removeNoise = false;
        private Common.NoiseFloor _noiseFloor = Common.NoiseFloor.Normal;
        private float _customNoiseFloor = 0.01f;
        private long _lastHasAudioTicks = 0;
        private object _lockObj = new object();

        private static float CalculateCustomNoiseFloor(int val)
        {
            if (val <= 1)
                val = 1;
            return (float)(val / 1000f);
        }

        public SilenceHelper(int samplesPerSecond, Common.NoiseFloor noiseFloor, bool removeNoise, int customNoiseFloor)
        {
            _soundPackets = new bool[samplesPerSecond];
            _soundPacketPointer = 0;
            for (int i = 0; i < _soundPackets.Length; i++)
                _soundPackets[i] = true;
            _noiseFloor = noiseFloor;
            _removeNoise = removeNoise;
            _customNoiseFloor = CalculateCustomNoiseFloor(customNoiseFloor);
            ClearSilenceStats();
        }

        public bool RemoveNoise { get { return _removeNoise; } }

        public void UpdateNoiseFloor(Common.NoiseFloor noiseFloor, int customNoiseFloor)
        {
            _noiseFloor = noiseFloor;
            _customNoiseFloor = CalculateCustomNoiseFloor(customNoiseFloor);
        }
        public void UpdateRemoveNoise(bool bClear) { _removeNoise = bClear; }

        private void PushHasSoundWithLock(bool bHasSound)
        {
            lock (_lockObj)
            {
                PushHasSound(bHasSound);
            }
        }
        private bool PushHasSound(bool bHasSound)
        {
            _soundPackets[_soundPacketPointer++] = bHasSound;
            if (_soundPacketPointer >= _soundPackets.Length)
                _soundPacketPointer = 0;
            return bHasSound;
        }
        public void ClearSilenceStats()
        {
            _hasSound = false;
            lock (_lockObj)
            {
                for (int i = 0; i < _soundPackets.Length; i++)
                {
                    _soundPackets[i] = true;
                }
                _soundPacketPointer = 0;
            }
        }

        private void ClearNoiseProcessSoundSamples(params float[] samples)
        {
            if (_removeNoise == false || samples == null || samples.Length <= 0)
                return;
            float low = LowNoiseFloor;
            float high = HighNoiseFloor;
            for (int iPos = 0; iPos < samples.Length; iPos++)
            {
                if (!ProcessFloat(samples[iPos], low, high))
                    samples[iPos] = 0.0f;
            }
        }

        private void CalculateHasSound(int iMinGoodCnt)
        {
            int iGoodCnt = 0;
            for (int i = 0; i < _soundPackets.Length; i++)
            {
                if(_soundPackets[i])
                {
                    iGoodCnt++;
                    if (iGoodCnt > iMinGoodCnt)
                        break;
                }
            }

            _hasSound = (iGoodCnt >= iMinGoodCnt);
        }

        public bool HasSound
        {
            get
            {
                if (_lastHasAudioTicks == 0 || (DateTime.Now.Ticks - _lastHasAudioTicks) > MAX_NO_SOUND_TICKS)
                    return false;
                return _hasSound;
            }
        }

        private void ClearSoundFromBytes(byte[] bytes, int bytesOffset, int bytesLen)
        {
            for (int i = 0; i < bytesLen; i++)
                bytes[i + bytesOffset] = 0x00;
        }

        public float[][] PreProcessSamplesToFloat(byte[] bytes, int bytesOffset, int bytesRecorded, int bytesPerSample, int channelCount, bool isIeeeFloat)
        {
            try
            {
                if (bytes == null || bytes.Length <= 0)
                    return null;

                _lastHasAudioTicks = DateTime.Now.Ticks;

                float low = LowNoiseFloor;
                float high = HighNoiseFloor;
                float curSample = 0.0f;
                bool bHasSound = false;
                bool bCurHasSound = HasSound;

                int totalSampleCnt = (bytesRecorded / bytesPerSample);
                int sampleCnt = totalSampleCnt / channelCount;
                int offset = bytesOffset;
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
                                    curSample = BitConverter.ToInt16(bytes, offset) / 32768f;
                                    break;
                                }
                            case 3:
                                {
                                    curSample = (((sbyte)bytes[offset + 2] << 16) | (bytes[offset + 1] << 8) | bytes[offset]) / 8388608f;
                                    break;
                                }
                            case 4:
                                {
                                    if (isIeeeFloat)
                                    {
                                        curSample = BitConverter.ToSingle(bytes, offset);
                                    }
                                    else
                                    {
                                        curSample = BitConverter.ToInt32(bytes, offset) / (Int32.MaxValue + 1f);
                                    }
                                    break;
                                }
                            default:
                                {
                                    curSample = 0.0f;
                                    break;
                                }
                        }

                        bHasSound = PushHasSound(ProcessFloat(curSample, low, high));
                        if (_removeNoise && !bCurHasSound)
                        {
                            if (bHasSound)
                            {
                                samples[iChannel][sampleNum] = curSample;
                            }
                            else
                            {
                                samples[iChannel][sampleNum] = 0.0f;
                                ClearSoundFromBytes(bytes, offset, bytesPerSample);
                            }
                        }
                        else
                        {
                            samples[iChannel][sampleNum] = curSample;
                        }
                        offset += bytesPerSample;
                    }
                    sampleNum++;
                }

                CalculateHasSound(totalSampleCnt / (HAS_NOISE_DIVIDER * channelCount));

                return samples.ToArray();
            }
            catch { return null; }
        }
        public float[] PreProcessSamplesToFloat(byte[] bytes, int bytesOffset, int bytesRecorded, int bytesPerSample, bool isIeeeFloat = false)
        {
            if (bytes == null || bytes.Length <= 0)
                return null;
            float[][] samples = PreProcessSamplesToFloat(bytes, bytesOffset, bytesRecorded, bytesPerSample, 1, isIeeeFloat);
            if (samples != null && samples.Length > 0)
                return samples[0];
            else
                return null;
        }

        public bool ProcessSoundSamples(params float[] samples)
        {
            if (samples == null || samples.Length <= 0)
                return HasSound;
            bool bHasSound;
            float sample;

            _lastHasAudioTicks = DateTime.Now.Ticks;

            int iHasSoundCnt = 0;

            float low = LowNoiseFloor;
            float high = HighNoiseFloor;

            for (int iPos = 0; iPos < samples.Length; iPos++)
            {
                sample = samples[iPos];
                bHasSound = ProcessFloat(sample, low, high);
                if (bHasSound)
                    iHasSoundCnt++;
            }
            _hasSound = (iHasSoundCnt >= samples.Length / HAS_NOISE_DIVIDER);
            return HasSound;
        }

        private bool ProcessFloat(float sample, float low, float high)
        {
#if DEBUG
            bool bHasSound = (sample < low || sample > high);
            RadioLog.Common.ConsoleHelper.ColorWriteLine(bHasSound ? ConsoleColor.DarkMagenta : ConsoleColor.Gray, "Sound Sample Byte: {0} = {1}", sample, bHasSound);
            return bHasSound;
#else
            return (sample < low || sample > high);
#endif
        }

        private float LowNoiseFloor
        {
            get
            {
                switch (_noiseFloor)
                {
                    case Common.NoiseFloor.Low: return -0.025f;
                    case Common.NoiseFloor.High: return -0.08f;
                    case Common.NoiseFloor.ExtraHigh: return -0.15f;
                    case Common.NoiseFloor.Custom: return -_customNoiseFloor;
                    default: return -0.05f;
                }
            }
        }
        private float HighNoiseFloor
        {
            get
            {
                switch (_noiseFloor)
                {
                    case Common.NoiseFloor.Low: return 0.025f;
                    case Common.NoiseFloor.High: return 0.08f;
                    case Common.NoiseFloor.ExtraHigh: return 0.15f;
                    case Common.NoiseFloor.Custom: return _customNoiseFloor;
                    default: return 0.05f;
                }
            }
        }
        }
}

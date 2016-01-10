#define STAR_SAMPLE_FORMAT_FLOAT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders
{
    public class STAR
    {
        public delegate void STARReceivedDelegate(STAR decoder, RadioLog.Common.SignalCode sigCode, uint unitID, uint tag, uint status, uint message);

        const int NDEC = 4;
        const double THINCR = DecoderHelpers.TWOPI / 8;

        static readonly float[] _sintable = new float[]{
         0.000000f,  0.024541f,  0.049068f,  0.073565f,  0.098017f,  0.122411f,  0.146730f,  0.170962f,
         0.195090f,  0.219101f,  0.242980f,  0.266713f,  0.290285f,  0.313682f,  0.336890f,  0.359895f,
         0.382683f,  0.405241f,  0.427555f,  0.449611f,  0.471397f,  0.492898f,  0.514103f,  0.534998f,
         0.555570f,  0.575808f,  0.595699f,  0.615232f,  0.634393f,  0.653173f,  0.671559f,  0.689541f,
         0.707107f,  0.724247f,  0.740951f,  0.757209f,  0.773010f,  0.788346f,  0.803208f,  0.817585f,
         0.831470f,  0.844854f,  0.857729f,  0.870087f,  0.881921f,  0.893224f,  0.903989f,  0.914210f,
         0.923880f,  0.932993f,  0.941544f,  0.949528f,  0.956940f,  0.963776f,  0.970031f,  0.975702f,
         0.980785f,  0.985278f,  0.989177f,  0.992480f,  0.995185f,  0.997290f,  0.998795f,  0.999699f,
         1.000000f,  0.999699f,  0.998795f,  0.997290f,  0.995185f,  0.992480f,  0.989177f,  0.985278f,
         0.980785f,  0.975702f,  0.970031f,  0.963776f,  0.956940f,  0.949528f,  0.941544f,  0.932993f,
         0.923880f,  0.914210f,  0.903989f,  0.893224f,  0.881921f,  0.870087f,  0.857729f,  0.844854f,
         0.831470f,  0.817585f,  0.803208f,  0.788346f,  0.773010f,  0.757209f,  0.740951f,  0.724247f,
         0.707107f,  0.689541f,  0.671559f,  0.653173f,  0.634393f,  0.615232f,  0.595699f,  0.575808f,
         0.555570f,  0.534998f,  0.514103f,  0.492898f,  0.471397f,  0.449611f,  0.427555f,  0.405241f,
         0.382683f,  0.359895f,  0.336890f,  0.313682f,  0.290285f,  0.266713f,  0.242980f,  0.219101f,
         0.195090f,  0.170962f,  0.146730f,  0.122411f,  0.098017f,  0.073565f,  0.049068f,  0.024541f,
         0.000000f, -0.024541f, -0.049068f, -0.073565f, -0.098017f, -0.122411f, -0.146730f, -0.170962f,
        -0.195090f, -0.219101f, -0.242980f, -0.266713f, -0.290285f, -0.313682f, -0.336890f, -0.359895f,
        -0.382683f, -0.405241f, -0.427555f, -0.449611f, -0.471397f, -0.492898f, -0.514103f, -0.534998f,
        -0.555570f, -0.575808f, -0.595699f, -0.615232f, -0.634393f, -0.653173f, -0.671559f, -0.689541f,
        -0.707107f, -0.724247f, -0.740951f, -0.757209f, -0.773010f, -0.788346f, -0.803208f, -0.817585f,
        -0.831470f, -0.844854f, -0.857729f, -0.870087f, -0.881921f, -0.893224f, -0.903989f, -0.914210f,
        -0.923880f, -0.932993f, -0.941544f, -0.949528f, -0.956940f, -0.963776f, -0.970031f, -0.975702f,
        -0.980785f, -0.985278f, -0.989177f, -0.992480f, -0.995185f, -0.997290f, -0.998795f, -0.999699f,
        -1.000000f, -0.999699f, -0.998795f, -0.997290f, -0.995185f, -0.992480f, -0.989177f, -0.985278f,
        -0.980785f, -0.975702f, -0.970031f, -0.963776f, -0.956940f, -0.949528f, -0.941544f, -0.932993f,
        -0.923880f, -0.914210f, -0.903989f, -0.893224f, -0.881921f, -0.870087f, -0.857729f, -0.844854f,
        -0.831470f, -0.817585f, -0.803208f, -0.788346f, -0.773010f, -0.757209f, -0.740951f, -0.724247f,
        -0.707107f, -0.689541f, -0.671559f, -0.653173f, -0.634393f, -0.615232f, -0.595699f, -0.575808f,
        -0.555570f, -0.534998f, -0.514103f, -0.492898f, -0.471397f, -0.449611f, -0.427555f, -0.405241f,
        -0.382683f, -0.359895f, -0.336890f, -0.313682f, -0.290285f, -0.266713f, -0.242980f, -0.219101f,
        -0.195090f, -0.170962f, -0.146730f, -0.122411f, -0.098017f, -0.073565f, -0.049068f, -0.024541f };

        public enum star_format
        {
            star_format_1,                  // t1, t2, and s1 do not contribute to unit ID - original format, IDs to 2047
            star_format_1_4095,             // t1 = 2048, t2 used for mobile/portable, s1 ignored
            star_format_1_16383,            // t1 = 8192, t2 = 4096, s1 = 2048 used to allow unit IDs to 16383 -- most typical
            star_format_sys,                // t1,t2 used for system ID, s1 = 2048
            star_format_2,                  // t1 = 4096, t2 = mobile/portable, s1 = 2048
            star_format_3,                  // t1 = 4096, t2 = 8192, s1 = 2048
            star_format_4                   // t1 = 4096, t2 = 2048, s1 = 8192
        }

        private class STARSampleItem
        {
            public uint phsr;
            public int phstate;
            public int lastbit;
            public int thisbit;
            public uint rbit;
            public float theta;
            public float accum;
            public uint bitsr;
            public int bitstate;
            public uint bits0;
            public uint bits1;
            public uint bits2;
            public int bitcount;
        }

        private int _sampleRate;
        private STARSampleItem[] starSamples = new STARSampleItem[NDEC];
        private uint lastBits0;
        private int valid;
        private star_format _callback_format;
        private STARReceivedDelegate _callback = null;
        private string _sourceName = string.Empty;
        private bool _buffersAreClear = false;

        public STAR(int sampleRate, STARReceivedDelegate callback, star_format callback_format, string sourceName)
        {
            this._sampleRate = sampleRate;
            this._sourceName = sourceName;
            _callback = callback;
            _callback_format = callback_format;
            for (int i = 0; i < NDEC; i++)
            {
                starSamples[i] = new STARSampleItem();
            }
            _buffersAreClear = false;
            ClearAllDecoders();
        }

        private void ClearAllDecoders()
        {
            if (_buffersAreClear)
                return;
            for (int i = 0; i < NDEC; i++)
            {
                _reset_decoder(i);
            }
            valid = 0;
            _buffersAreClear = true;
        }
        private void _reset_decoder(int num)
        {
            starSamples[num].phsr = 0;
            starSamples[num].phstate = 0;
            starSamples[num].lastbit = 0;
            starSamples[num].rbit = 0;
            starSamples[num].accum = 0;
            starSamples[num].bitsr = 0;
            starSamples[num].bitstate = 0;
            starSamples[num].theta = (float)(THINCR * num);
        }
        private void _bitshift(int num, uint b)
        {
            starSamples[num].bitsr <<= 1;
            starSamples[num].bitsr |= b;
            switch (starSamples[num].bitstate)
            {
                case 0:
                    {
                        if ((starSamples[num].bitsr & 0x001f) == 0x0e)
                        {
                            starSamples[num].bitstate = 1;
                            starSamples[num].bitcount = 32 - 5;
                        }
                        else if ((starSamples[num].bitsr & 0x001f) == 0x11)
                        {
                            starSamples[num].bitsr ^= 0x1f;
                            starSamples[num].bitstate = 1;
                            starSamples[num].bitcount = 32 - 5;
                            starSamples[num].rbit ^= 1;
                        }
                        break;
                    }
                case 1:
                    {
                        if (--starSamples[num].bitcount == 0)
                        {
                            starSamples[num].bits0 = starSamples[num].bitsr;
                            starSamples[num].bitstate = 2;
                            starSamples[num].bitcount = 32;
                        }
                        break;
                    }
                case 2:
                    {
                        if (--starSamples[num].bitcount == 0)
                        {
                            starSamples[num].bits1 = starSamples[num].bitsr;
                            starSamples[num].bitstate = 3;
                            starSamples[num].bitcount = 16;
                        }
                        break;
                    }
                case 3:
                    {
                        if (--starSamples[num].bitcount == 0)
                        {
                            starSamples[num].bits2 = starSamples[num].bitsr;
                            starSamples[num].bits2 <<= 16;
                            starSamples[num].bitstate = 4;
                            if ((starSamples[num].bits0 & 0x07ff0000) == ((~(starSamples[num].bits1)) & 0x07ff0000))
                            {
                                if (DecoderHelpers.STAR_ComputeCDC(starSamples[num].bits0) == (starSamples[num].bits0 & 0x3f) && ((starSamples[num].bits0 & 0x3f) == (starSamples[num].bits1 & 0x3f)))
                                {
                                    lastBits0 = starSamples[num].bits0;
                                    ClearAllDecoders();
                                    valid = 1;
                                }
                            }
                        }
                        break;
                    }
                case 4:
                    {
                        _reset_decoder(num);
                        break;
                    }
            }
        }
        private void _ph_shift(int num, uint ph)
        {
            uint x;
            starSamples[num].phsr <<= 1;
            starSamples[num].phsr |= ph;
            switch (starSamples[num].phstate)
            {
                case 0:
                    {
                        if ((starSamples[num].phsr & 0xffffffff) == 0xf0f0f0f0)
                        {
                            starSamples[num].phstate = 1;
                            starSamples[num].bitstate = 0;
                        }
                        break;
                    }
                case 1:
                    {
                        starSamples[num].phstate = 2;
                        break;
                    }
                case 2:
                    {
                        starSamples[num].phstate = 3;
                        break;
                    }
                case 3:
                    {
                        starSamples[num].phstate = 4;
                        break;
                    }
                case 4:
                    {
                        x = starSamples[num].phsr & 0x01;
                        x += (starSamples[num].phsr >> 1) & 0x01;
                        x += (starSamples[num].phsr >> 2) & 0x01;
                        x += (starSamples[num].phsr >> 3) & 0x01;
                        if (x > 2)
                            starSamples[num].thisbit = 0x01;
                        else
                            starSamples[num].thisbit = 0x00;
                        if ((starSamples[num].thisbit ^ starSamples[num].lastbit) > 0)
                            starSamples[num].rbit ^= 0x01;
                        _bitshift(num, starSamples[num].rbit);
                        starSamples[num].lastbit = starSamples[num].thisbit;
                        starSamples[num].phstate = 1;
                        break;
                    }
            }
        }
        private void _process_sample_per(int num, float s)
        {
            int ofs;
            starSamples[num].theta += (float)((DecoderHelpers.TWOPI * 1600.0f) / _sampleRate);
            ofs = ((int)(starSamples[num].theta * (256.0f / DecoderHelpers.TWOPI)) % _sintable.Length);

            if (ofs >= _sintable.Length)
            {
                ClearAllDecoders();
                Console.WriteLine("STAR._process_sample_per: ofs TOO BIG!");
                return;
            }

            starSamples[num].accum += (_sintable[ofs] * s);
            if (starSamples[num].theta >= DecoderHelpers.TWOPI)
            {
                if (starSamples[num].accum < 0.0)
                    _ph_shift(num, 0);
                else
                    _ph_shift(num, 1);
                starSamples[num].theta -= (float)DecoderHelpers.TWOPI;
                starSamples[num].accum = 0.0f;
            }
        }
        private void _process_sample(float s)
        {
            for (int i = 0; i < NDEC; i++)
            {
                _process_sample_per(i, s);
            }
        }
        private int _star_decoder_get(star_format format, out uint unitId, out uint tag, out uint status, out uint message)
        {
            unitId = 0;
            tag = 0;
            status = 0;
            message = 0;
            if (valid > 0)
            {
                unitId = (lastBits0 >> 16) & 0x7ff;
                tag = (lastBits0 >> 14) & 0x03;
                status = (lastBits0 >> 10) & 0x0f;
                message = (lastBits0 >> 6) & 0x0f;
                switch (format)
                {
                    case star_format.star_format_1: // t1, t2, and s1 do not contribute to unit ID - original format, IDs to 2047
                        {
                            break;
                        }
                    case star_format.star_format_1_4095: // t1 = 2048, t2 used for mobile/portable, s1 ignored
                        {
                            if ((tag & 0x02) > 0)
                                unitId += 2048;
                            status &= 0x07;
                            break;
                        }
                    case star_format.star_format_1_16383: // t1 = 8192, t2 = 4096, s1 = 2048 used to allow unit IDs to 16383 -- most typical
                        {
                            if ((tag & 0x02) > 0)
                                unitId += 8192;
                            if ((tag & 0x01) > 0)
                                unitId += 4096;
                            if ((status & 0x08) > 0)
                                unitId += 2048;
                            status &= 0x07;
                            tag = 0;
                            break;
                        }
                    case star_format.star_format_sys: // t1,t2 used for system ID, s1 = 2048
                        {
                            if ((status & 0x08) > 0)
                                unitId += 2048;
                            status &= 0x07;
                            break;
                        }
                    case star_format.star_format_2: // t1 = 4096, t2 = mobile/portable, s1 = 2048
                        {
                            if ((tag & 0x02) > 0)
                                unitId += 4096;
                            tag &= 0x01;
                            if ((status & 0x08) > 0)
                                unitId += 2048;
                            status &= 0x07;
                            break;
                        }
                    case star_format.star_format_3: // t1 = 4096, t2 = 8192, s1 = 2048
                        {
                            if ((tag & 0x02) > 0)
                                unitId += 4096;
                            if ((tag & 0x01) > 0)
                                unitId += 8192;
                            tag = 0;
                            if ((status & 0x08) > 0)
                                unitId += 2048;
                            status &= 0x07;
                            break;
                        }
                    case star_format.star_format_4: // t1 = 4096, t2 = 2048, s1 = 8192
                        {
                            if ((tag & 0x02) > 0)
                                unitId += 4096;
                            if ((tag & 0x01) > 0)
                                unitId += 2048;
                            tag = 0;
                            if ((status & 0x08) > 0)
                                unitId += 8192;
                            status &= 0x07;
                            break;
                        }
                }
            }
            else
            {
                return -1;
            }
            return 0;
        }

#if STAR_SAMPLE_FORMAT_U8
        public int ProcessSamples(byte[] samples, int numSamples, bool bHasSound)
#elif STAR_SAMPLE_FORMAT_U16
        public int ProcessSamples(UInt16[] samples, int numSamples, bool bHasSound)
#elif STAR_SAMPLE_FORMAT_S16
        public int ProcessSamples(Int16[] samples, int numSamples, bool bHasSound)
#elif STAR_SAMPLE_FORMAT_FLOAT
        public int ProcessSamples(float[] samples, int numSamples, bool bHasSound)
#endif
        {
            if (!bHasSound)
            {
                ClearAllDecoders();
                return 0;
            }

            int i;
            float value;

            _buffersAreClear = false;
            for (i = 0; i < numSamples; i++)
            {
#if STAR_SAMPLE_FORMAT_U8
                value = (samples[i] - 128.0)/256;
#elif STAR_SAMPLE_FORMAT_U16
                value = (samples[i] - 32768.0)/65536.0;
#elif STAR_SAMPLE_FORMAT_S16
                value = (samples[i] / 65536.0;
#elif STAR_SAMPLE_FORMAT_FLOAT
                value = samples[i];
#else
                value = samples[i];
#endif
                _process_sample(value);
                if (valid > 0)
                {
                    uint unitId;
                    uint tag;
                    uint status;
                    uint message;
                    if (_star_decoder_get(_callback_format, out unitId, out tag, out status, out message) >= 0)
                    {
                        Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Green, "Src:{0},STAR:{1},Tag:{2},Status:{3},Msg:{4}", _sourceName, unitId, tag, status, message);
                        ClearAllDecoders();
                        if (_callback != null)
                        {
                            _callback(this, Common.SignalCode.PTT, unitId, tag, status, message);
                        }
                    }
                }
            }

            return valid;
        }
    }
}

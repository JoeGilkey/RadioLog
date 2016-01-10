using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Instrument
{
    public abstract class StreamTap<T> : Tap<T>
    {
        protected float mSampleRateRatio;

        public StreamTap(TapType type, string name, int delay, float sampleRate)
            : base(type, name, delay)
        {
            mSampleRateRatio = sampleRate;
        }

        public float getSampleRateRatio()
        {
            return mSampleRateRatio;
        }
    }
}

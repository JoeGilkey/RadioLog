using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders.P25
{
    public class FrameSync
    {
        public static readonly FrameSync P25_PHASE1 = new FrameSync(0x5575F5FF77FFL);
        public static readonly FrameSync P25_PHASE1_INVERTED = new FrameSync(0xFFDF5F55DD55L);

        private long mSync;

        public FrameSync(long sync)
        {
            mSync = sync;
        }

        public long getSync()
        {
            return mSync;
        }
    }
}
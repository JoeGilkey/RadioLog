using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.DSP
{
    public interface SyncDetectProvider
    {
        void setSyncDetectListener(SyncDetectListener listener);
    }
}

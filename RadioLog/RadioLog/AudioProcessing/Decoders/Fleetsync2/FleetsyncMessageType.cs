using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders.Fleetsync2
{
    public enum FleetsyncMessageType
    {
        ACKNOWLEDGE,
        ANI,
        EMERGENCY,
        GPS,
        LONE_WORKER_EMERGENCY,
        PAGING,
        STATUS,
        UNKNOWN
    }
}
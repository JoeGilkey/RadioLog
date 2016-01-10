using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.DSP
{
    /**
 * Interface to allow an external process indicate that it has detected a
 * sync pattern in the bit stream
 */
    public interface SyncDetectListener
    {
        void syncDetected();
    }
}

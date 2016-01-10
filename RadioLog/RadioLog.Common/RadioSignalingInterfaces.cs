using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Common
{
    public interface IRadioContactEmergency
    {
        string SignalingLookupKey { get; }
        EmergencyState EmergencyState { get; }
        bool CanDoManualMayday { get; set; }
    }
}

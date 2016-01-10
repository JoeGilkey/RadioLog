using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders.P25.Message
{
    public class P25Message : RadioLog.AudioProcessing.Message
    {
        public static readonly int[] NAC = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
        public static readonly int[] DUID = { 12, 13, 14, 15 };
        public static readonly int[] BCH = { 16,17,18,19,20,21,22,23,24,25,26,27,28,29,
		30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,
		54,55,56,57,58,59,60,61,62,63 };

        protected RadioLog.Common.SafeBitArray mMessage;
        protected Decoders.P25.Reference.DataUnitID mDUID;

        public P25Message(RadioLog.Common.SafeBitArray message, Decoders.P25.Reference.DataUnitID duid)
            : base()
        {
            mMessage = message;
            mDUID = duid;
        }

        public string getNAC() { return mMessage.getHex(NAC, 3); }
        public Decoders.P25.Reference.DataUnitID getDUID() { return mDUID; }

        public override bool isValid() { return true; }

        public override string GetFormatName()
        {
            return RadioLog.Common.SignalingNames.P25;
        }
        public override string GetDescription()
        {
            return string.Empty;
        }
        public override string GetUnitId()
        {
            return mDUID.getLabel();
        }
        public override Common.SignalCode GetSignalCode()
        {
            return Common.SignalCode.Generic;
        }
    }
}

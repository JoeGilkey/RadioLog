using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders.P25.Reference
{
    public class DataUnitID
    {
        public static readonly DataUnitID NID = new DataUnitID(-1, 64, "Network and Data Unit ID", false);
        public static readonly DataUnitID HDU = new DataUnitID(0, 722, "Header Data Unit", false);
        public static readonly DataUnitID TDU = new DataUnitID(3, 64, "Simple Terminator Data Unit", false);
        public static readonly DataUnitID LDU1 = new DataUnitID(5, 1630, "Logical Link Data Unit 1", true);
        public static readonly DataUnitID TSBK1 = new DataUnitID(7, 260, "Trunking Signaling Block", false); //Single Block
        public static readonly DataUnitID TSBK2 = new DataUnitID(7, 260, "Trunking Signaling Block", false); //Single Block
        public static readonly DataUnitID TSBK3 = new DataUnitID(7, 260, "Trunking Signaling Block", false); //Single Block
        public static readonly DataUnitID LDU2 = new DataUnitID(10, 1632, "Logical Link Data Unit 2", true);
        public static readonly DataUnitID PDU1 = new DataUnitID(12, 456, "Packet Data Unit", false);
        public static readonly DataUnitID PDU2 = new DataUnitID(12, 652, "Packet Data Unit", false);
        public static readonly DataUnitID PDU3 = new DataUnitID(12, 848, "Packet Data Unit", false);
        public static readonly DataUnitID TDULC = new DataUnitID(15, 372, "Terminator Data Unit With Link Control", false);
        public static readonly DataUnitID UNKN = new DataUnitID(-1, 0, "Unknown", false);

        private int mValue;
        private int mMessageLength;
        private string mLabel;
        private bool mParity;

        public DataUnitID(int value, int length, string label, bool parity)
        {
            mValue = value;
            mMessageLength = length;
            mLabel = label;
            mParity = parity;
        }

        public int getValue() { return mValue; }
        public int getMessageLength() { return mMessageLength; }
        public string getLabel() { return mLabel; }
        public bool getParity() { return mParity; }

        public static DataUnitID fromValue(int value)
        {
            switch (value)
            {
                case 0:
                    return HDU;
                case 3:
                    return TDU;
                case 5:
                    return LDU1;
                case 7:
                    return TSBK1;
                case 10:
                    return LDU2;
                case 12:
                    return PDU1;
                case 15:
                    return TDULC;
                default:
                    return UNKN;
            }
        }

        public bool Equals(DataUnitID test) { return Equals(this, test); }
        public static bool Equals(DataUnitID first, DataUnitID second)
        {
            if (first == null || second == null)
                return false;
            else
                return first.getValue() == second.getValue() && first.getMessageLength() == second.getMessageLength() && first.getParity() == second.getParity();
        }
    }
}

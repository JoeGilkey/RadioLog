using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders.P25.Message
{
    public class PDUMessage : P25Message
    {
        public static readonly int PDU_HEADER_DUMMY1 = 66; //Always 0
        public static readonly int PDU_HEADER_CONFIRMATION_REQUIRED = 67;
        public static readonly int PDU_HEADER_DIRECTION_FLAG = 68;
        public static readonly int[] PDU_HEADER_FORMAT = { 69, 70, 71, 72, 73 };
        public static readonly int PDU_HEADER_DUMMY2 = 74; //Always 1
        public static readonly int PDU_HEADER_DUMMY3 = 75; //Always 1
        public static readonly int[] PDU_HEADER_SAP_ID = { 76, 77, 78, 79, 80 };
        public static readonly int[] PDU_HEADER_VENDOR_ID = { 81, 82, 83, 84, 85, 86, 87, 88 };
        public static readonly int[] PDU_HEADER_LOGICAL_LINK_ID = { 89, 90, 91, 92, 93, 94, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114 };
        public static readonly int PDU_HEADER_FULL_MESSAGE_FLAG = 115;
        public static readonly int[] PDU_HEADER_BLOCKS_TO_FOLLOW = { 116, 117, 118, 119, 120, 121, 122 };
        public static readonly int PDU_HEADER_DUMMY4 = 123; //Always 0
        public static readonly int PDU_HEADER_DUMMY5 = 124; //Always 0
        public static readonly int PDU_HEADER_DUMMY6 = 125; //Always 0
        public static readonly int[] PDU_HEADER_PAD_BLOCKS = { 126, 127, 128, 129, 130 };
        public static readonly int PDU_HEADER_RESYNCHRONIZE_FLAG = 131;
        public static readonly int[] PDU_HEADER_SEQUENCE_NUMBER = { 132, 133, 134 };
        public static readonly int[] PDU_HEADER_FRAGMENT_SEQUENCE_NUMBER = { 135, 136, 137, 138 };
        public static readonly int PDU_HEADER_DUMMY7 = 139; //Always 0
        public static readonly int PDU_HEADER_DUMMY8 = 140; //Always 0
        public static readonly int[] PDU_HEADER_DATA_HEADER_OFFSET = { 141, 142, 143, 144, 145, 146 };
        public static readonly int[] PDU_HEADER_CRC = { 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162 };

        public PDUMessage(RadioLog.Common.SafeBitArray message, Decoders.P25.Reference.DataUnitID duid) : base(message, duid) { }

        public string getConfirmation() { return mMessage[PDU_HEADER_CONFIRMATION_REQUIRED] ? "CON" : "UNC"; }
        public string getDirection() { return mMessage[PDU_HEADER_DIRECTION_FLAG] ? "OSP" : "ISP"; }
        public Decoders.P25.Reference.PDUFormat getFormat()
        {
            int iVal = mMessage.getInt(PDU_HEADER_FORMAT);
            try
            {
                return (Reference.PDUFormat)iVal;
            }
            catch
            {
                return Reference.PDUFormat.UNKNOWN;
            }
        }
    }
}

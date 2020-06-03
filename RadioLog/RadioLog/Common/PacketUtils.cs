using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Common
{
    public static class PacketUtils
    {
        #region Hex Routines
        public static byte[] HexStrToBytes(string inStr) { if (string.IsNullOrEmpty(inStr))return null; else return HexStrToBytes(inStr, inStr.Length); }
        public static byte[] HexStrToBytes(string inStr, int iMinLen)
        {
            if (string.IsNullOrEmpty(inStr))
                return null;
            int iLen = Math.Max(iMinLen, inStr.Length);
            if (((int)(iLen % 2)) != 0)
                iLen++;
            if (inStr.Length < iMinLen)
                inStr = inStr.PadLeft(iMinLen, '0');
            int iByteLen = (int)(inStr.Length / 2);
            if (iByteLen <= 0)
                return null;
            byte[] rslt = new byte[iByteLen];
            for (int i = 0; i < iByteLen; i++)
            {
                rslt[i] = (byte)((TextCharToByte(inStr[i * 2]) << 4) + (TextCharToByte(inStr[(i * 2) + 1])));
            }
            return rslt;
        }
        public static string BytesToHexStr(byte[] val, int offset, int len)
        {
            if (val == null || val.Length < offset + len)
                return string.Empty;
            string rslt = string.Empty;
            for (int i = 0; i < len; i++)
                rslt += ByteToTextStr(val[i + offset]);
            return rslt;
        }
        public static string BytesToHexStr(byte[] val)
        {
            if (val == null || val.Length <= 0)
                return string.Empty;
            string rslt = string.Empty;
            for (int i = 0; i < val.Length; i++)
                rslt += ByteToTextStr(val[i]);
            return rslt;
        }
        static byte TextCharToByte(char c)
        {
            switch (c)
            {
                case '1': return 0x01;
                case '2': return 0x02;
                case '3': return 0x03;
                case '4': return 0x04;
                case '5': return 0x05;
                case '6': return 0x06;
                case '7': return 0x07;
                case '8': return 0x08;
                case '9': return 0x09;
                case 'a':
                case 'A': return 0x0a;
                case 'b':
                case 'B': return 0x0b;
                case 'c':
                case 'C': return 0x0c;
                case 'd':
                case 'D': return 0x0d;
                case 'e':
                case 'E': return 0x0e;
                case 'f':
                case 'F': return 0x0f;
                default: return 0x00;
            }
        }
        static string ByteToTextStr(byte b)
        {
            return string.Format("{0:X}{1:X}", (b >> 4), (b & 0x0f));
        }
        #endregion
    }
}

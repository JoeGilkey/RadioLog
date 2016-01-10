using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Common
{
    public static class BitHelper
    {
        public static uint RotateLeft(this uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }
        public static long RotateLeft(this long value, int count)
        {
            return (value << count) | (value >> (64 - count));
        }

        public static uint RotateRight(this uint value, int count)
        {
            return (value >> count) | (value << (32 - count));
        }
        public static long RotateRight(this long value, int count)
        {
            return (value >> count) | (value << (64 - count));
        }
    }
}

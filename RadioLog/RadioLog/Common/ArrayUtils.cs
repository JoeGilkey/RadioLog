using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Common
{
    public static class ArrayUtils
    {
        public static void Fill<T>(T[] array, T value) { Fill(array, 0, array.Length, value); }
        public static void Fill<T>(T[] array, int start, int count, T value)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (start + count > array.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            for (var i = start; i < start + count; i++)
            {
                array[i] = value;
            }
        }
    }
}

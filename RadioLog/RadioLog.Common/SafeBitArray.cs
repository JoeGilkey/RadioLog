using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Common
{
    public class SafeBitArray
    {
        private System.Collections.BitArray _bits;
        private int mPointer = 0;
        private int mFixedLen = 0;

        public SafeBitArray(int len)
        {
            _bits = new System.Collections.BitArray(len);
            mPointer = 0;
            mFixedLen = len;
        }
        public SafeBitArray() : this(0) { }

        public SafeBitArray(SafeBitArray bitset, int len)
            : this(len)
        {
            _bits.Or(bitset._bits);
            mPointer = len - 1;
        }

        public SafeBitArray Clone() { return CloneWithLength(_bits.Length); }
        public SafeBitArray CloneWithLength(int len) { return CloneWithLength(0, len); }
        public SafeBitArray CloneWithLength(int startPos, int len)
        {
            SafeBitArray rslt = new SafeBitArray();
            for (int i = 0; i < len; i++)
            {
                rslt[i] = this[i + startPos];
            }
            return rslt;
        }
        public SafeBitArray CloneFromIndexToIndex(int startIndex, int endIndex) { return CloneWithLength(startIndex, endIndex - startIndex); }

        public int pointer() { return mPointer; }
        public int nextSetBit(int fromIndex)
        {
            for (int i = fromIndex; i < _bits.Length; i++)
            {
                if (_bits[i])
                    return i;
            }
            return 0;
        }
        public void flip(int bitIndex)
        {
            this[bitIndex] = !this[bitIndex];
        }

        public bool this[int index]
        {
            get
            {
                EnsureBitLen(index);
                return _bits[index];
            }
            set
            {
                SetBitValue(index, value);
            }
        }

        public int Cardinality() { return Cardinality(0, _bits.Length); }
        public int Cardinality(int len) { return Cardinality(0, len); }
        public int Cardinality(int startPos, int len)
        {
            int iCnt = 0;
            for (int i = 0; i < len; i++)
            {
                if (_bits[i + startPos])
                    iCnt++;
            }
            return iCnt;
        }

        public string getHex(int[] bits, int digitDisplayCount)
        {
            if (bits.Length <= 31)
            {
                int value = getInt(bits);
                return string.Format("{0:X" + digitDisplayCount.ToString() + "}", value);
            }
            else if (bits.Length <= 63)
            {
                long value = getLong(bits);
                return string.Format("{0:X" + digitDisplayCount.ToString() + "}", value);
            }
            else
                return string.Empty;
        }

        protected void SetLength(int len)
        {
            _bits.Length = len;
        }
        protected void EnsureBitLen(int bitIndex)
        {
            if (_bits.Length < (bitIndex + 1))
                _bits.Length = bitIndex + 1;
        }

        public void ClearRange(int offset, int len)
        {
            for (int i = 0; i < len; i++)
                this[i + offset] = false;
        }
        public void ClearAll()
        {
            _bits.Length = 0;
            mPointer = 0;
        }

        public void Add(bool value)
        {
            SetBitValue(mPointer++, value);
        }
        public bool IsFull()
        {
            if (mFixedLen <= 0)
                return false;
            else
                return mPointer >= mFixedLen;
        }
        public void SetSize(int fixedLen)
        {
            mFixedLen = fixedLen;
        }
        public void SetPointer(int pointer)
        {
            mPointer = pointer;
        }

        public int getInt(int[] bits)
        {
            if (bits.Length > 31)
                return 0;
            int retVal = 0;
            for (int x = 0; x < bits.Length; x++)
            {
                if (this[bits[x]])
                    retVal += 1 << (bits.Length - 1 - x);
            }
            return retVal;
        }
        public long getLong(int start, int end)
        {
            long retVal = 0;

            for (int x = start; x <= end; x++)
            {
                if (this[x])
                {
                    retVal += 1 << (end - x);
                }
            }

            return retVal;
        }
        public long getLong(int[] bits)
        {
            if (bits.Length > 63)
            {
                return 0;
            }

            long retVal = 0;

            for (int x = 0; x < bits.Length; x++)
            {
                if (this[bits[x]])
                {
                    retVal += 1 << (bits.Length - 1 - x);
                }
            }

            return retVal;
        }

        public void SetBit(int bitNum)
        {
            EnsureBitLen(bitNum);
            _bits.Set(bitNum, true);
        }
        public void ClearBit(int bitNum)
        {
            EnsureBitLen(bitNum);
            _bits.Set(bitNum, false);
        }
        public void SetBitValue(int bitNum, bool val)
        {
            EnsureBitLen(bitNum);
            _bits.Set(bitNum, val);
        }
    }
}

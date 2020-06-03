using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders.DSD
{
    public class PointerArrayHolder<T>
    {
        private T[] _values = null;
        private int _pointer = 0;
        private T _defaultVal = default(T);

        public PointerArrayHolder(int iSize, int iPointerPos = 0, T defaultVal = default(T))
        {
            _values = new T[iSize];
            if (iPointerPos >= 0 && iPointerPos < iSize)
                _pointer = iPointerPos;
            else
                _pointer = 0;

            _defaultVal = defaultVal;

            ClearValues();
        }

        public void ClearValueRange(int iStartIndex, int iCount)
        {
            for (int i = 0; i < iCount; i++)
            {
                if (i + iStartIndex >= 0 && i + iStartIndex < _values.Length)
                {
                    _values[i + iStartIndex] = _defaultVal;
                }
            }
        }
        public void ClearValues()
        {
            ClearValueRange(0, _values.Length);
        }

        public int PointerPosition
        {
            get { return _pointer; }
            set
            {
                if (_pointer != value && value >= 0 && value < _values.Length)
                {
                    _pointer = value;
                }
            }
        }

        public T PointerVal
        {
            get { return _values[_pointer]; }
            set { _values[_pointer] = value; }
        }
    }
}

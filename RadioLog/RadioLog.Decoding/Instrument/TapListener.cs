using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Instrument
{
    public interface TapListener<T>
    {
        void receive(T t);
    }
}

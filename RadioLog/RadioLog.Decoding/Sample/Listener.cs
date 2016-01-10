using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Sample
{
    public interface Listener<T>
    {
        void receive(T t);
    }
}

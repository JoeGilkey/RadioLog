using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.DSP.FSK
{
    public class C4FMDecoder : IListener<float>
    {
        private Filter.C4FMSymbolFilter mSymbolFilter;

        public C4FMDecoder(int sampleRate)
        {
            mSymbolFilter = new Filter.C4FMSymbolFilter(sampleRate);
        }

        public void Receive(float value)
        {
            mSymbolFilter.Receive(value);
        }
    }
}

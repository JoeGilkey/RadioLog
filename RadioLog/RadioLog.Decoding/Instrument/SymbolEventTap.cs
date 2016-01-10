using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Instrument
{
    public class SymbolEventTap : StreamTap<DSP.FSK.SymbolEvent>, Sample.Listener<DSP.FSK.SymbolEvent>
    {
        private Sample.Listener<DSP.FSK.SymbolEvent> mListener;

        public SymbolEventTap(String name, int delay, float sampleRate) : base(TapType.STREAM_SYMBOL, name, delay, sampleRate) { }

        public void receive(DSP.FSK.SymbolEvent symbolEvent)
        {
            if (mListener != null)
            {
                mListener.receive(symbolEvent);
            }

            foreach (Instrument.TapListener<DSP.FSK.SymbolEvent> listener in mListeners)
            {
                listener.receive(symbolEvent);
            }
        }

        public void setListener(Sample.Listener<DSP.FSK.SymbolEvent> listener)
        {
            mListener = listener;
        }

        public void removeListener(Sample.Listener<DSP.FSK.SymbolEvent> listener)
        {
            mListener = null;
        }

    }
}

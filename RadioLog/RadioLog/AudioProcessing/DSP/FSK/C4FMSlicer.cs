using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.DSP.FSK
{
    public class C4FMSlicer:IListener<float>
    {
        private static readonly float THRESHOLD = 2.0f;

        private Broadcaster<C4FMSymbol> mBroadcaster = new Broadcaster<C4FMSymbol>();

        public void Receive(float value)
        {
            if (value > THRESHOLD)
                dispatch(C4FMSymbol.SYMBOL_PLUS_3);
            else if (value > 0)
                dispatch(C4FMSymbol.SYMBOL_PLUS_1);
            else if (value > -THRESHOLD)
                dispatch(C4FMSymbol.SYMBOL_MINUS_1);
            else
                dispatch(C4FMSymbol.SYMBOL_MINUS_3);
        }

        private void dispatch(C4FMSymbol symbol)
        {
            mBroadcaster.Receive(symbol);
        }

        public void AddListener(IListener<C4FMSymbol> listener)
        {
            mBroadcaster.AddListener(listener);
        }
        public void RemoveListener(IListener<C4FMSymbol> listener)
        {
            mBroadcaster.RemoveListener(listener);
        }
    }
}

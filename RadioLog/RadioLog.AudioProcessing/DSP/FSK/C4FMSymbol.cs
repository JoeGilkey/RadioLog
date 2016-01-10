using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.DSP.FSK
{
    public class C4FMSymbol
    {
        public static readonly C4FMSymbol SYMBOL_PLUS_3 = new C4FMSymbol(false, true);
        public static readonly C4FMSymbol SYMBOL_PLUS_1 = new C4FMSymbol(false, false);
        public static readonly C4FMSymbol SYMBOL_MINUS_1 = new C4FMSymbol(true, false);
        public static readonly C4FMSymbol SYMBOL_MINUS_3 = new C4FMSymbol(true, true);

        private bool mBit1;
        private bool mBit2;

        public C4FMSymbol(bool bit1, bool bit2)
        {
            mBit1 = bit1;
            mBit2 = bit2;
        }

        public bool getBit1() { return mBit1; }
        public bool getBit2() { return mBit2; }

        public static C4FMSymbol Inverted(C4FMSymbol symbol)
        {
            if (Equals(symbol, SYMBOL_MINUS_1))
                return SYMBOL_MINUS_3;
            else if (Equals(symbol, SYMBOL_MINUS_3))
                return SYMBOL_PLUS_3;
            else if (Equals(symbol, SYMBOL_PLUS_1))
                return SYMBOL_MINUS_1;
            else
                return SYMBOL_PLUS_1;
        }

        public static bool Equals(C4FMSymbol symbol, C4FMSymbol other)
        {
            if (other == null)
                return false;
            else
                return (other.getBit1() == symbol.getBit1() && other.getBit2() == symbol.getBit2());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.DSP.FSK
{
    public class SymbolEvent
    {
        private Common.SafeBitArray mBitset;
        private int mSamplesPerSymbol;
        private bool mDecision;
        private Shift mShift;

        public SymbolEvent(Common.SafeBitArray bitset, int samplesPerSymbol, bool decision, Shift shift)
        {
            mBitset = bitset;
            mSamplesPerSymbol = samplesPerSymbol;
            mDecision = decision;
            mShift = shift;
        }

        public Common.SafeBitArray getBitSet()
        {
            return mBitset;
        }

        public int getSamplesPerSymbol()
        {
            return mSamplesPerSymbol;
        }

        public bool getDecision()
        {
            return mDecision;
        }

        public Shift getShift()
        {
            return mShift;
        }

        public enum Shift
        {
            LEFT,
            RIGHT,
            NONE
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.DSP
{
    public class Slicer : Sample.Listener<bool>
    {
        public enum Output { NORMAL, INVERTED };

        private Common.SafeBitArray mBitSet = new Common.SafeBitArray();
        private int mSymbolLength;
        private int mDecisionThreshold;
        private int mSampleCounter;
        private Instrument.SymbolEventTap mSymbolEventTap;

        private bool mNormalOutput = true;
        private Sample.Listener<Boolean> mListener;

        public Slicer(Output output, int samplesPerSymbol)
        {
            mNormalOutput = (output == Output.NORMAL);

            mSymbolLength = samplesPerSymbol;

            /* Round up */
            mDecisionThreshold = (int)(mSymbolLength / 2) + (mSymbolLength % 2);
        }

        private System.Collections.BitArray CloneBitArray(System.Collections.BitArray inBits, int start, int len)
        {
            System.Collections.BitArray rslt = new System.Collections.BitArray(len);
            for (int i = 0; i < len; i++)
            {
                rslt.Set(i, inBits.Get(i + start));
            }
            return rslt;
        }
        public void receive(Boolean sample)
        {
            if (mSampleCounter >= 0)
            {
                if (sample)
                {
                    mBitSet.SetBit(mSampleCounter);
                }
                else
                {
                    mBitSet.ClearBit(mSampleCounter);
                }
            }

            mSampleCounter++;

            if (mSampleCounter >= mSymbolLength)
            {
                bool decision = mBitSet.Cardinality() >= mDecisionThreshold;

                send(decision);

                /* Shift timing left if the left bit in the bitset is opposite 
                 * the decision and the right bit is the same */
                if ((mBitSet[0] ^ decision) &&
                    (!(mBitSet[mSymbolLength - 1] ^ decision)))
                {
                    sendTapEvent(mBitSet, DSP.FSK.SymbolEvent.Shift.LEFT, decision);

                    reset();

                    mSampleCounter--;
                }
                /* Shift timing right if the left bit is the same as the 
                 * decision and the right bit is opposite */
                else if ((!(mBitSet[0] ^ decision)) &&
                         (mBitSet[mSymbolLength - 1] ^ decision))
                {
                    sendTapEvent(mBitSet, DSP.FSK.SymbolEvent.Shift.RIGHT, decision);

                    /* Last bit from previous symbol to pre-fill next symbol */
                    bool previousSoftBit = mBitSet[mSymbolLength - 1];

                    reset();

                    if (previousSoftBit)
                    {
                        mBitSet.SetBit(0);
                    }

                    mSampleCounter++;
                }
                /* No shift */
                else
                {
                    sendTapEvent(mBitSet, DSP.FSK.SymbolEvent.Shift.NONE, decision);

                    reset();
                }
            }
        }

        /**
         * Sends the bit decision to the listener
         */
        private void send(bool decision)
        {
            if (mListener != null)
            {
                mListener.receive(mNormalOutput ? decision : !decision);
            }
        }

        private void reset()
        {
            mBitSet.ClearAll();
            mSampleCounter = 0;
        }

        public void setListener(Sample.Listener<Boolean> listener)
        {
            mListener = listener;
        }

        public void removeListener(Sample.Listener<Boolean> listener)
        {
            mListener = null;
        }

        /**
         * Sends instrumentation tap event to all registered listeners 
         */
        private void sendTapEvent(Common.SafeBitArray bitset, DSP.FSK.SymbolEvent.Shift shift, bool decision)
        {
            if (mSymbolEventTap != null)
            {
                DSP.FSK.SymbolEvent sEvent =
                        new DSP.FSK.SymbolEvent(bitset.CloneWithLength(mSymbolLength),
                                         mSymbolLength,
                                         decision,
                                         shift);

                mSymbolEventTap.receive(sEvent);
            }
        }

        public void addTap(Instrument.SymbolEventTap tap)
        {
            mSymbolEventTap = tap;
        }

        public void removeTap()
        {
            mSymbolEventTap = null;
        }
    }
}
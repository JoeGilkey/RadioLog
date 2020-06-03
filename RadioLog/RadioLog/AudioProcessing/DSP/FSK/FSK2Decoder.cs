using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.DSP.FSK
{
    public class FSK2Decoder : Instrumentable<SymbolEvent>, IListener<float>, IListenerOutput<bool>
    {
        private IListener<bool> mListener = null;
        private RadioLog.Common.BooleanAveragingBuffer mDelayBuffer;
        private RadioLog.Common.BooleanAveragingBuffer mLowPassFilter;
        private bool mNormalOutput;
        private int mSamplesPerSymbol;
        private int mSymbolRate;
        private Slicer mSlicer;

        public FSK2Decoder(int sampleRate, int symbolRate, Output output)
        {
            mSamplesPerSymbol = (int)(sampleRate / symbolRate);
            mNormalOutput = (output == Output.NORMAL);
            mSymbolRate = symbolRate;

            mDelayBuffer = new RadioLog.Common.BooleanAveragingBuffer(mSamplesPerSymbol);
            mLowPassFilter = new RadioLog.Common.BooleanAveragingBuffer(mSamplesPerSymbol);

            mSlicer = new Slicer(this, mSamplesPerSymbol);
        }

        public void Receive(float floatSample)
        {
            /* Square the sample.  Greater than zero is a 1 (true) and less than 
		 * zero is a 0 (false) */
            bool bitSample = (floatSample >= 0.0f);

            /* Feed the delay buffer and fetch the one-baud delayed sample */
            bool delayedBitSample = mDelayBuffer.get(bitSample);

            /* Correlation: xor current bit with delayed bit */
            bool softBit = bitSample ^ delayedBitSample;

            /* Low pass filter to smooth the correlated values */
            bool filteredSoftBit = mLowPassFilter.getAverage(softBit);

            /* Send the filtered correlated bit to the slicer */
            mSlicer.Receive(filteredSoftBit);
        }

        /**
	 * Symbol slicer with auto-aligning baud timing
	 */
        public class Slicer
        {
            private RadioLog.Common.SafeBitArray mBitSet = new Common.SafeBitArray();
            private int mSymbolLength;
            private int mDecisionThreshold;
            private int mSampleCounter;
            private FSK2Decoder mDecoder;

            public Slicer(FSK2Decoder decoder, int samplesPerSymbol)
            {
                mDecoder = decoder;
                mSymbolLength = samplesPerSymbol;

                mDecisionThreshold = (int)(mSymbolLength / 2);

                /* Adjust for an odd number of samples per baud */
                if (mSymbolLength % 2 == 1)
                {
                    mDecisionThreshold++;
                }
            }

            public void Receive(bool softBit)
            {
                if (mSampleCounter >= 0)
                {
                    if (softBit)
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
                        sendTapEvent(mBitSet, Shift.LEFT, decision);

                        reset();

                        mSampleCounter--;
                    }
                    /* Shift timing right if the left bit is the same as the 
                     * decision and the right bit is opposite */
                    else if ((!(mBitSet[0] ^ decision)) && (mBitSet[mSymbolLength - 1] ^ decision))
                    {
                        sendTapEvent(mBitSet, Shift.RIGHT, decision);

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
                        sendTapEvent(mBitSet, Shift.NONE, decision);

                        reset();
                    }
                }
            }

            /**
             * Sends the bit decision to the listener
             */
            private void send(bool decision)
            {
                if (mDecoder.mListener != null)
                {
                    mDecoder.mListener.Receive(mDecoder.mNormalOutput ? decision : !decision);
                }
            }

            private void reset()
            {
                mBitSet.ClearAll();
                mSampleCounter = 0;
            }

            /**
             * Sends instrumentation tap event to all registered listeners 
             */
            private void sendTapEvent(RadioLog.Common.SafeBitArray bitset, Shift shift, bool decision)
            {
                foreach (IListener<SymbolEvent> tap in mDecoder.GetListeners())
                {
                    SymbolEvent sEvent = new SymbolEvent(bitset.CloneFromIndexToIndex(0, mSymbolLength), mSymbolLength, decision, shift);

                    tap.Receive(sEvent);
                }
            }
        }

        public void SetOutputListener(IListener<bool> outListener)
        {
            mListener = outListener;
        }

        public void RemoveOutputListener()
        {
            mListener = null;
        }
    }
}

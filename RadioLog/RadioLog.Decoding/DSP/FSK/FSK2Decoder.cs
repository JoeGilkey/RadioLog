using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.DSP.FSK
{
    /**
 * Binary Frequency Shift Keying (FSK) decoder.  Implements a BFSK correlation
 * decoder.  Provides normal or inverted decoded output.  Requires the symbol
 * rate to be an integer multiple of the sample rate.
 * 
 * Implements a correlation decoder that correlates a modulated FSK signal upon 
 * against a one-baud delayed version of itself, converting all float samples to 
 * binary (0 or 1), and uses XOR to produce the correlation value.  Low pass 
 * filtering smoothes the correlated output.
 * 
 * Automatically aligns to symbol timing during symbol transitions (0 to 1, or
 * 1 to 0) by inspecting the samples at the symbol edges and advancing or 
 * retarding symbol window to maintain continuous symbol alignment.
 * 
 * Use a DC-removal filter prior to this decoder to ensure samples don't have
 * a DC component.
 * 
 * Implements instrumentable interface, so that slice events can be received
 * externally to analyze decoder performance.
 */
    public class FSK2Decoder : Sample.Real.RealSampleBroadcaster, Instrument.Instrumentable<DSP.FSK.SymbolEvent>
    {
        public enum Output { NORMAL, INVERTED };

        /* Instrumentation taps */
        private static readonly string INSTRUMENT_DECISION = "Tap Point: FSK2 Symbol Decision";
        private List<Instrument.Tap<DSP.FSK.SymbolEvent>> mAvailableTaps;
        private List<Instrument.SymbolEventTap> mTaps = new List<Instrument.SymbolEventTap>();

        private Sample.Listener<bool> mListener;
        private Buffer.BooleanAveragingBuffer mDelayBuffer;
        private Buffer.BooleanAveragingBuffer mLowPassFilter;
        private Slicer mSlicer;
        private bool mNormalOutput;
        private int mSamplesPerSymbol;
        private int mSymbolRate;

        public FSK2Decoder(int sampleRate, int symbolRate, Output output)
        {
            /* Ensure we're using an integral symbolRate of the sampleRate */
            System.Diagnostics.Debug.Assert(sampleRate % symbolRate == 0);

            mSamplesPerSymbol = (int)(sampleRate / symbolRate);
            mNormalOutput = (output == Output.NORMAL);
            mSymbolRate = symbolRate;

            mDelayBuffer = new Buffer.BooleanAveragingBuffer(mSamplesPerSymbol);
            mLowPassFilter = new Buffer.BooleanAveragingBuffer(mSamplesPerSymbol);
            mSlicer = new Slicer(this, mSamplesPerSymbol);
        }

        /**
         * Disposes of all references to prepare for garbage collection
         */
        public void dispose()
        {
            mListener = null;
        }

        /**
         * Primary sample input
         */
        public void receive(float floatSample)
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
            mSlicer.receive(filteredSoftBit);
        }

        /**
         * Registers a listener to receive the decoded FSK bits
         */
        public void setListener(Sample.Listener<Boolean> listener)
        {
            mListener = listener;
        }

        /**
         * Removes the listener
         */
        public void removeListener(Sample.Listener<Boolean> listener)
        {
            mListener = null;
        }

        /**
         * Symbol slicer with auto-aligning baud timing
         */
        public class Slicer
        {
            private FSK2Decoder _decoder;
            private Common.SafeBitArray mBitSet = new Common.SafeBitArray();
            private int mSymbolLength;
            private int mDecisionThreshold;
            private int mSampleCounter;

            public Slicer(FSK2Decoder decoder, int samplesPerSymbol)
            {
                _decoder = decoder;
                mSymbolLength = samplesPerSymbol;

                mDecisionThreshold = (int)(mSymbolLength / 2);

                /* Adjust for an odd number of samples per baud */
                if (mSymbolLength % 2 == 1)
                {
                    mDecisionThreshold++;
                }
            }

            public void receive(bool softBit)
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
                if (_decoder!=null&&_decoder.mListener != null)
                {
                    _decoder.mListener.receive(_decoder.mNormalOutput ? decision : !decision);
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
            private void sendTapEvent(Common.SafeBitArray bitset, DSP.FSK.SymbolEvent.Shift shift, bool decision)
            {
                if (_decoder != null)
                {
                    foreach (Instrument.SymbolEventTap tap in _decoder.mTaps)
                    {
                        SymbolEvent sEvent =
                                new SymbolEvent(bitset.CloneWithLength(mSymbolLength),
                                                 mSymbolLength,
                                                 decision,
                                                 shift);

                        tap.receive(sEvent);
                    }
                }
            }
        }

        /**
         * Get instrumentation taps
         */
        public List<Instrument.Tap<DSP.FSK.SymbolEvent>> getTaps()
        {
            if (mAvailableTaps == null)
            {
                mAvailableTaps = new List<Instrument.Tap<DSP.FSK.SymbolEvent>>();
                mAvailableTaps.Add(
                        new Instrument.SymbolEventTap(INSTRUMENT_DECISION, 0, .025f));
            }

            return mAvailableTaps;
        }

        /**
         * Add instrumentation tap
         */
        public void addTap(Instrument.Tap<DSP.FSK.SymbolEvent> tap)
        {
            Instrument.SymbolEventTap s = tap as Instrument.SymbolEventTap;
            if (s != null && !mTaps.Contains(tap))
            {
                mTaps.Add(s);
            }
        }

        /**
         * Remove instrumentation tap 
         */
        public void removeTap(Instrument.Tap<DSP.FSK.SymbolEvent> tap)
        {
            mTaps.Remove((Instrument.SymbolEventTap)tap);
        }
    }
}

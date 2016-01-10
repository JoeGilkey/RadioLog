using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders
{
    public class P25Decoder:Decoder
    {
        private DSP.Filter.ComplexFIRFilter mBasebandFilter;
        private DSP.NBFM.FMDiscriminator mDemodulator = new DSP.NBFM.FMDiscriminator(1);
        private DSP.Filter.C4FMSymbolFilter mSymbolFilter;
        private DSP.FSK.C4FMSlicer mSlicer = new DSP.FSK.C4FMSlicer();
        private DSP.FSK.P25MessageFramer mNormalFramer;
        private DSP.FSK.P25MessageFramer mInvertedFramer;
        private DSP.FSK.P25MessageProcessor mMessageProcessor;

        public P25Decoder(int sampleRate, SampleType sampleType)
            : base(sampleType)
        {
            if (sampleType == SampleType.COMPLEX) //JG This is used when listening to the raw feed, not demodulated.
            {
                mBasebandFilter = new DSP.Filter.ComplexFIRFilter(DSP.Filter.FilterFactory.getLowPass(sampleRate, 5000, 31, WindowType.HAMMING), 1.0);
                this.addComplexListener(mBasebandFilter);
                mBasebandFilter.SetListener(mDemodulator);
                mDemodulator.setListener(getRealReceiver());
            }

            mSymbolFilter = new DSP.Filter.C4FMSymbolFilter(sampleRate);
            addRealSampleListener(mSymbolFilter);
            mSymbolFilter.SetOutputListener(mSlicer);

            mMessageProcessor = new DSP.FSK.P25MessageProcessor();
            
            mNormalFramer = new DSP.FSK.P25MessageFramer(P25.FrameSync.P25_PHASE1.getSync(), 64, false);
            mSlicer.AddListener(mNormalFramer);
            mNormalFramer.setListener(mMessageProcessor);

            mInvertedFramer = new DSP.FSK.P25MessageFramer(P25.FrameSync.P25_PHASE1_INVERTED.getSync(), 64, true);
            mSlicer.AddListener(mInvertedFramer);
            mInvertedFramer.setListener(mMessageProcessor);

            mMessageProcessor.addMessageListener(this);
        }

        public override DecoderType getDecoderType()
        {
            return DecoderType.P25_PHASE1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders
{
    public class Fleetsync2Decoder : Decoder
    {
        /* Decimated sample rate ( 48,000 / 2 = 24,000 ) feeding the decoder */
        private static readonly int sDECIMATED_SAMPLE_RATE = 24000;

        /* Baud or Symbol Rate */
        private static readonly int sSYMBOL_RATE = 1200;

        /* Message length - 5 x REVS + 16 x SYNC + 8 x 64Bit Blocks */
        private static readonly int sMESSAGE_LENGTH = 537;

        public override DecoderType getDecoderType() { return DecoderType.FLEETSYNC2; }

        private DSP.FSK.FSK2Decoder mFSKDecoder;
        private DSP.Filter.FloatHalfBandFilter mDecimationFilter;
        private DSP.Filter.FloatFIRFilter mBandPassFilter;
        private Bits.MessageFramer mMessageFramer;
        private Fleetsync2.Fleetsync2MessageProcessor mMessageProcessor;

        public Fleetsync2Decoder(int sampleRate)
            : base(SampleType.REAL)
        {
            mDecimationFilter = new DSP.Filter.FloatHalfBandFilter(DSP.Filter.Filters.FIR_HALF_BAND_31T_ONE_EIGHTH_FCO, 1.0002);
            addRealSampleListener(mDecimationFilter);

            mBandPassFilter = new DSP.Filter.FloatFIRFilter(DSP.Filter.FilterCoefficientHelper.getCoefficients(DSP.Filter.Filters.FIRBP_1200FSK_24000FS), 1.02);
            mDecimationFilter.SetOutputListener(mBandPassFilter);

            //mFSKDecoder = new DSP.FSK.FSK2Decoder(sDECIMATED_SAMPLE_RATE, sSYMBOL_RATE, Output.INVERTED);
            mFSKDecoder = new DSP.FSK.FSK2Decoder(sampleRate, sSYMBOL_RATE, Output.INVERTED);
            mBandPassFilter.SetOutputListener(mFSKDecoder);

            mMessageFramer = new Bits.MessageFramer(Bits.SyncPatternHelper.getPattern(Bits.SyncPattern.FLEETSYNC2), sMESSAGE_LENGTH);
            mFSKDecoder.SetOutputListener(mMessageFramer);

            mMessageProcessor = new Fleetsync2.Fleetsync2MessageProcessor();
            mMessageFramer.addMessageListener(mMessageProcessor);
            mMessageProcessor.addMessageListener(this);
        }

        public override IListener<float> getRealReceiver() { return mDecimationFilter; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Decode.FleetSync2
{
    public class Fleetsync2Decoder: Decoder, Instrument.Instrumentable<DSP.FSK.SymbolEvent>
{
	/* Decimated sample rate ( 48,000 / 2 = 24,000 ) feeding the decoder */
	private static readonly int sDECIMATED_SAMPLE_RATE = 24000;
	
	/* Baud or Symbol Rate */
	private static readonly int sSYMBOL_RATE = 1200;

	/* Message length - 5 x REVS + 16 x SYNC + 8 x 64Bit Blocks */
    private static readonly int sMESSAGE_LENGTH = 537;
    
    /* Instrumentation Taps */
    private List<Instrument.Tap<DSP.FSK.SymbolEvent>> mAvailableTaps;
	private static readonly String INSTRUMENT_INPUT = "Tap Point: Float Input";
	private static readonly String INSTRUMENT_BANDPASS_FILTER_TO_FSK2_DEMOD = "Tap Point: Bandpass Filter > < FSK2 Decoder";
	private static readonly String INSTRUMENT_FSK2_DECODER_TO_MESSAGE_FRAMER = "Tap Point: FSK2 Decoder > < Message Framer";
	
    private DSP.FSK.FSK2Decoder mFSKDecoder;
    private DSP.Filter.FloatHalfBandFilter mDecimationFilter;
    private DSP.Filter.FloatFIRFilter mBandPassFilter;
    private Bits.MessageFramer mMessageFramer;
    private Fleetsync2MessageProcessor mMessageProcessor;
    
    public Fleetsync2Decoder():base(Source.SampleType.REAL)
	{
    	
        mDecimationFilter = new DSP.Filter.FloatHalfBandFilter(DSP.Filter.FilterType.FIR_HALF_BAND_31T_ONE_EIGHTH_FCO, 1.0002 );
        addRealSampleListener( mDecimationFilter );

        mBandPassFilter = new FloatFIRFilter( DSP.Filter.FilterTypeCoefficients.GetFilterCoefficients(DSP.Filter.FilterType.FIRBP_1200FSK_24000FS), 1.02 );
        mDecimationFilter.setListener( mBandPassFilter );

        mFSKDecoder = new DSP.FSK.FSK2Decoder.FSK2Decoder(sDECIMATED_SAMPLE_RATE, sSYMBOL_RATE, DSP.FSK.FSK2Decoder.Output.INVERTED);
        mBandPassFilter.setListener( mFSKDecoder );

        mMessageFramer = new Message.MessageFramer( SyncPattern.FLEETSYNC2.getPattern(), sMESSAGE_LENGTH );
        mFSKDecoder.setListener( mMessageFramer );
        
        mMessageProcessor = new Fleetsync2MessageProcessor( aliasList );
        mMessageFramer.addMessageListener( mMessageProcessor );
        mMessageProcessor.addMessageListener( this );
	}
    
    public override void dispose()
    {
        base.dispose();
    	
    	mDecimationFilter.dispose();
    	mBandPassFilter.dispose();
    	mFSKDecoder.dispose();
    	mMessageFramer.dispose();
    	mMessageProcessor.dispose();
    }

	public DecoderType getType()
    {
	    return DecoderType.FLEETSYNC2;
    }

	/**
	 * Returns a float listener interface for connecting this decoder to a 
	 * float stream provider
	 */
	public Sample.Real.RealSampleBroadcaster getRealReceiver()
	{
        return (Sample.Real.RealSampleBroadcaster)mDecimationFilter;
	}
	
	public List<Instrument.Tap<DSP.FSK.SymbolEvent>> getTaps()
    {
		if( mAvailableTaps == null )
		{
			mAvailableTaps = new List<Instrument.Tap<DSP.FSK.SymbolEvent>();
			
			mAvailableTaps.Add( new Instrument.FloatTap( INSTRUMENT_INPUT, 0, 1.0f ) );
			mAvailableTaps.Add( new Instrument.FloatTap( INSTRUMENT_BANDPASS_FILTER_TO_FSK2_DEMOD, 0, 0.5f ) );
            mAvailableTaps.AddRange(mFSKDecoder.getTaps());
			mAvailableTaps.Add( new BinaryTap( INSTRUMENT_FSK2_DECODER_TO_MESSAGE_FRAMER, 0, 0.025f ) );
		}
		
	    return mAvailableTaps;
    }

	public void addTap( Instrument.Tap<DSP.FSK.SymbolEvent> tap )
    {
		mFSKDecoder.addTap( tap );

		switch( tap.getName() )
		{
			case INSTRUMENT_INPUT:
				FloatTap inputTap = (FloatTap)tap;
				addRealSampleListener( inputTap );
				break;
			case INSTRUMENT_BANDPASS_FILTER_TO_FSK2_DEMOD:
				FloatTap bpTap = (FloatTap)tap;
				mBandPassFilter.setListener( bpTap );
				bpTap.setListener( mFSKDecoder );
				break;
			case INSTRUMENT_FSK2_DECODER_TO_MESSAGE_FRAMER:
				BinaryTap decoderTap = (BinaryTap)tap;
				mFSKDecoder.setListener( decoderTap );
				decoderTap.setListener( mMessageFramer );
		        break;
		}
    }

	public void removeTap( Instrument.Tap<DSP.FSK.SymbolEvent> tap )
    {
		mFSKDecoder.removeTap( tap );

		switch( tap.getName() )
		{
			case INSTRUMENT_INPUT:
				FloatTap inputTap = (FloatTap)tap;
				removeRealListener( inputTap );
				break;
			case INSTRUMENT_BANDPASS_FILTER_TO_FSK2_DEMOD:
				mBandPassFilter.setListener( mFSKDecoder );
				break;
			case INSTRUMENT_FSK2_DECODER_TO_MESSAGE_FRAMER:
				mFSKDecoder.setListener( mMessageFramer );
		        break;
		}
    }

	public void addUnfilteredRealSampleListener( Sample.Real.RealSampleBroadcaster listener )
    {
		//Not implemented
    }
}
}

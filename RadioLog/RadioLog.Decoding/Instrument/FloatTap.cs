using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Instrument
{
    public class FloatTap:StreamTap<float>
{
	private Sample.Real.RealSampleBroadcaster mListener;
	
	public FloatTap( String name, int delay, float sampleRateRatio ):base(TapType.STREAM_FLOAT, name, delay, sampleRateRatio){
    }

    public void receive(float sample)
    {
        if (mListener != null)
        {
            mListener.receive(sample);
        }

        foreach (TapListener<float> listener in mListeners)
        {
            listener.receive(sample);
        }
    }

	public void setListener( Sample.Real.RealSampleBroadcaster listener )
    {
		mListener = listener;
    }

    public void removeListener(Sample.Real.RealSampleBroadcaster listener)
    {
		mListener = null;
    }
}
}

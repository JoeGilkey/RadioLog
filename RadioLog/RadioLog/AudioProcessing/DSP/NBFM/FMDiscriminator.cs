using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.DSP.NBFM
{
    public class FMDiscriminator : IListener<ComplexSample>
    {
        private IListener<float> mListener;
        private ComplexSample mPreviousSample = new ComplexSample(0.0f, 0.0f);
        private double mGain;

        public FMDiscriminator(double gain)
        {
            mGain = gain;
        }

        public void setGain(double gain)
        {
            mGain = gain;
        }

        public void Receive(ComplexSample currentSample)
        {
            /**
		 * Multiply the current sample against the complex conjugate of the 
		 * previous sample to derive the phase delta between the two samples
		 * 
		 * Negating the previous sample quadrature produces the conjugate
		 */
            double i = (currentSample.InPhase() * mPreviousSample.InPhase()) -
                    (currentSample.Quadrature() * -mPreviousSample.Quadrature());
            double q = (currentSample.Quadrature() * mPreviousSample.InPhase()) +
                    (currentSample.InPhase() * -mPreviousSample.Quadrature());

            double angle;

            //Check for divide by zero
            if (i == 0)
            {
                angle = 0.0;
            }
            else
            {
                /**
                 * Use the arcus tangent of imaginary (q) divided by real (i) to
                 * get the phase angle (+/-) which was directly manipulated by the
                 * original message waveform during the modulation.  This value now
                 * serves as the instantaneous amplitude of the demodulated signal
                 */
                double denominator = 1.0d / i;
                angle = Math.Atan((double)q * denominator);
            }

            if (mListener != null)
            {
                mListener.Receive((float)(angle * mGain));
            }

            /**
             * Store the current sample to use during the next iteration
             */
            mPreviousSample = currentSample;
        }

        public void setListener(IListener<float> listener)
        {
            mListener = listener;
        }
    }
}
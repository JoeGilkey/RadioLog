using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Sample.Complex
{
    /**
 * ComplexSample for handling left/right channel audio, or inphase/quadrature 
 * samples, etc.  
 */
    public class ComplexSample
    {
        private static readonly long serialVersionUID = 1L;

        private float mLeft;
        private float mRight;

        public ComplexSample(float left, float right)
        {
            mLeft = left;
            mRight = right;
        }

        public ComplexSample copy()
        {
            return new ComplexSample(mLeft, mRight);
        }

        public String toString()
        {
            return "I:" + mLeft + " Q:" + mRight;
        }

        /**
         * Returns a new sample representing the conjugate of this one
         */
        public ComplexSample conjugate()
        {
            return new ComplexSample(mLeft, -mRight);
        }

        /**
         * Multiplies this sample by the scalor value
         */
        public void multiply(float scalor)
        {
            mLeft *= scalor;
            mRight *= scalor;
        }

        /**
         * Multiplies this sample by the multiplier sample
         */
        public void multiply(ComplexSample multiplier)
        {
            float inphase = (this.inphase() * multiplier.inphase()) -
                            (this.quadrature() * multiplier.quadrature());

            float quadrature = (this.quadrature() * multiplier.inphase()) +
                               (this.inphase() * multiplier.quadrature());

            mLeft = inphase;
            mRight = quadrature;
        }

        public static ComplexSample multiply(ComplexSample sample1,
                                              ComplexSample sample2)
        {
            float inphase = (sample1.inphase() * sample2.inphase()) -
            (sample1.quadrature() * sample2.quadrature());

            float quadrature = (sample1.quadrature() * sample2.inphase()) +
               (sample1.inphase() * sample2.quadrature());

            return new ComplexSample(inphase, quadrature);
        }

        public static ComplexSample multiply(ComplexSample sample, float left, float right)
        {
            float l = (sample.left() * left) - (sample.right() * right);
            float r = (sample.right() * left) + (sample.left() * right);

            return new ComplexSample(l, r);
        }

        /**
         * Adds the adder sample values to this sample
         */
        public void add(ComplexSample adder)
        {
            mLeft += adder.left();
            mRight += adder.right();
        }

        /**
         * Magnitude of this sample
         */
        public float magnitude()
        {
            return (float)Math.Sqrt(magnitudeSquared());
        }

        /**
         * Magnitude squared of this sample
         */
        public float magnitudeSquared()
        {
            return (float)((inphase() * inphase()) +
                          (quadrature() * quadrature()));
        }

        /**
         * Returns the vector length to 1 (unit circle)
         */
        public void normalize()
        {
            multiply((float)(1.0f / magnitude()));
        }

        /**
         * Returns the vector length to 1 (unit circle),
         * avoiding square root multiplication
         */
        public void fastNormalize()
        {
            multiply((float)(1.95f - magnitudeSquared()));
        }

        public float left()
        {
            return mLeft;
        }

        public float right()
        {
            return mRight;
        }

        public float inphase()
        {
            return mLeft;
        }

        public float quadrature()
        {
            return mRight;
        }

        public float x()
        {
            return mLeft;
        }

        public float y()
        {
            return mRight;
        }

        public float real()
        {
            return mLeft;
        }

        public float imaginery()
        {
            return mRight;
        }

        /**
         * Returns the greater absolute value between left and right values 
         */
        public float maximumAbsolute()
        {
            if (Math.Abs(mLeft) > Math.Abs(mRight))
            {
                return Math.Abs(mLeft);
            }
            else
            {
                return Math.Abs(mRight);
            }
        }

        /**
         * Creates a new complex sample representing the angle with unit circle
         * magnitude
         * @param angle in radians
         * @return
         */
        public static ComplexSample fromAngle(float angle)
        {
            return new ComplexSample((float)Math.Cos(angle),
                                      (float)Math.Sin(angle));
        }
    }
}

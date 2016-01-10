using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.DSP.Filter
{
    public class FloatFIRFilter : IListener<float>, IListenerOutput<float>
    {
        private IListener<float> mListener;
        private List<float> mBuffer;
        private int mBufferSize = 1; //Temporary initial value
        private int mBufferPointer = 0;
        private double[] mCoefficients;
        private int[][] mIndexMap;
        private int mCenterCoefficient;
        private int mCenterCoefficientMapIndex;
        private double mGain;

        public FloatFIRFilter(double[] coefficients, double gain)
        {
            mCoefficients = coefficients;
            mBuffer = new List<float>();
            mBufferSize = mCoefficients.Length;
            mGain = gain;

            //Fill the buffer with zero valued samples, so we don't have to check for null
            for (int x = 0; x < mCoefficients.Length; x++)
            {
                mBuffer.Add(0.0f);
            }

            generateIndexMap(mCoefficients.Length);
        }

        public void SetOutputListener(IListener<float> outListener)
        {
            mListener = outListener;
        }

        public void RemoveOutputListener()
        {
            mListener = null;
        }

        private void generateIndexMap(int size)
        {
            mIndexMap = new int[size][];
            for (int i = 0; i < size; i++)
            {
                mIndexMap[i] = new int[size];
            }

            //Last column will be the center coefficient index value for each row
            mCenterCoefficientMapIndex = size - 1;
            mCenterCoefficient = (int)(size / 2);

            //Setup the first row.  Offset is the first row's center coefficient value
            //and will become the index value we place in the last column
            mIndexMap[0][mCenterCoefficient] = mCenterCoefficient;

            //Indexes 0 to 1/2 are their index value, and indexes 1/2 to end are
            //an offset of the first half of the values
            for (int x = 0; x < mCenterCoefficient; x++)
            {
                mIndexMap[0][x] = x;
                mIndexMap[0][x + mCenterCoefficient] = size - 1 - x;
            }

            //For each subsequent map row, increment the value of the preceding row
            //same column by 1, wrapping to zero when we exceed the size value
            for (int x = 1; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    mIndexMap[x][y] = mIndexMap[x - 1][y] + 1;

                    if (mIndexMap[x][y] >= size)
                    {
                        mIndexMap[x][y] = 0;
                    }
                }
            }
        }

        public void Receive(float value)
        {
            send(get(value));
        }

        private void send(float sample)
        {
            if (mListener != null)
            {
                mListener.Receive(sample);
            }
        }
        public float get(float newSample)
        {
            //Add the new sample to the buffer
            mBuffer[mBufferPointer]= newSample;

            //Increment & Adjust the buffer pointer for circular wrap around
            mBufferPointer++;

            if (mBufferPointer >= mBufferSize)
            {
                mBufferPointer = 0;
            }

            //Convolution - multiply filter coefficients by the circular buffer 
            //samples to calculate a new filtered value
            double accumulator = 0.0d;

            //Start with the center tap value
            accumulator += mCoefficients[mCenterCoefficient] *
                    mBuffer[mIndexMap[mBufferPointer][mCenterCoefficientMapIndex]];

            //For the remaining coefficients, add the symmetric samples, oldest and newest
            //first, then multiply by the single coefficient
            for (int x = 0; x < mCenterCoefficient; x++)
            {
                accumulator += mCoefficients[x] *
                    (mBuffer[mIndexMap[mBufferPointer][x]] +
                      mBuffer[mIndexMap[mBufferPointer][x + mCenterCoefficient]]);
            }

            //We're almost finished ... apply gain, cast the doubles to shorts and
            //return the value
            return (float)(accumulator * mGain);
        }
    }
}

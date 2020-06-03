using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.DSP.Filter
{
    public class FloatHalfBandFilter : IListener<float>, IListenerOutput<float>
    {
        private IListener<float> mListener;
        private List<float> mBuffer;
        private int mBufferSize = 1; //Temporary initial value
        private int mBufferPointer = 0;
        private double mGain;
        private double[] mCoefficients;
        private int[][] mIndexMap;
        private int mCenterCoefficient;
        private int mCenterCoefficientMapIndex;
        private bool mDispatchFlag = false;

        public FloatHalfBandFilter(Filters filter, double gain)
        {
            mCoefficients = FilterCoefficientHelper.getCoefficients(filter);
            mBuffer = new List<float>();
            mBufferSize = mCoefficients.Length;

            //Fill the buffer with zero valued samples
            for (int x = 0; x < mCoefficients.Length; x++)
            {
                mBuffer.Add(0.0f);
            }

            generateIndexMap(mCoefficients.Length);
            mGain = gain;
        }

        public void Receive(float newSample)
        {
            //Add the new sample to the buffer
            mBuffer[mBufferPointer] = newSample;

            //Increment & Adjust the buffer pointer for circular wrap around
            mBufferPointer++;

            if (mBufferPointer >= mBufferSize)
            {
                mBufferPointer = 0;
            }

            //Toggle the flag every time, so that we can calculate and dispatch a
            //sample every other time, when the flag is true (ie decimate by 2)
            mDispatchFlag = !mDispatchFlag;

            //Calculate a filtered sample when the flag is true
            if (mDispatchFlag)
            {
                //Convolution - multiply filter coefficients by the circular buffer 
                //samples to calculate a new filtered value
                double accumulator = 0;

                //Start with the center tap value
                accumulator += mCoefficients[mCenterCoefficient] *
                        mBuffer[mIndexMap[mBufferPointer][mCenterCoefficientMapIndex]];

                //For the remaining coefficients, add the symmetric samples, oldest and newest
                //first, then multiply by the single coefficient
                for (int x = 0; x < mCenterCoefficientMapIndex; x += 2)
                {
                    accumulator += mCoefficients[x] *
                        (mBuffer[mIndexMap[mBufferPointer][x]] +
                          mBuffer[mIndexMap[mBufferPointer][x + 1]]);
                }

                //We're almost finished ... apply gain, cast the doubles to shorts and
                //send it on it's merry way
                if (mListener != null)
                {
                    mListener.Receive((float)(accumulator * mGain));
                }
            }
        }

        private void generateIndexMap(int size)
        {
            //Ensure we have an odd size
            int mapWidth = ((size + 1) / 2) + 1;

            //Set center tap index for coefficients array
            mCenterCoefficient = (size - 1) / 2;
            mCenterCoefficientMapIndex = mCenterCoefficient + 1;

            mIndexMap = new int[size][];
            for (int i = 0; i < size; i++)
            {
                mIndexMap[i] = new int[mapWidth];
            }

            //Setup the first row, buffer pointer index 0, as a starting point
            for (int x = 0; x < mapWidth - 2; x += 2)
            {
                mIndexMap[0][x] = x;
                mIndexMap[0][x + 1] = size - 1 - x;
            }

            //Place center tap index in last element
            mIndexMap[0][mCenterCoefficientMapIndex] = mCenterCoefficient;

            //For each subsequent row, increment the previous row's value by 1, 
            //subtracting size as needed, to keep the values between 0 and size - 1
            for (int x = 1; x < size; x++)
            {
                for (int y = 0; y < mapWidth; y++)
                {
                    mIndexMap[x][y] = mIndexMap[x - 1][y] + 1;

                    if (mIndexMap[x][y] >= size)
                    {
                        mIndexMap[x][y] -= size;
                    }
                }
            }
        }

        public void SetOutputListener(IListener<float> outListener)
        {
            mListener = outListener;
        }

        public void RemoveOutputListener()
        {
            mListener = null;
        }
    }
}

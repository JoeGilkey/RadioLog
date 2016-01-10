using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Buffer
{
    /**
 * Circular Buffer - implements a circular buffer with a method to get the 
 * current head of the buffer, and put a new value in its place at the same
 * time
 * 
 * Also provides averaging over the elements in the buffer
 */
    public class BooleanAveragingBuffer
    {

        private bool[] mBuffer;
        private int mBufferPointer;

        public BooleanAveragingBuffer(int length)
        {
            mBuffer = new bool[length];

            //Preload the array with false values
            Common.ArrayUtils.Fill(mBuffer, false);
        }

        public bool get(bool newValue)
        {
            //Fetch current boolean value
            bool retVal = mBuffer[mBufferPointer];

            //Load the new value into the buffer
            put(newValue);

            return retVal;
        }

        /**
         * Loads the newValue into this buffer and adjusts the buffer pointer
         * to prepare for the next get/put cycle
         */
        private void put(bool newValue)
        {
            //Store the new value to the buffer
            mBuffer[mBufferPointer] = newValue;

            //Increment the buffer pointer
            mBufferPointer++;

            //Wrap the buffer pointer around to 0 when necessary
            if (mBufferPointer >= mBuffer.Length)
            {
                mBufferPointer = 0;
            }
        }

        /**
         * Loads the newValue into the buffer, calculates the average
         * and returns that average from this method
         * 
         * This effectively performs low-pass filtering
         */
        public bool getAverage(bool newValue)
        {
            //Load the new value into the buffer
            put(newValue);

            int trueCount = 0;

            for (int x = 0; x < mBuffer.Length; x++)
            {
                if (mBuffer[x])
                {
                    trueCount++;
                }
            }

            /**
             * If the number of true values in the buffer is 
             * more than half, return true, otherwise, false
             */
            if (trueCount > (int)(mBuffer.Length / 2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

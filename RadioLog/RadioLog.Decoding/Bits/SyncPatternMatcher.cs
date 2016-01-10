using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RadioLog.Common;

namespace RadioLog.Decoding.Bits
{
    /**
 * Sync pattern matcher.
 * 
 * Note: works for sync patterns up to 63 bits (integer size - 1 ) long.  
 * If a 64 bit or larger sync pattern is applied, then the wrapping feature
 * of the Integer.rotate methods will wrap the MSB around to the LSB and
 * corrupt the matching value.
 *
 */
    public class SyncPatternMatcher
    {
        private long mBits = 0;
        private long mMask = 0;
        private long mSync = 0;

        public SyncPatternMatcher(bool[] syncPattern)
        {
            //Setup a bit mask of all ones the length of the sync pattern
            mMask = (long)((Math.Pow(2, syncPattern.Length)) - 1);

            //Convert the sync bits into a long value for comparison
            for (int x = 0; x < syncPattern.Length; x++)
            {
                if (syncPattern[x])
                {
                    mSync += 1 << (syncPattern.Length - 1 - x);
                }
            }
        }

        public void receive(bool bit)
        {
            //Left shift the previous value
            mBits = mBits.RotateLeft(1);

            //Apply the mask to erase the previous MSB
            mBits &= mMask;

            //Add in the new bit
            if (bit)
            {
                mBits += 1;
            }
        }

        /**
         * Indicates if the most recently received bit sequence matches the 
         * sync pattern
         */
        public bool matches()
        {
            return (mBits == mSync);
        }
    }
}

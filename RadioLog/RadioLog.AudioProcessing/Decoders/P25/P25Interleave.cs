﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders.P25
{
    public class P25Interleave
    {
        private static int[] INTERLEAVE = new int[] { 0,1,2,3,52,53,54,55,100,101,
        102,103,148,149,150,151,4,5,6,7,56,57,58,59,104,105,106,107,152,153,154,
        155,8,9,10,11,60,61,62,63,108,109,110,111,156,157,158,159,12,13,14,15,
        64,65,66,67,112,113,114,115,160,161,162,163,16,17,18,19,68,69,70,71,116,
        117,118,119,164,165,166,167,20,21,22,23,72,73,74,75,120,121,122,123,168,
        169,170,171,24,25,26,27,76,77,78,79,124,125,126,127,172,173,174,175,28,
        29,30,31,80,81,82,83,128,129,130,131,176,177,178,179,32,33,34,35,84,85,
        86,87,132,133,134,135,180,181,182,183,36,37,38,39,88,89,90,91,136,137,
        138,139,184,185,186,187,40,41,42,43,92,93,94,95,140,141,142,143,188,189,
        190,191,44,45,46,47,96,97,98,99,144,145,146,147,192,193,194,195,48,49,
        50,51 };

        private static int[] DEINTERLEAVE = new int[] { 0,1,2,3,16,17,18,19,32,33,
	    34,35,48,49,50,51,64,65,66,67,80,81,82,83,96,97,98,99,112,113,114,115,
		128,129,130,131,144,145,146,147,160,161,162,163,176,177,178,179,192,193,
		194,195,4,5,6,7,20,21,22,23,36,37,38,39,52,53,54,55,68,69,70,71,84,85,
		86,87,100,101,102,103,116,117,118,119,132,133,134,135,148,149,150,151,
		164,165,166,167,180,181,182,183,8,9,10,11,24,25,26,27,40,41,42,43,56,57,
		58,59,72,73,74,75,88,89,90,91,104,105,106,107,120,121,122,123,136,137,
		138,139,152,153,154,155,168,169,170,171,184,185,186,187,12,13,14,15,28,
		29,30,31,44,45,46,47,60,61,62,63,76,77,78,79,92,93,94,95,108,109,110,
		111,124,125,126,127,140,141,142,143,156,157,158,159,172,173,174,175,188,
		189,190,191 };

        public static RadioLog.Common.SafeBitArray deinterleave(RadioLog.Common.SafeBitArray message, int start, int end)
        {
            RadioLog.Common.SafeBitArray original = message.CloneFromIndexToIndex(start, end);

            /* Clear block bits in source message */
            message.ClearRange(start, end);

            /* Iterate only the set bits in the original message and apply 
             * the deinterleave -- we don't have to evaluate the 0 bits */
            for (int i = original.nextSetBit(0);
                     i >= 0 && i < DEINTERLEAVE.Length;
                     i = original.nextSetBit(i + 1))
            {
                message.SetBit(start + DEINTERLEAVE[i]);
            }

            return message;
        }

        public static RadioLog.Common.SafeBitArray interleave(RadioLog.Common.SafeBitArray message, int start, int end)
        {
            RadioLog.Common.SafeBitArray original = message.CloneFromIndexToIndex(start, end);

            /* Clear block bits in source message */
            message.ClearRange(start, end);

            /* Iterate only the set bits in the original message and apply 
             * the deinterleave -- we don't have to evaluate the 0 bits */
            for (int i = original.nextSetBit(0);
                     i >= 0 && i < INTERLEAVE.Length;
                     i = original.nextSetBit(i + 1))
            {
                message.SetBit(start + INTERLEAVE[i]);
            }

            return message;
        }
    }
}

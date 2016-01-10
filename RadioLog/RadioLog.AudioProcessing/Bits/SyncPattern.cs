using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Bits
{
    public enum SyncPattern
    {
        FLEETSYNC1,
        /* Revs (0x0A) and Sync (0x23EB) = 10101 0010001111101011 */
        FLEETSYNC2,

        /**
         * Note: we use a truncated portion of the NRZ-I encoded sync pattern.
         * The truncated version seems to work well enough for detecting the burst, 
         * while limiting falsing.
         * 
         *       Sync: .... .... 0000 0111 0000 1001 0010 1010 0100 0100 0110 1111
         * Rev & Sync: ...1 0101 0000 0111 0000 1001 0010 1010 0100 0100 0110 1111
         *      NRZ-I: .... 1111 1000 0100 1000 1101 1011 1111 0110 0110 0101 1000
         *       This: .... .... 1000 0100 1000 1101 1011 1111 .... .... .... ....
         */
        /* Sync (0x07092A446F) = 0000011100001001001010100100010001101111 */
        MDC1200,
        /* Revs (0xA) and Sync(0xA4D7) = 1010 1100010011010111 */
        MPT1327_CONTROL,

        /**
         * French variant of MPT-1327.  This variant uses the following a different
         * sync pattern:
         * 
         * SYNC: 1011 0100 0011 0011
         */
        MPT1327_CONTROL_FRENCH,

        /* Revs (0xA) and Sync(0x3B28) = 1010 0011101100101000 */
        MPT1327_TRAFFIC,

        /**
         * French variant of MPT-1327.  This variant uses the following a different
         * sync pattern:
         * 
         * SYNT: 0100 1011 1100 1100
         */
        MPT1327_TRAFFIC_FRENCH,

        /* Sync (0x158) = 1101011000 */
        PASSPORT,
        LTR_STANDARD_OSW,
        LTR_STANDARD_ISW
    }

    public static class SyncPatternHelper
    {
        public static bool[] getPattern(SyncPattern pattern)
        {
            switch (pattern)
            {
                case SyncPattern.FLEETSYNC1: return new bool[] 
	{ 
		false, true, false, true, false,     //end of revs
		false, true, true, true,             //0111 0x7 
		false, true, true, false,            //0110 0x6
		false, true, false, true,            //0101 0x5
		false, false, false, false           //0000 0x0
	};
                case SyncPattern.FLEETSYNC2: return new bool[] 
	{ 
		false, true, false, true, false,	//End of bit revs
		false, false, true, false,          //0010 0x2 
		false, false, true, true,           //0011 0x3
		true, true, true, false,            //1110 0xE
		true, false, true, true             //1011 0xB
	};
                case SyncPattern.MDC1200: return new bool[]
	{
		false, false, false, false,//0000 0x0
		false, true, true, true,   //0111 0x7
		false, false, false, false,//0000 0x0
		true, false, false, true,  //1001 0x9
		false, false, true, false, //0010 0x2
		true, false, true, false,  //1010 0xA
		false, true, false, false, //0100 0x4
		false, true, false, false, //0100 0x4
		false, true, true, false,  //0110 0x6
		true, true, true, true     //1111 0xF
	};
                case SyncPattern.MPT1327_CONTROL: return new bool[]
	{
		true, false, true, false,   //Includes 4 of 16 rev bits
		true, true, false, false,   //1100 0xA
		false, true, false, false,  //0100 0x4
		true, true, false, true,    //1101 0xD
		false, true, true, true     //0111 0x7
	};
                case SyncPattern.MPT1327_CONTROL_FRENCH: return new bool[]
	{
		true, false, true, false,   //Includes final 4 of 16 rev bits
		true, false, true, true,    //1011 0xB
		false, true, false, false,  //0100 0x4
		false, false, true, true,   //0011 0x3
		false, false, true, true    //0011 0x3
	};
                case SyncPattern.MPT1327_TRAFFIC: return new bool[]
	{
		true, false, true, false,   //1010 Includes 4 of 16 rev bits
		false, false, true, true,   //0011 0x3
		true, false, true, true,    //1011 0xB
		false, false, true, false,  //0010 0x2
		true, false, false, false   //1000 0x8
	};
                case SyncPattern.MPT1327_TRAFFIC_FRENCH: return new bool[]
	{
		true, false, true, false,   //Includes final 4 of 16 rev bits
		false, true, false, false,  //0100 0x4
		true, false, true, true,    //1011 0xB
		true, true, false, false,   //1100 0xC
		true, true, false, false   //1100 0xC
	};
                case SyncPattern.PASSPORT: return new bool[] 
	{
		true, 						//0001 0x1
		false, true, false, true, 	//0101 0x5
		true, false, false, false	//1000 0x8
	};
                case SyncPattern.LTR_STANDARD_OSW: return new bool[] 
	{
		true,false,true,false,true,true,false,false,false
	};
                case SyncPattern.LTR_STANDARD_ISW: return new bool[] 
	{
		false, true, false, true,false, false, true, true, true
	};
            }
            return null;
        }
    }
}
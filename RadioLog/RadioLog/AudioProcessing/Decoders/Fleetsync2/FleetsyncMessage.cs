using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders.Fleetsync2
{
    public class FleetsyncMessage:Message
    {
        //Message Header
        private static int[] sRevs = { 4, 3, 2, 1, 0 };
        private static int[] sSync = { 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5 };

        //Message Block 1
        private static int[] sStatusMessage = { 27, 26, 25, 24, 23, 22, 21 };
        private static int[] sMessageType = { 33, 32, 31, 30, 29 };

        private static int sEmergencyFlag = 22;
        private static int sLoneWorkerFlag = 24;
        private static int sPagingFlag = 26;
        private static int sEndOfTransmissionFlag = 27;
        private static int sManualFlag = 28;
        private static int sANIFlag = 29;
        private static int sStatusFlag = 30;
        private static int sAcknowledgeFlag = 31;
        //32 - unknown - always 0
        //33 - unknown - always 0
        //34 - unknown - set for ACKNOWLEDGE
        private static int sGPSExtensionFlag = 35;
        private static int sFleetExtensionFlag = 36;
        private static int[] sFleetFrom = { 44, 43, 42, 41, 40, 39, 38, 37 };
        private static int[] sIdentFrom = { 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45 };
        private static int[] sIdentTo = { 68, 67, 66, 65, 64, 63, 62, 61, 60, 59, 58, 57 };
        private static int[] sCRC1 = { 84, 83, 82, 81, 80, 79, 78, 77, 76, 75, 74, 73, 72, 71, 70, 69 };

        //Message Block 2
        private static int[] sFleetTo = { 92, 91, 90, 89, 88, 87, 86, 85 };
        private static int[] sCRC2 = { 148, 147, 146, 145, 144, 143, 142, 141, 140, 139, 138, 137, 136, 135, 134, 132 };

        //Message Block 3
        private static int[] sGPSHours = { 176, 175, 174, 173, 172 };
        private static int[] sGPSMinutes = { 182, 181, 180, 179, 178, 177 };
        private static int[] sGPSSeconds = { 188, 187, 186, 185, 184, 183 };
        private static int[] sCRC3 = { 212, 211, 210, 209, 208, 207, 206, 205, 204, 203, 202, 201, 200, 199, 198, 197 };

        //Message Block 4
        private static int[] sGPSChecksum = { 220, 219, 218, 217, 216, 215, 214, 213 };
        private static int[] sLatitudeDegreesMinutes = { 236, 235, 234, 233, 232, 231, 230, 229, 228, 227, 226, 225, 224, 223, 222, 221 };
        private static int[] sLatitudeDecimalMinutes = { 251, 250, 249, 248, 247, 246, 245, 244, 243, 242, 241, 240, 239, 238 };
        private static int[] sSpeed = { 276, 275, 274, 273, 272, 271, 270, 269, 268, 267, 266, 265, 264, 263, 262, 261, 260, 259, 258, 257, 256, 255, 254, 253, 252 };
        private static int[] sCRC4 = { 276, 275, 274, 273, 272, 271, 270, 269, 268, 267, 266, 265, 264, 263, 262, 261 };

        //Message Block 5
        private static int[] sGPSCentury = { 284, 283, 282, 281, 280, 279, 278, 277 };
        private static int[] sGPSYear = { 291, 290, 289, 288, 287, 286, 285 };
        private static int[] sGPSMonth = { 295, 294, 293, 292 };
        private static int[] sGPSDay = { 300, 299, 298, 297, 296 };
        private static int[] sLongitudeDegreesMinutes = { 316, 315, 314, 313, 312, 311, 310, 309, 308, 307, 306, 305, 304, 303, 302, 301 };
        private static int[] sLongitudeDecimalMinutes = { 331, 330, 329, 328, 327, 326, 325, 324, 323, 322, 321, 320, 319, 318 };
        private static int[] sCRC5 = { 340, 339, 338, 337, 336, 335, 334, 333, 332, 331, 330, 329, 328, 327, 326, 325 };

        //Message Block 6
        private static int[] sGPSUnknown1 = { 352, 351, 350, 349 };
        private static int[] sGPSHeading = { 365, 363, 362, 361, 360, 359, 358, 357, 356, 355, 354, 353 };
        private static int[] sCRC6 = { 404, 403, 402, 401, 400, 399, 398, 397, 396, 395, 394, 393, 392, 391, 390, 389 };

        //Message Block 7
        private static int[] sBLK7WhatIsIt = { 444, 443, 442, 441, 440, 439, 438, 437, 436, 435, 434, 433, 432, 431, 430, 429 };
        private static int[] sCRC7 = { 468, 467, 466, 465, 464, 463, 462, 461, 460, 459, 458, 457, 456, 455, 454, 453 };

        //Message Block 8
        private static int[] sGPSSpeed = { 491, 490, 489, 488, 487, 486, 485, 484 };
        private static int[] sGPSSpeedFractional = { 499, 498, 497, 496, 495, 494, 493, 492 };
        private static int[] sCRC8 = { 532, 531, 530, 529, 528, 527, 526, 525, 524, 523, 522, 521, 520, 519, 518, 517 };

        private RadioLog.Common.SafeBitArray mMessage;
        private CRC[] mCRC = new CRC[8];

        public FleetsyncMessage(RadioLog.Common.SafeBitArray message)
        {
            mMessage = message;
            checkParity();
        }

        private void checkParity()
        {
            //Check message block 1
            mCRC[0] = detectAndCorrect(21, 85);

            //Only check subsequent blocks if we know block 1 is correct
            if (mCRC[0] == CRC.PASSED || mCRC[0] == CRC.CORRECTED)
            {
                if (hasFleetExtensionFlag())
                {
                    //Check message block 2
                    mCRC[1] = detectAndCorrect(85, 149);
                }

                if (hasGPSFlag())
                {
                    //Check message block 3
                    mCRC[2] = detectAndCorrect(149, 213);
                    //Check message block 4
                    mCRC[3] = detectAndCorrect(213, 277);
                    //Check message block 5
                    mCRC[4] = detectAndCorrect(277, 341);
                    //Check message block 6
                    mCRC[5] = detectAndCorrect(341, 405);
                    //Check message block 7
                    mCRC[6] = detectAndCorrect(405, 469);
                    //Check message block 8
                    mCRC[7] = detectAndCorrect(469, 533);
                }
            }
        }

        private CRC detectAndCorrect(int start, int end)
    {
    	RadioLog.Common.SafeBitArray original = mMessage.CloneFromIndexToIndex(start,end);
    	
    	CRC retVal = CRCFleetsync.check( original );
    	
    	//Attempt to correct single-bit errors
    	if( retVal == CRC.FAILED_PARITY )
    	{
    		int[] errorBitPositions = CRCFleetsync.findBitErrors( original );
    		
    		if( errorBitPositions != null )
    		{
                foreach(int errorBitPosition in errorBitPositions){
                    mMessage.flip(start+errorBitPosition);
                }
    			
    			retVal = CRC.CORRECTED;
    		}
    	}
    	
    	return retVal;
    }

        public override bool isValid()
        {
            bool valid = true;

            for (int x = 0; x < mCRC.Length; x++)
            {
                CRC crc = mCRC[x];

                if (crc != null)
                {
                    if (crc == CRC.FAILED_CRC ||
                        crc == CRC.FAILED_PARITY ||
                        crc == CRC.UNKNOWN)
                    {
                        valid = false;
                    }
                }
            }

            return valid;
        }

        public FleetsyncMessageType getMessageType()
        {
            if (hasStatusFlag())
            {
                if (hasAcknowledgeFlag())
                {
                    return FleetsyncMessageType.ACKNOWLEDGE;
                }

                if (hasGPSFlag())
                {
                    return FleetsyncMessageType.GPS;
                }
                else
                {
                    return FleetsyncMessageType.STATUS;
                }
            }
            else
            {
                if (hasAcknowledgeFlag())
                {
                    return FleetsyncMessageType.ACKNOWLEDGE;
                }

                if (hasANIFlag())
                {
                    return FleetsyncMessageType.ANI;
                }

                if (hasGPSFlag())
                {
                    return FleetsyncMessageType.GPS;
                }

                if (hasPagingFlag())
                {
                    return FleetsyncMessageType.PAGING;
                }

                if (hasEmergencyFlag())
                {
                    if (hasLoneWorkerFlag())
                    {
                        return FleetsyncMessageType.LONE_WORKER_EMERGENCY;
                    }
                    else
                        return FleetsyncMessageType.EMERGENCY;
                }
            }

            return FleetsyncMessageType.UNKNOWN;
        }

        public int getStatus()
        {
            return getStatusNumber() + 9;
        }

        /**
         * Returns the RAW status number.  The actual status number should be
         * accessed via the getStatus() method.
         * @return
         */
        public int getStatusNumber()
        {
            return getInt(sStatusMessage);
        }

        public int getFleetFrom()
        {
            return getInt(sFleetFrom) + 99;
        }

        public int getIdentifierFrom()
        {
            return getInt(sIdentFrom) + 999;
        }

        /**
         * Inverted Flags - 0 = flag is true
         * @return
         */
        public bool hasEndOfTransmissionFlag()
        {
            return !mMessage[sEndOfTransmissionFlag];
        }

        public bool hasEmergencyFlag()
        {
            return !mMessage[sEmergencyFlag];
        }

        public bool hasLoneWorkerFlag()
        {
            return !mMessage[sLoneWorkerFlag];
        }

        public bool hasPagingFlag()
        {
            return !mMessage[sPagingFlag];
        }

        public bool hasANIFlag()
        {
            return mMessage[sANIFlag];
        }

        public bool hasAcknowledgeFlag()
        {
            return mMessage[sAcknowledgeFlag];
        }

        public bool hasFleetExtensionFlag()
        {
            return mMessage[sFleetExtensionFlag];
        }

        public bool hasGPSFlag()
        {
            return mMessage[sGPSExtensionFlag];
        }

        public bool hasStatusFlag()
        {
            return mMessage[sStatusFlag];
        }

        public int getFleetTo()
        {
            if (hasFleetExtensionFlag())
            {
                return getInt(sFleetTo) + 99;
            }
            else
            {
                return getInt(sFleetFrom) + 99;
            }
        }

        public int getIdentifierTo()
        {
            return getInt(sIdentTo) + 999;
        }

        public double getHeading()
        {
            double retVal = 0.0;

            int heading = getInt(sGPSHeading);

            if (heading != 4095)
            {
                retVal = (double)(heading / 10.0);
            }

            return retVal;
        }

        public int getBlock7WhatIsIt()
        {
            int value = getInt(sBLK7WhatIsIt);

            if (value == 65535)
            {
                value = -1;
            }

            return value;
        }

        public double getSpeed()
        {
            double retVal = 0.0;

            int temp = getInt(sSpeed);

            if (temp != 0)
            {
                retVal = (double)temp / 1000.0D;
            }
            return retVal;
        }

        public double getLatitude()
        {
            //TODO: determine the correct hemisphere indicator and replace this
            //hardcoded "0" with the correct hemisphere value

            return convertDDMToDD(0,
                                   getInt(sLatitudeDegreesMinutes),
                                   getInt(sLatitudeDecimalMinutes));
        }

        public double getLongitude()
        {
            //TODO: determine the correct hemisphere indicator and replace this
            //hardcoded "1" with the correct hemisphere value

            return convertDDMToDD(1,
                       getInt(sLongitudeDegreesMinutes),
                       getInt(sLongitudeDecimalMinutes));
        }

        public double convertDDMToDD(int hemisphere, int degreesMinutes, int decimalDegrees)
        {
            double retVal = 0.0;

            if (degreesMinutes != 0)
            {
                //Degrees - divide value by 100 and retain the whole number value (ie degrees)
                retVal += (double)(degreesMinutes / 100);

                //Minutes - modulus by 100 to get the whole minutes value
                int wholeMinutes = degreesMinutes % 100;

                if (wholeMinutes != 0)
                {
                    retVal += (double)(wholeMinutes / 60.0D);
                }
            }

            if (decimalDegrees != 0)
            {
                //Fractional Minutes - divide by 10,000 to get the decimal place correct
                //then divide by 60 (minutes) to get the decimal value
                //10,000 * 60 = 600,000
                retVal += (double)(decimalDegrees / 600000.0D);
            }

            //Adjust the value +/- for the hemisphere
            if (hemisphere == 1) //South and West values
            {
                retVal = -retVal;
            }

            return retVal;
        }

        public int getGPSChecksum()
        {
            return getInt(sGPSChecksum);
        }

        public int getGPSDay()
        {
            return getInt(sGPSDay) + 1;
        }

        public int getGPSMonth()
        {
            return getInt(sGPSMonth);
        }

        public int getGPSYear()
        {
            //    	return ( ( getInt( sGPSCentury ) + 1 ) * 100 ) + getInt( sGPSYear );
            return (2000) + getInt(sGPSYear);
        }

        private int getInt(int[] bits)
        {
            int retVal = 0;

            for (int x = 0; x < bits.Length; x++)
            {
                if (mMessage[bits[x]])
                {
                    retVal += 1 << x;
                }
            }

            return retVal;
        }

        public override string GetFormatName()
        {
            return RadioLog.Common.SignalingNames.FLEET_SYNC_2;
        }
        public override string GetDescription()
        {
            switch (getMessageType())
            {
                case FleetsyncMessageType.ANI: return "ANI";
                case FleetsyncMessageType.EMERGENCY: return "*** EMERGENCY ***";
                case FleetsyncMessageType.LONE_WORKER_EMERGENCY: return "*** LONE WORKER EMERGENCY ***";
                case FleetsyncMessageType.ACKNOWLEDGE: return "ACK";
                case FleetsyncMessageType.PAGING: return string.Format("PAGING: {0}-{1}", getFleetTo(), getIdentifierTo());
                case FleetsyncMessageType.STATUS: return string.Format("STATUS: {0}", getStatus());
                case FleetsyncMessageType.GPS: return string.Format("GPS: LAT={0}, LON={1}", getLatitude(), getLongitude());
                default: return string.Format("MESSAGE TYPE: {0}", getMessageType());
            }
        }
        public override string GetUnitId()
        {
            return string.Format("{0}-{1}", getFleetFrom(), getIdentifierFrom());
        }
        public override Common.SignalCode GetSignalCode()
        {
            switch (getMessageType())
            {
                case FleetsyncMessageType.ANI: return Common.SignalCode.PTT;
                case FleetsyncMessageType.EMERGENCY: return Common.SignalCode.Emergency;
                case FleetsyncMessageType.GPS: return Common.SignalCode.Generic;
                case FleetsyncMessageType.ACKNOWLEDGE: return Common.SignalCode.Generic;
                case FleetsyncMessageType.LONE_WORKER_EMERGENCY: return Common.SignalCode.Emergency;
                case FleetsyncMessageType.PAGING: return Common.SignalCode.PTT;
                case FleetsyncMessageType.STATUS: return Common.SignalCode.Generic;
                default: return Common.SignalCode.Generic;
            }
        }
    }
}

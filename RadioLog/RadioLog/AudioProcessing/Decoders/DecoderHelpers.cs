using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioLog.AudioProcessing.Decoders
{
    internal static class DecoderHelpers
    {
        public const double TWOPI = (2.0 * Math.PI);

        /**
         * Methods
         */
        /// <summary>
        /// Helper function to flip the given number of bits in the given CRC.
        /// </summary>
        /// <param name="crc">CRC to flip</param>
        /// <param name="bitnum">Number of bits to flip</param>
        /// <returns>Flipped CRC</returns>
        public static ushort MDC1200_Flip(ushort crc, int bitnum)
        {
            ushort crcout, i, j;

            j = 1;
            crcout = 0;

            // iterate through the bits of the CRC flipping them
            for (i = (ushort)(1 << (bitnum - 1)); i > 0; i >>= 1) /* not sure what boolean value the middle element is supposed to check .. was i; */
            {
                if ((crc & i) > 0) /* not sure what boolean value is supposed to be checked, assuming non-zero */
                    crcout |= j;
                j <<= 1;
            }
            return (crcout);
        }

        /// <summary>
        /// Helper function to compute the CRC of the given byte array.
        /// </summary>
        /// <param name="p">Byte array to compute CRC for</param>
        /// <param name="len">Length of array</param>
        /// <returns>CRC of byte array</returns>
        public static ushort MDC1200_ComputeCRC(byte[] p, int len)
        {
            int i, j;
            ushort c;
            int bit;
            ushort crc = 0x0000;

            // iterate through the length of the byte array
            for (i = 0; i < len; i++)
            {
                c = (ushort)p[i];
                c = MDC1200_Flip(c, 8);

                for (j = 0x80; j > 0; j >>= 1) /* not sure what boolean value the middle element is supposed to check .. was j; */
                {
                    bit = crc & 0x8000;
                    crc <<= 1;
                    if ((c & j) > 0) /* not sure what boolean value is supposed to be checked, assuming non-zero */
                        bit ^= 0x8000;
                    if (bit > 0) /* not sure what boolean value is suppoesd to be checked, assuming non-zero */
                        crc ^= 0x1021;
                }
            }

            crc = MDC1200_Flip(crc, 16);
            crc ^= 0xFFFF;
            crc &= 0xFFFF;

            return (crc);
        }

        public static uint STAR_ComputeCDC(uint input)
        {
            uint crcsr = 0x2a;
            int bit;
            uint cur;
            int inv;
            for (bit = 6; bit < (21 + 6); bit++)
            {
                cur = (input >> (32 - bit)) & 0x01;
                inv = (int)(cur ^ (0x01 & (crcsr >> 5)));
                crcsr <<= 1;
                if (inv > 0)
                    crcsr ^= 0x2f;
            }
            return crcsr & 0x3f;
        }
    }
}

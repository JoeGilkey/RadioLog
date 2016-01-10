using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.DSP.Filter
{
    public class Window
    {
        public static double[] getWindow(WindowType type, int length)
        {
            switch (type)
            {
                case WindowType.BLACKMAN:
                    return getBlackmanWindow(length);
                case WindowType.COSINE:
                    return getCosineWindow(length);
                case WindowType.HAMMING:
                    return getHammingWindow(length);
                case WindowType.HANNING:
                    return getHanningWindow(length);
                case WindowType.NONE:
                default:
                    return getRectangularWindow(length);
            }
        }

        public static double[] getRectangularWindow(int length)
        {
            double[] coefficients = new double[length];

            RadioLog.Common.ArrayUtils.Fill<double>(coefficients, 1.0d);

            return coefficients;
        }

        public static double[] getCosineWindow(int length)
        {
            double[] coefficients = new double[length];


            if (length % 2 == 0) //Even length
            {
                int half = (int)((length - 1) / 2);

                for (int x = -half; x < length / 2 + 1; x++)
                {
                    coefficients[x + half] = Math.Cos(
                            ((double)x * Math.PI) / ((double)length + 1.0d));
                }
            }
            else //Odd length
            {
                int half = (int)length / 2;

                for (int x = -half; x < half + 1; x++)
                {
                    coefficients[x + half] = Math.Cos(
                            ((double)x * Math.PI) / ((double)length + 1.0d));
                }
            }

            return coefficients;
        }

        public static double[] getBlackmanWindow(int length)
        {
            double[] coefficients = new double[length];

            for (int x = 0; x < length; x++)
            {
                coefficients[x] = .426591D -
                      (.496561D * Math.Cos((Math.PI * 2.0D * (double)x) / (double)(length - 1)) +
                      (.076848D * Math.Cos((Math.PI * 4.0D * (double)x) / (double)(length - 1))));
            }

            return coefficients;
        }

        public static double[] getHammingWindow(int length)
        {
            double[] coefficients = new double[length];

            if (length % 2 == 0) //Even length
            {
                for (int x = 0; x < length; x++)
                {
                    coefficients[x] = .54D - (.46D *
                        Math.Cos((Math.PI * (2.0D * (double)x + 1.0d)) / (double)(length - 1)));
                }
            }
            else //Odd length
            {
                for (int x = 0; x < length; x++)
                {
                    coefficients[x] = .54D - (.46D *
                            Math.Cos((2.0D * Math.PI * (double)x) / (double)(length - 1)));
                }
            }

            return coefficients;
        }

        public static double[] getHanningWindow(int length)
        {
            double[] coefficients = new double[length];

            if (length % 2 == 0) //Even length
            {
                for (int x = 0; x < length; x++)
                {
                    coefficients[x] = .5D - (.5D *
                            Math.Cos((Math.PI * (2.0D * (double)x + 1)) / (double)(length - 1)));
                }
            }
            else //Odd length
            {
                for (int x = 0; x < length; x++)
                {
                    coefficients[x] = .5D - (.5D *
                            Math.Cos((2.0D * Math.PI * (double)x) / (double)(length - 1)));
                }
            }


            return coefficients;
        }

        public static float[] apply(double[] coefficients, float[] samples)
        {
            for (int x = 0; x < coefficients.Length; x++)
            {
                samples[x] = (float)(samples[x] * coefficients[x]);
            }

            return samples;
        }

        public static float[] apply(WindowType type, float[] samples)
        {
            double[] coefficients = getWindow(type, samples.Length);

            return apply(coefficients, samples);
        }

        public static double[] apply(double[] coefficients, double[] samples)
        {
            for (int x = 0; x < coefficients.Length; x++)
            {
                samples[x] = samples[x] * coefficients[x];
            }

            return samples;
        }

        public static double[] apply(WindowType type, double[] samples)
        {
            double[] coefficients = getWindow(type, samples.Length);

            return apply(coefficients, samples);
        }
    }
}

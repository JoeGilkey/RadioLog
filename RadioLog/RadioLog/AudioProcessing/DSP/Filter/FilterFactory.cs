using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.DSP.Filter
{
    public class FilterFactory
    {
        public static double[] getLowPass(int sampleRate, long cutoff, int filterLength, WindowType windowType)
        {
            if (filterLength % 2 == 0) //even length
            {
                double[] values = getSinc(sampleRate, cutoff, filterLength + 2, windowType);

                //throw away the 0 index and the last index
                double[] rslt = new double[values.Length - 2];
                Array.Copy(values, 1, rslt, 0, rslt.Length);
                return rslt;
            }
            else
            {
                double[] values = getSinc(sampleRate, cutoff, filterLength + 1, windowType);

                //throw away the 0 index
                double[] rslt = new double[values.Length - 1];
                Array.Copy(values, 1, rslt, 0, rslt.Length);
                return rslt;
            }
        }

        public static double[] getUnityResponseArray(int sampleRate, long frequency, int length)
        {
            double[] unityArray = new double[length * 2];

            int binCount = (int)((Math.Round((double)frequency / (double)sampleRate * (double)length)));

            unityArray[0] = 1.0d;

            for (int x = 1; x <= binCount; x++)
            {
                unityArray[x] = 1.0d;
                unityArray[length - x] = 1.0d;
            }
            return unityArray;
        }

        public static double[] getSinc(int sampleRate, long frequency, int length, WindowType window)
        {
            //Get unity response array (one element longer to align with IDFT size)
            double[] frequencyResponse = getUnityResponseArray(sampleRate, frequency, length + 1);

            //Apply Inverse DFT against frequency response unity values, leaving the
            //IDFT bin results in the frequency response array
            FFTHelper idft = new FFTHelper();
            idft.RealFFT(frequencyResponse, false);
            /*
            DoubleFFT_1D idft = new DoubleFFT_1D(length + 1);
            idft.realInverseFull(frequencyResponse, false);
            */
              
            //Transfer the IDFT results to the return array
            double[] coefficients = new double[length];
            int middleCoefficient = (int)(length / 2);

            //Bin 0 of the idft is our center coefficient
            coefficients[middleCoefficient] = frequencyResponse[0];

            //The remaining idft bins from 1 to (middle - 1) are the mirror image
            //coefficients
            for (int x = 1; x < middleCoefficient; x++)
            {
                coefficients[middleCoefficient + x] = frequencyResponse[2 * x];
                coefficients[middleCoefficient - x] = frequencyResponse[2 * x];
            }

            //Apply the window against the coefficients
            coefficients = Window.apply(window, coefficients);

            //Normalize to unity (1) gain
            coefficients = normalize(coefficients);

            return coefficients;
        }

        private static double[] normalize(double[] coefficients)
        {
            double accumulator = 0;

            for (int x = 0; x < coefficients.Length; x++)
            {
                accumulator += Math.Abs(coefficients[x]);
            }

            for (int x = 0; x < coefficients.Length; x++)
            {
                coefficients[x] = coefficients[x] / accumulator;
            }

            return coefficients;
        }
    }
}

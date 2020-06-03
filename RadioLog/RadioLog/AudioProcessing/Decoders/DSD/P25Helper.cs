using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders.DSD
{
    public class P25Helper
    {
        internal const int P25_HEURISTICS_SIZE = 200;
        internal const int MIN_ELEMENTS_FOR_HEURISTICS = 10;

        public static int use_previous_dibit(int rf_mod)
        {
            // 0: C4FM modulation
            // 1: QPSK modulation
            // 2: GFSK modulation

            // Use previoud dibit information when on C4FM
            return (rf_mod == 0) ? 1 : 0;
        }

        public static void update_error_stats(P25Heuristics heuristics, int bits, int errors)
        {
            heuristics.bit_count += (uint)bits;
            heuristics.bit_error_count += (uint)errors;

            // Normalize to avoid overflow in the counters
            if ((heuristics.bit_count & 1) == 0 && (heuristics.bit_error_count & 1) == 0)
            {
                // We can divide both values by 2 safely. We just care about their ratio, not the actual value
                heuristics.bit_count >>= 1;
                heuristics.bit_error_count >>= 1;
            }
        }

        public static float get_P25_BER_estimate(P25Heuristics heuristics)
        {
            float ber;
            if (heuristics.bit_count == 0)
            {
                ber = 0.0F;
            }
            else
            {
                ber = ((float)heuristics.bit_error_count) * 100.0F / ((float)heuristics.bit_count);
            }
            return ber;
        }

        public static void update_p25_heuristics(P25Heuristics heuristics, int previous_dibit, int original_dibit, int dibit, int analog_value)
        {
            float mean;
            int old_value;
            float old_mean;

            SymbolHeuristics sh;
            int number_errors;

            previous_dibit = 0;

            // Locate the Gaussian (SymbolHeuristics structure) we are going to update
            sh = heuristics.symbols[previous_dibit, dibit];

            // Update the circular buffers of values
            old_value = sh.values[sh.index];
            old_mean = sh.means[sh.index];

            // Update the BER statistics
            number_errors = 0;
            if (original_dibit != dibit)
            {
                if ((original_dibit == 0 && dibit == 3) || (original_dibit == 3 && dibit == 0) ||
                    (original_dibit == 1 && dibit == 2) || (original_dibit == 2 && dibit == 1))
                {
                    // Interpreting a "00" as "11", "11" as "00", "01" as "10" or "10" as "01" counts as 2 errors
                    number_errors = 2;
                }
                else
                {
                    // The other 8 combinations count (where original_dibit != dibit) as 1 error.
                    number_errors = 1;
                }
            }
            update_error_stats(heuristics, 2, number_errors);

            // Update the running mean and variance. This is to calculate the PDF faster when required
            if (sh.count >= P25_HEURISTICS_SIZE)
            {
                sh.sum -= old_value;
                sh.var_sum -= (((float)old_value) - old_mean) * (((float)old_value) - old_mean);
            }
            sh.sum += analog_value;

            sh.values[sh.index] = analog_value;
            if (sh.count < P25_HEURISTICS_SIZE)
            {
                sh.count++;
            }
            mean = sh.sum / ((float)sh.count);
            sh.means[sh.index] = mean;
            if (sh.index >= (P25_HEURISTICS_SIZE - 1))
            {
                sh.index = 0;
            }
            else
            {
                sh.index++;
            }

            sh.var_sum += (((float)analog_value) - mean) * (((float)analog_value) - mean);
        }

        public static float evaluate_pdf(SymbolHeuristics se, int value)
        {
            float x = (se.count * ((float)value) - se.sum);
            float y = -0.5F * x * x / (se.count * se.var_sum);

            float pdf = (float)(Math.Sqrt(se.count / se.var_sum) * Math.Exp(y) / Math.Sqrt(2.0F * ((float)Math.PI)));

            return pdf;
        }

        public static bool estimate_symbol(int rf_mod, P25Heuristics heuristics, int previous_dibit, int analog_value, int dibit)
        {
            bool valid;
            int i;
            float[] pdfs = new float[4];

            int use_prev_dibit = use_previous_dibit(rf_mod);

            if (use_prev_dibit == 0)
            {
                // Ignore
                previous_dibit = 0;
            }

            valid = true;

            // Check if we have enough values to model the Gaussians for each symbol involved.
            for (i = 0; i < 4; i++)
            {
                if (heuristics.symbols[previous_dibit, i].count >= MIN_ELEMENTS_FOR_HEURISTICS)
                {
                    pdfs[i] = evaluate_pdf(heuristics.symbols[previous_dibit, i], analog_value);
                }
                else
                {
                    // Not enough data, we don't trust this result
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                // Find the highest pdf
                int max_index;
                float max;

                max_index = 0;
                max = pdfs[0];
                for (i = 1; i < 4; i++)
                {
                    if (pdfs[i] > max)
                    {
                        max_index = i;
                        max = pdfs[i];
                    }
                }

                // The symbol is the one with the highest pdf
                dibit = max_index;
            }

            return valid;
        }
    }

    public class SymbolHeuristics
    {
        public int[] values;
        public float[] means;
        public int index;
        public int count;
        public float sum;
        public float var_sum;

        public SymbolHeuristics()
        {
            this.values = new int[P25Helper.P25_HEURISTICS_SIZE];
            this.means = new float[P25Helper.P25_HEURISTICS_SIZE];
            this.index = 0;
            this.count = 0;
            this.sum = 0.0f;
            this.var_sum = 0.0f;
        }
    }

    public class P25Heuristics
    {
        public uint bit_count;
        public uint bit_error_count;
        public SymbolHeuristics[,] symbols;

        public P25Heuristics()
        {
            this.bit_count = 0;
            this.bit_error_count = 0;
            this.symbols = new SymbolHeuristics[4, 4];
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    this.symbols[x, y] = new SymbolHeuristics();
                }
            }
        }
    }

    public struct AnalogSignal
    {
        public int value;
        public int dibit;
        public int corrected_dibit;
        public int sequence_broken;
    }
}

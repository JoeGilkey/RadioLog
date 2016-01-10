using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioLog.AudioProcessing
{
    public class WaveFileHelper
    {
        public static void ProcessProvider(NAudio.Wave.ISampleProvider provider, string sourceName)
        {
            if (provider == null)
                return;
            float[] buffer = new float[1024];
            bool bContinue = true;
            int sampleRate = provider.WaveFormat.SampleRate;
            Decoders.MDC1200 mdc = new Decoders.MDC1200(sampleRate, MDCDelegate, sourceName);
            //Decoders.STAR star = new Decoders.STAR(sampleRate, null, RadioLog.AudioProcessing.Decoders.STAR.star_format.star_format_1_16383);
            while (bContinue)
            {
                int iCnt = provider.Read(buffer, 0, buffer.Length);
                bContinue = iCnt > 0;
                if (bContinue)
                {
                    mdc.ProcessSamples(buffer, iCnt, true);
                    //star.star_decoder_process_samples(buffer, iCnt);
                }
            }
        }

        private static void MDCDelegate(Decoders.MDC1200 decoder, RadioLog.Common.SignalCode sigCode, int frameCount, byte op, byte arg, ushort unitID, byte extra0, byte extra1, byte extra2, byte extra3, string opMsg)
        {
            //
        }

        public static void ProcessFile(string fileName)
        {
            try
            {
                string fileExt = System.IO.Path.GetExtension(fileName.ToLower());
                if (fileExt.Contains("mp3"))
                {
                    using (NAudio.Wave.Mp3FileReader rdr = new NAudio.Wave.Mp3FileReader(fileName))
                    {
                        //var newFormat = new NAudio.Wave.WaveFormat(48000, 16, 1);
                        var newFormat = new NAudio.Wave.WaveFormat(16000, 16, 1);
                        using (var conversionStream = new NAudio.Wave.WaveFormatConversionStream(newFormat, rdr))
                        {
                            if (System.IO.File.Exists("mdc1200tmp.wav"))
                                System.IO.File.Delete("mdc1200tmp.wav");
                            NAudio.Wave.WaveFileWriter.CreateWaveFile("mdc1200tmp.wav", conversionStream);
                        }
                    }
                }
                else
                {
                    using (NAudio.Wave.WaveFileReader rdr = new NAudio.Wave.WaveFileReader(fileName))
                    {
                        var newFormat = new NAudio.Wave.WaveFormat(16000, 16, 1);
                        using (var conversionStream = new NAudio.Wave.WaveFormatConversionStream(newFormat, rdr))
                        {
                            if (System.IO.File.Exists("mdc1200tmp.wav"))
                                System.IO.File.Delete("mdc1200tmp.wav");
                            NAudio.Wave.WaveFileWriter.CreateWaveFile("mdc1200tmp.wav", conversionStream);
                        }
                    }
                }
                using (NAudio.Wave.AudioFileReader rdr = new NAudio.Wave.AudioFileReader("mdc1200tmp.wav"))
                {
                    ProcessProvider(rdr, fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Process File Exception: {0}", ex.Message);
            }
        }
    }
}

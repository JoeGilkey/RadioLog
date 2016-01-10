using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;

namespace RadioLog.AudioProcessing.Providers
{
    public class SavingWaveProvider : IWaveProvider, IDisposable
    {
        private readonly IWaveProvider sourceWaveProvider;
        private bool isWriterDisposed;

        public SavingWaveProvider(IWaveProvider sourceWaveProvider, string waveFilePath)
        {
            this.sourceWaveProvider = sourceWaveProvider;
            
        }

        protected virtual void ProcessSamples(byte[] buffer, int offset, int count) { }
        protected virtual void DisposeProcessors() { }

        public int Read(byte[] buffer, int offset, int count)
        {
            var read = sourceWaveProvider.Read(buffer, offset, count);
            try
            {
                if (count > 0 && !isWriterDisposed)
                {
                    ProcessSamples(buffer, offset, count);
                }
                if (count == 0)
                {
                    Dispose();
                }
            }
            catch
            {
                Dispose();
            }
            return read;
        }

        public WaveFormat WaveFormat { get { return sourceWaveProvider.WaveFormat; } }

        public void Dispose()
        {
            if (!isWriterDisposed)
            {
                isWriterDisposed = true;
                DisposeProcessors();
            }
        }
    }
}

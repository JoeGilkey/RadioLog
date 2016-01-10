using System;
using System.IO;

namespace RadioLog.AudioProcessing
{
    public class ReadFullyStream : Stream
    {
        internal static readonly int DEFAULT_READ_TIMEOUT = (int)TimeSpan.FromSeconds(15).TotalMilliseconds;

        private readonly Stream sourceStream;
        private long pos = 0; // psuedo-position
        private readonly byte[] readAheadBuffer;
        private int readAheadLength;
        private int readAheadOffset;

        public ReadFullyStream(Stream sourceStream)
        {
            this.sourceStream = sourceStream;
            try
            {
                if (this.sourceStream.CanTimeout)
                {
                    this.sourceStream.ReadTimeout = DEFAULT_READ_TIMEOUT;
                    //Console.WriteLine("ReadFullyStream.sourceStream.ReadTimeout set!");
                }
            }
            catch { }
            readAheadBuffer = new byte[4096];
        }
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            throw new InvalidOperationException();
        }

        public override long Length
        {
            get { return pos; }
        }

        public override long Position
        {
            get
            {
                return pos;
            }
            set
            {
                throw new InvalidOperationException();
            }
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            while (bytesRead < count)
            {
                int readAheadAvailableBytes = readAheadLength - readAheadOffset;
                int bytesRequired = count - bytesRead;
                if (readAheadAvailableBytes > 0)
                {
                    int toCopy = Math.Min(readAheadAvailableBytes, bytesRequired);
                    Array.Copy(readAheadBuffer, readAheadOffset, buffer, offset + bytesRead, toCopy);
                    bytesRead += toCopy;
                    readAheadOffset += toCopy;
                }
                else
                {
                    readAheadOffset = 0;

                    IAsyncResult arRead = sourceStream.BeginRead(readAheadBuffer, 0, readAheadBuffer.Length, null, null);
                    if (!arRead.AsyncWaitHandle.WaitOne(DEFAULT_READ_TIMEOUT))
                    {
                        throw new TimeoutException("Could not read within the timeout period.");
                    }

                    readAheadLength = sourceStream.EndRead(arRead);

                    //readAheadLength = sourceStream.Read(readAheadBuffer, 0, readAheadBuffer.Length);
                    if (readAheadLength == 0)
                    {
                        break;
                    }
                }
            }
            pos += bytesRead;
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }
    }
}

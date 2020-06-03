using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RadioLog.Common
{
    public static class StreamReadHelper
    {
        static readonly int DEFAULT_READ_TIMEOUT = (int)TimeSpan.FromSeconds(15).TotalMilliseconds;

        public static int ReadStream(Stream stream, byte[] buffer, int offset, int count) { return ReadStream(stream, buffer, offset, count, DEFAULT_READ_TIMEOUT); }
        public static int ReadStream(Stream stream, byte[] buffer, int offset, int count, int timeout)
        {
            if (stream == null || buffer == null || buffer.Length < (offset + count))
                return -1;
            IAsyncResult arRead = stream.BeginRead(buffer, offset, count, null, null);
            if (!arRead.AsyncWaitHandle.WaitOne(timeout))
            {
                throw new TimeoutException("Could not read within the timeout period.");
            }
            return stream.EndRead(arRead);
        }
    }
}

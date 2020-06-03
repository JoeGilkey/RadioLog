using System;
using System.Collections.Generic;
using System.IO;

namespace RadioLog.AudioProcessing
{
    public class IcyStream:Stream
    {
        private readonly Stream sourceStream;
        private long pos = 0; // psuedo-position
        private int metaInt;
        private int receivedBytes = 0;
        private IcyStreamProcessMetaDelegate metaDelegate;

        public IcyStream(Stream src, int icyMetaInt, IcyStreamProcessMetaDelegate metaProcessor)
        {
            this.sourceStream = src;
            this.metaInt = icyMetaInt;
            this.metaDelegate = metaProcessor;
        }

        public override bool CanTimeout { get { return this.sourceStream.CanTimeout; } }
        public override int ReadTimeout
        {
            get
            {
                return this.sourceStream.ReadTimeout;
            }
            set
            {
                this.sourceStream.ReadTimeout = value;
            }
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

        protected void ParseMetaInfo(byte[] metaInfo)
        {
#if DEBUG_ICY_META
            string strMetaDebug=string.Empty;
            if (metaInfo == null || metaInfo.Length <= 0)
            {
                strMetaDebug = "ICY-META RECEIVED: EMPTY META PACKET";
            }
            else
            {
                try
                {
                    strMetaDebug = string.Format("ICY-META RECEIVED: {0} ({1})", Common.PacketUtils.BytesToHexStr(metaInfo), System.Text.Encoding.ASCII.GetString(metaInfo));
                }
                catch (Exception ex)
                {
                    strMetaDebug = string.Format("ICY-META RECEIVED: {0}, Error:{1}", Common.PacketUtils.BytesToHexStr(metaInfo), Common.DebugHelper.GetFullExceptionMessage(ex));
                }
            }
            Common.DebugHelper.WriteLine(strMetaDebug);
            ConsoleHelper.ColorWriteLine(ConsoleColor.White, strMetaDebug);
#endif
            if (metaInfo != null && metaDelegate != null)
            {
                metaDelegate(metaInfo);
            }
        }

        private int InternalReadFromSource(byte[] buffer, int offset, int count)
        {
            IAsyncResult arRead = sourceStream.BeginRead(buffer, offset, count, null, null);
            if (!arRead.AsyncWaitHandle.WaitOne(ReadFullyStream.DEFAULT_READ_TIMEOUT))
            {
                throw new TimeoutException("Could not read within the timeout period.");
            }
            return sourceStream.EndRead(arRead);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (receivedBytes == metaInt)
            {
                int metaLen = sourceStream.ReadByte();
                if (metaLen > 0)
                {
                    byte[] metaInfo = new byte[metaLen * 16];
                    int len = 0;
                    while ((len += InternalReadFromSource(metaInfo, len, metaInfo.Length - len)) < metaInfo.Length)
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                    pos += metaInfo.Length;
                    ParseMetaInfo(metaInfo);
                }
                receivedBytes = 0;
            }
            int bytesLeft = ((metaInt - receivedBytes) > count) ? count : (metaInt - receivedBytes);

            int result = InternalReadFromSource(buffer, offset, bytesLeft);

            //int result = sourceStream.Read(buffer, offset, bytesLeft);

            pos += result;
            receivedBytes += result;
            return result;
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

        public static string[] GetTagItems(string inStr, char delim, char textDelim, bool stripTextDelim)
        {
            if (string.IsNullOrWhiteSpace(inStr))
                return null;
            List<string> parts = new List<string>();
            string curStr = string.Empty;
            bool bInText = false;
            for (int i = 0; i < inStr.Length; i++)
            {
                if (inStr[i] == delim && !bInText)
                {
                    parts.Add(curStr);
                    curStr = string.Empty;
                }
                else if (inStr[i] == textDelim)
                {
                    if (!stripTextDelim)
                    {
                        curStr += inStr[i];
                    }
                    bInText = !bInText;
                }
                else
                {
                    curStr += inStr[i];
                }
            }
            if (!string.IsNullOrWhiteSpace(curStr))
                parts.Add(curStr);
            return parts.ToArray();
        }
        public static Dictionary<string, string> ParseIcyMeta(string inMeta)
        {
            if (string.IsNullOrWhiteSpace(inMeta))
                return null;
            inMeta = inMeta.Replace("\0", "");
            if (string.IsNullOrWhiteSpace(inMeta))
                return null;
            string[] parts = GetTagItems(inMeta, ';', '\'', false);
            if (parts == null)
                return null;
            Dictionary<string, string> rslt = new Dictionary<string, string>();
            foreach (string part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    string[] subParts = GetTagItems(part, '=', '\'', true);
                    string key = string.Empty;
                    string val = string.Empty;
                    if (subParts != null && subParts.Length >= 1)
                    {
                        key = subParts[0];
                        if (subParts.Length >= 2)
                            val = subParts[1];
                    }
                    if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
                    {
                        rslt.Add(key, val);
                    }
                }
            }
            return rslt;
        }
        public static Dictionary<string, string> ParseIcyMeta(byte[] metaBytes)
        {
            if (metaBytes == null || metaBytes.Length <= 0)
                return null;
            return ParseIcyMeta(System.Text.Encoding.ASCII.GetString(metaBytes));
        }
    }

    public delegate void IcyStreamProcessMetaDelegate(byte[] metaBytes);
}

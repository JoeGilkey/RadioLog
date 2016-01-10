using System;
using System.IO;
using System.IO.Ports;
using System.Reflection;

namespace RadioLog.Common
{
    public class ExSerialPort : SerialPort
    {
        public ExSerialPort() : base() { }
        public ExSerialPort(string portName) : base(portName) { }
        public ExSerialPort(string portName, int baudRate) : base(portName, baudRate) { }
        public ExSerialPort(string portName, int baudRate, Parity parity) : base(portName, baudRate, parity) { }
        public ExSerialPort(string portName, int baudRate, Parity parity, int dataBits) : base(portName, baudRate, parity, dataBits) { }
        public ExSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) : base(portName, baudRate, parity, dataBits, stopBits) { }

        protected override void Dispose(bool disposing)
        {
            var stream = typeof(SerialPort).GetField("internalSerialStream", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this) as Stream;
            if (stream != null)
            {
                try
                {
                    stream.Dispose();
                }
                catch { }
            }
            base.Dispose(disposing);
        }

        public bool SafeOpen()
        {
            try
            {
                this.DtrEnable = true;
                this.Open();
                return true;
            }
            catch (Exception ex)
            {
                DebugHelper.WriteExceptionToLog("ExSerialPort.SafeOpen", ex, false);
                return false;
            }
        }
        public string SafeReadTo(string value)
        {
            try
            {
                if (!IsOpen)
                {
                    if (!SafeOpen())
                        return null;
                }
                if (IsOpen)
                    return ReadTo(value);
                else
                    return null;
            }
            catch (Exception ex)
            {
                DebugHelper.WriteExceptionToLog("ExSerialPort.SafeReadTo", ex, false, value);
                return null;
            }
        }
        public void SafeWriteBytes(byte[] buffer, int offset, int count)
        {
            try
            {
                if (!IsOpen)
                {
                    if (!SafeOpen())
                        return;
                }
                if (IsOpen)
                {
                    Write(buffer, offset, count);
                }
            }
            catch (Exception ex)
            {
                DebugHelper.WriteExceptionToLog("ExSerialPort.SafeWriteBytes", ex, false);
            }
        }
        public int SafeReadBytes(byte[] buffer, int offset, int size)
        {
            try
            {
                if (!IsOpen)
                {
                    if (!SafeOpen())
                        return 0;
                }
                if (IsOpen)
                {
                    return Read(buffer, offset, size);
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                DebugHelper.WriteExceptionToLog("ExSerialPort.SafeWriteBytes", ex, false);
                return 0;
            }
        }
        public int SafeReadByte()
        {
            try
            {
                if (!IsOpen)
                {
                    if (!SafeOpen())
                        return -1;
                }
                if (IsOpen)
                    return ReadByte();
                else
                    return -1;
            }
            catch (Exception ex)
            {
                DebugHelper.WriteExceptionToLog("ExSerialPort.SafeReadByte", ex, false);
                return -1;
            }
        }
        public void SafeWrite(string text)
        {
            try
            {
                if (!IsOpen)
                {
                    if (!SafeOpen())
                        return;
                }
                if (IsOpen)
                    Write(text);
            }
            catch (Exception ex)
            {
                DebugHelper.WriteExceptionToLog("ExSerialPort.SafeWrite", ex, false, text);
            }
        }

        #region AT Command Helpers
        public string ReadNextATLine()
        {
            if (this.BytesToRead <= 0)
                return string.Empty;
            try
            {
                return SafeReadTo("\r\n");
            }
            catch (Exception ex)
            {
                DebugHelper.WriteExceptionToLog("ExSerialPort.ReadNextATLine", ex, false);
                return string.Empty;
            }
        }
        public string ATReadUntilOkOrError()
        {
            string responseStr = string.Empty;
            bool bDone = false;
            while (!bDone)
            {
                string tmpStr = ReadNextATLine().Trim();
                responseStr += tmpStr + "\r\n";
                bDone = (string.Equals(tmpStr, "OK", StringComparison.InvariantCultureIgnoreCase) || string.Equals(tmpStr, "ERROR", StringComparison.InvariantCultureIgnoreCase));
            }
            return responseStr;
        }
        public string[] ATReadResponseParts()
        {
            string inStr = ATReadUntilOkOrError();
            if (string.IsNullOrEmpty(inStr))
                return null;
            else
                return inStr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
        #endregion
    }
}
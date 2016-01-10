using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;

namespace RadioLog.Common
{
    public abstract class BaseSerialPortTaskObject:BaseTaskObject
    {
        private string _portName;
        private int _baud;
        private Parity _parity;
        private int _dataBits;
        private StopBits _stopBits;
        private ExSerialPort _serialPort = null;

        public BaseSerialPortTaskObject(string portName, int baud, Parity parity, int dataBits, StopBits stopBits, int sleepDelay = 1000)
            : base(sleepDelay)
        {
            _portName = portName;
            _baud = baud;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;

            CreateSerialPort();
        }

        private void CreateSerialPort()
        {
            DebugHelper.WriteLine("CreateSerialPort: {0} START", _portName);
            try
            {
                _serialPort = new ExSerialPort(_portName, _baud, _parity, _dataBits, _stopBits);
            }
            catch (Exception ex)
            {
                DebugHelper.WriteExceptionToLog("BaseSerialPortTaskObject:CreateSerialPort", ex, true);
                _serialPort = null;
            }
            DebugHelper.WriteLine("CreateSerialPort: {0} END", _portName);
        }

        public bool DoSettingsMatch(string portName, int baud, Parity parity, int dataBits, StopBits stopBits)
        {
            bool bMatch = string.Equals(portName, _portName, StringComparison.InvariantCultureIgnoreCase);
            bMatch &= _baud == baud;
            bMatch &= _parity == parity;
            bMatch &= _dataBits == dataBits;
            bMatch &= _stopBits == stopBits;
            return bMatch;
        }
        public void RefreshPortFromNewSettings(string portName, int baud, Parity parity, int dataBits, StopBits stopBits)
        {
            if (DoSettingsMatch(portName, baud, parity, dataBits, stopBits))
                return;
            CloseSerialPort();
            _portName = portName;
            _baud = baud;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            CreateSerialPort();
        }

        public bool IsPortOpen()
        {
            if (_serialPort == null)
                return false;
            else
                return _serialPort.IsOpen;
        }

        public bool OpenSerialPort()
        {
            if (IsPortOpen())
                return false;
            try
            {
                return _serialPort.SafeOpen();
            }
            catch(Exception ex)
            {
                DebugHelper.WriteExceptionToLog("BaseSerialPortTaskObject:OpenSerialPort", ex, true);
                System.Threading.Thread.Sleep(500);
                return false;
            }
        }

        public void CloseSerialPort()
        {
            if (!IsPortOpen())
                return;
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                    _serialPort.Close();
            }
            finally
            {
                System.Threading.Thread.Sleep(500);
            }
        }

        public int BytesToRead
        {
            get
            {
                if (IsPortOpen())
                    return _serialPort.BytesToRead;
                else
                    return 0;
            }
        }
        public int BytesToWrite
        {
            get
            {
                if (IsPortOpen())
                    return _serialPort.BytesToWrite;
                else
                    return 0;
            }
        }
        public void Write(string text)
        {
            if (IsPortOpen())
            {
                _serialPort.SafeWrite(text);
            }
        }
        public string ReadTo(string value)
        {
            if (IsPortOpen())
                return _serialPort.SafeReadTo(value);
            else
                return null;
        }
    }
}

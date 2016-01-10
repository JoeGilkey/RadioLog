using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;

namespace RadioLog.Scanners
{
    public class BCDScanner : Common.BaseSerialPortTaskObject
    {
        private enum StorageType { None, All, UnitId, AllExceptUnitId };
        private readonly long WAIT_FOR_RESPONSE_TICKS = TimeSpan.FromSeconds(3).Ticks;

        string _lastfreq = string.Empty;
        string _lastmod = string.Empty;
        string _lastsys = string.Empty;
        string _lastdep = string.Empty;
        string _lastchan = string.Empty;
        string _lastunit = string.Empty;
        private bool _hp1Mode = false;
        private bool _bcd996xtUnitIdSupport = false;
        private char _delimChar = ',';
        private int _unitIdDisplayLine = 3;
        private ConcurrentQueue<string> _pendingCommands = new ConcurrentQueue<string>();
        private long _maxWaitForResponseTick = 0;
        private bool _unitIdDisplayEnabled = false;

        private string FixFreq(string inStr)
        {
            if (string.IsNullOrWhiteSpace(inStr))
                return string.Empty;
            string rslt=string.Empty;
            if (inStr.IndexOf('.') >= 0)
            {
                rslt = inStr.TrimStart(new char[] { '0' });
            }
            else if (inStr.Length != 8)
            {
                rslt = inStr;
            }
            else
            {
                rslt = string.Concat(inStr.Substring(1, 3), ".", inStr.Substring(4));
                rslt = rslt.TrimStart(new char[] { '0' });
            }
            int indx=rslt.IndexOf('.');
            if (indx >= 0 && rslt.Length > indx + 4 && rslt.Substring(indx + 4, 1) == "0")
            {
                rslt = rslt.Substring(0, indx + 4);
            }
            return rslt;
        }

        private void ProcessScannerResponse(string[] splitList)
        {
            StorageType st = StorageType.None;
            string freq = string.Empty;
            string mod = string.Empty;
            string sys = string.Empty;
            string dep = string.Empty;
            string chan = string.Empty;
            string unit = string.Empty;
            if (_hp1Mode)
            {
                if (!string.IsNullOrWhiteSpace(splitList[0]) && !string.IsNullOrWhiteSpace(splitList[1]) && splitList[0].ToUpper().Trim() == "RMT" && splitList[1].ToUpper().Trim() == "STATUS")
                {
                    freq = splitList[2].Trim();
                    mod = splitList[3].Trim();
                    sys = splitList[8].Trim();
                    dep = splitList[9].Trim();
                    chan = splitList[10].Trim();
                    unit = splitList[15].Trim();
                    st = StorageType.All;
                }
            }
            else
            {
                switch (splitList[0])
                {
                    case "MDL":
                        {
                            string modelStr = splitList[1].Trim();
                            _bcd996xtUnitIdSupport = string.Equals(modelStr, "BCD996XT", StringComparison.InvariantCultureIgnoreCase);
                            break;
                        }
                    case "GLG":
                        {
                            if (splitList.Length >= 9)
                            {
                                bool landed = splitList[8] == "1";
                                if (landed)
                                {
                                    freq = FixFreq(splitList[1].Trim());
                                    mod = splitList[2].Trim();
                                    sys = splitList[5].Trim();
                                    dep = splitList[6].Trim();
                                    chan = splitList[7].Trim();
                                    st = StorageType.AllExceptUnitId;
                                }
                            }
                            break;
                        }
                    case "STS":
                        {
                            string dispForm = splitList[1];
                            if (dispForm.Length >= _unitIdDisplayLine)
                            {
                                freq = _lastfreq;
                                mod = _lastmod;
                                sys = _lastsys;
                                dep = _lastdep;
                                chan = _lastchan;
                                unit = splitList[_unitIdDisplayLine * 2];
                                if (!string.IsNullOrWhiteSpace(unit) && unit.Contains('?'))
                                    unit = string.Empty;
                                st = StorageType.UnitId;
                            }
                            break;
                        }
                }
            }
            bool bChanged = false;

            if (st == StorageType.All || st == StorageType.AllExceptUnitId)
            {
                bChanged |= (!string.IsNullOrWhiteSpace(freq) && _lastfreq != freq);
                bChanged |= (!string.IsNullOrWhiteSpace(mod) && _lastmod != mod);
                bChanged |= (!string.IsNullOrWhiteSpace(sys) && _lastsys != sys);
                bChanged |= (!string.IsNullOrWhiteSpace(dep) && _lastdep != dep);
                bChanged |= (!string.IsNullOrWhiteSpace(chan) && _lastchan != chan);

                _lastfreq = freq;
                _lastmod = mod;
                _lastsys = sys;
                _lastdep = dep;
                _lastchan = chan;
            }
            if (st == StorageType.All || st == StorageType.UnitId)
            {
                bChanged |= (!string.IsNullOrWhiteSpace(unit) && _lastunit != unit);
                _lastunit = unit;
            }

            bool bGoodToGo = false;
            if (_hp1Mode)
                bGoodToGo = true;
            else if (_bcd996xtUnitIdSupport)
                bGoodToGo = !string.IsNullOrWhiteSpace(unit);
            else
                bGoodToGo = true;

            if (bChanged && bGoodToGo)
            {
                RaiseSignalReceived(freq, mod, sys, dep, chan, unit);
            }
        }

        public BCDScanner(string portName, int baud, Parity parity, int dataBits, StopBits stopBits, bool hp1Mode, int unitIdDisplayLine, bool unitIdDisplayEnabled)
            : base(portName, baud, parity, dataBits, stopBits, 250)
        {
            _hp1Mode = hp1Mode;
            _unitIdDisplayLine = unitIdDisplayLine;
            _unitIdDisplayEnabled = unitIdDisplayEnabled;
            SetupForHP1Mode();
        }

        private void SetupForHP1Mode()
        {
            while (_pendingCommands.Count > 0)
            {
                string s;
                if (!_pendingCommands.TryDequeue(out s))
                    break;
            }
            _bcd996xtUnitIdSupport = false;
            if (_hp1Mode)
            {
                _delimChar = '\t';
            }
            else
            {
                _delimChar = ',';
                _pendingCommands.Enqueue("MDL");
            }
        }

        public void UpdateScannerSettings(bool bEnabled, string portName, int baud, Parity parity, int dataBits, StopBits stopBits, bool hp1Mode)
        {
            bool needsRestart = !DoSettingsMatch(portName, baud, parity, dataBits, stopBits);
            if (needsRestart)
            {
                Stop();
            }
            _hp1Mode = hp1Mode;
            SetupForHP1Mode();
            RefreshPortFromNewSettings(portName, baud, parity, dataBits, stopBits);
            if (bEnabled && needsRestart)
            {
                Start();
            }
        }

        protected override bool DoPreStart()
        {
            if (base.DoPreStart())
            {
                try
                {
                    return OpenSerialPort();
                }
                catch (Exception ex)
                {
                    Common.DebugHelper.WriteExceptionToLog("BCDScanner.DoPreStart", ex, false);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        protected override void DoPostStop()
        {
            try
            {
                CloseSerialPort();
            }
            finally
            {
                base.DoPostStop();
            }
        }

        protected override void ThreadProc()
        {
            if (this.IsCancelPending)
                return;
            if (!IsPortOpen())
                return;
            if (_maxWaitForResponseTick > 0 && this.BytesToRead <= 0)
            {
                if (_maxWaitForResponseTick < DateTime.Now.Ticks)
                    _maxWaitForResponseTick = 0; //timed out...
                else
                    return;
            }
            if (this.BytesToRead > 0)
            {
                _maxWaitForResponseTick = 0;
                while (this.BytesToRead > 0)
                {
                    string inLine = this.ReadTo("\r");
                    if (!string.IsNullOrWhiteSpace(inLine))
                    {
                        string[] splitList = inLine.Split(new char[] { _delimChar }, StringSplitOptions.None);
                        if (splitList != null && splitList.Length > 0)
                        {
                            ProcessScannerResponse(splitList);
                        }
                    }
                }
            }
            else if (_pendingCommands.Count > 0)
            {
                string outCmd = string.Empty;
                if (_pendingCommands.TryDequeue(out outCmd))
                {
                    this.Write(outCmd + "\r");
                    _maxWaitForResponseTick = DateTime.Now.Ticks + WAIT_FOR_RESPONSE_TICKS;
                }
            }
            else
            {
                if (_hp1Mode)
                {
                    string cmd = "RMT" + _delimChar + "STATUS" + _delimChar;
                    cmd += MakeHP1Checksum(cmd).ToString();
                    _pendingCommands.Enqueue(cmd);
                }
                else
                {
                    _pendingCommands.Enqueue("GLG");
                    if (_unitIdDisplayEnabled && _bcd996xtUnitIdSupport)
                    {
                        _pendingCommands.Enqueue("STS");
                    }
                }
            }
        }

        private int MakeHP1Checksum(string str)
        {
            int sum = 0;
            for (int i = 0; i < str.Length; i++)
            {
                sum += (byte)(str[i]);
            }
            return sum;
        }

        public event EventHandler<BCDScannerSignalReceivedArgs> OnSignalReceived;
        private void RaiseSignalReceived(string freq, string mod, string sys, string dep, string chan, string unit)
        {
            if (OnSignalReceived != null)
            {
                OnSignalReceived(this, new BCDScannerSignalReceivedArgs(freq, mod, sys, dep, chan, unit));
            }
        }
    }

    public class BCDScannerSignalReceivedArgs : EventArgs
    {
        public string FrequencyTGroup { get; private set; }
        public string Modulation { get; private set; }
        public string SystemName { get; private set; }
        public string DepartmentName { get; private set; }
        public string ChannelName { get; private set; }
        public string UnitName { get; private set; }

        public BCDScannerSignalReceivedArgs(string freq, string mod, string sys, string dep, string chan, string unit)
        {
            this.FrequencyTGroup = freq;
            this.Modulation = mod;
            this.SystemName = sys;
            this.DepartmentName = dep;
            this.ChannelName = chan;
            this.UnitName = unit;
        }
    }
}

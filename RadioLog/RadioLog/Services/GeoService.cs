using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DotSpatial.Positioning;

namespace RadioLog.Services
{
    public class GeoService
    {
        private static GeoService _instance = null;
        public static GeoService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GeoService();
                }
                return _instance;
            }
        }

        private GeoService()
        {
            this.CurrentPosition = Position.Empty;
        }
        ~GeoService()
        {
            if (_instance != null)
            {
                _instance.Stop();
            }
        }

        private Device _curDevice = null;
        public Position CurrentPosition { get; private set; }

        public double? CurrentLatitude
        {
            get
            {
                if (CurrentPosition == null || CurrentPosition.IsEmpty || CurrentPosition.IsInvalid)
                    return null;
                else
                    return CurrentPosition.Latitude.DecimalDegrees;
            }
        }
        public double? CurrentLongitude
        {
            get
            {
                if (CurrentPosition == null || CurrentPosition.IsEmpty || CurrentPosition.IsInvalid)
                    return null;
                else
                    return CurrentPosition.Longitude.DecimalDegrees;
            }
        }

        public void Start()
        {
            if (_curDevice != null)
            {
                return;
            }
            System.Threading.Thread tGPS = new System.Threading.Thread(DetectAndStartGPS);
            tGPS.IsBackground = true;
            tGPS.Start();
        }
        public void Stop()
        {
            if (_curDevice != null)
            {
                try
                {
                    _curDevice.Close();
                }
                finally { _curDevice = null; }
            }
        }

        private void ClearCurrentPosition()
        {
            this.CurrentPosition = Position.Empty;
        }
        private void UpdateCurrentPosition(DotSpatial.Positioning.Position position)
        {
            if (position == null || position.IsEmpty || position.IsInvalid)
            {
                return;
            }

            this.CurrentPosition = position;
        }

        void DetectAndStartGPS()
        {
            try
            {
                if (Common.AppSettings.Instance.EnableGPS && !string.IsNullOrEmpty(Common.AppSettings.Instance.GPSPort) && Common.AppSettings.Instance.GPSPort.Length > 3)
                {
                    string strComNum = Common.AppSettings.Instance.GPSPort.Substring(3);
                    int iComNum = -1;
                    if (!int.TryParse(strComNum, out iComNum))
                        iComNum = -1;
                    Devices.DeviceDetectionAttemptFailed += (s, e) =>
                    {
                        //
                    };
                    Devices.DeviceDetectionCompleted += (s, e) =>
                    {
                        try
                        {
                            foreach (Device dev in Devices.GpsDevices)
                            {
                                SerialDevice sd=dev as SerialDevice;
                                if (sd != null && (sd.Port == Common.AppSettings.Instance.GPSPort || sd.PortNumber == iComNum))
                                {
                                    _curDevice = sd;
                                    break;
                                }
                            }
                            if (_curDevice == null)
                            {
                                _curDevice = new SerialDevice(Common.AppSettings.Instance.GPSPort);
                                Devices.Add(_curDevice);
                            }
                        }
                        catch
                        {
                            _curDevice = null;
                        }
                        bool bGoodDevice = false;
                        if (_curDevice != null)
                        {
                            try
                            {
                                _curDevice.Connected += (sd, ed) =>
                                {
                                    Common.DebugHelper.WriteLine("GPS Device Opened: {0}", Common.AppSettings.Instance.GPSPort);
                                };
                                _curDevice.Disconnected += (sd, ed) =>
                                {
                                    Common.DebugHelper.WriteLine("GPS Device Closed: {0}", Common.AppSettings.Instance.GPSPort);
                                };
                                if (!_curDevice.IsOpen)
                                {
                                    _curDevice.Open();
                                }
                                NmeaInterpreter nmea = new NmeaInterpreter();
                                nmea.PositionChanged += nmea_PositionChanged;
                                nmea.Start(_curDevice);
                                bGoodDevice = true;
                            }
                            catch (Exception ex)
                            {
                                Common.DebugHelper.WriteExceptionToLog("DetectAndStartGPS", ex, true, string.Format("Device found, but error setting up: {0}", Common.AppSettings.Instance.GPSPort));
                                bGoodDevice = false;
                            }
                        }
                        if (!bGoodDevice)
                        {
                            ClearCurrentPosition();
                        }
                    };
                    Devices.AllowBluetoothConnections = false;
                    Devices.AllowExhaustiveSerialPortScanning = false;
                    Devices.AllowSerialConnections = true;
                    Devices.BeginDetection();
                }
            }
            catch
            {
                _curDevice = null;
            }
        }

        void nmea_PositionChanged(object sender, PositionEventArgs e)
        {
            if (e == null || e.Position == null || e.Position.IsEmpty || e.Position.IsInvalid)
                return;
            UpdateCurrentPosition(e.Position);
        }
    }
}

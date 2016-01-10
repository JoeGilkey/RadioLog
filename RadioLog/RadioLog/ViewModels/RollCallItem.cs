using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RadioLog.Common;

namespace RadioLog.ViewModels
{
    public class RollCallItem : RadioLog.WPFCommon.ThreadSafeNotificationObject
    {
        private EmergencyState _emergencyState = EmergencyState.NonEmergency;
        private RadioInfo _radioInfo = null;

        public RollCallItem(RadioInfo radioInfo, EmergencyState emergencyState)
        {
            _radioInfo = radioInfo;
            _emergencyState = emergencyState;
        }

        public string DisplayName
        {
            get
            {
                if (_radioInfo == null)
                    return string.Empty;
                else
                    return _radioInfo.DisplayName;
            }
        }
        public string SignalingLookupKey
        {
            get
            {
                if (_radioInfo == null)
                    return string.Empty;
                else
                    return _radioInfo.SignalingLookupKey;
            }
        }
        public EmergencyState EmergencyState
        {
            get { return _emergencyState; }
            set
            {
                _emergencyState = value;
                OnPropertyChanged("EmergencyState");
            }
        }

        public void RaiseDisplayInfoChanged()
        {
            OnPropertyChanged("DisplayName");
            OnPropertyChanged("EmergencyState");
        }
    }
}

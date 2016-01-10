using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RadioLog.Common;

namespace RadioLog.ViewModels
{
    public class RadioSignalItemModel : RadioLog.WPFCommon.ThreadSafeNotificationObject, IRadioContactEmergency
    {
        private RadioLog.Common.RadioSignalingItem _signalItem;
        private EmergencyState _emergencyState = EmergencyState.NonEmergency;
        private bool _canDoManualMayday = false;

        public RadioSignalItemModel(RadioSignalingItem signalItem)
        {
            if (signalItem == null)
                throw new ArgumentNullException();
            this._signalItem = signalItem;
            if (signalItem.Code == SignalCode.Emergency)
            {
                _emergencyState = Common.EmergencyState.EmergencyActive;
            }
            else if (signalItem.Code == SignalCode.EmergencyAck)
            {
                _emergencyState = Common.EmergencyState.EmergencyCleared;
            }
            else
            {
                _emergencyState = Common.EmergencyState.NonEmergency;
            }
        }

        public RadioLog.Common.RadioSignalingItem RawSignalItem { get { return _signalItem; } }
        public SignalingSourceType SourceType { get { return _signalItem.SourceType; } }
        public string SourceName { get { return _signalItem.SourceName; } }
        public string SignalingFormat { get { return _signalItem.SignalingFormat; } }
        public SignalCode Code { get { return _signalItem.Code; } }
        public string UnitId { get { return _signalItem.UnitId; } }
        public string Description { get { return _signalItem.Description; } }
        public DateTime Timestamp { get { return _signalItem.Timestamp; } }

        public string SignalingLookupKey { get { return RadioInfo.GenerateLookupKey(SignalingFormat, UnitId); } }

        public string AgencyName { get { return _signalItem.AgencyName; } }
        public string UnitName { get { return _signalItem.UnitName; } }
        public string RadioName { get { return _signalItem.RadioName; } }
        public string AssignedPersonnel { get { return _signalItem.AssignedPersonnel; } }
        public string AssignedRole { get { return _signalItem.AssignedRole; } }
        public RadioTypeCode RadioType { get { return _signalItem.RadioType; } }

        public string TimestampStr { get { return RadioLog.Common.DisplayFormatterUtils.TimestampToDisplayStr(_signalItem.Timestamp); } }
        public string SourceTypeStr { get { return RadioLog.Common.DisplayFormatterUtils.SignalingSourceTypeToDisplayStr(_signalItem.SourceType); } }
        public string RadioTypeStr { get { return RadioLog.Common.DisplayFormatterUtils.RadioTypeCodeToDisplayStr(_signalItem.RadioType); } }

        public virtual void UpdateLookups()
        {
            Common.RadioInfoLookupHelper.Instance.PerformLookupsOnRadioSignalItem(_signalItem);
            OnPropertyChanged("RadioId");
            OnPropertyChanged("AgencyName");
            OnPropertyChanged("UnitId");
            OnPropertyChanged("Description");
            OnPropertyChanged("UnitName");
            OnPropertyChanged("RadioName");
            OnPropertyChanged("AssignedPersonnel");
            OnPropertyChanged("AssignedRole");
            OnPropertyChanged("RadioType");
            OnPropertyChanged("RadioTypeStr");
            OnPropertyChanged("CanDoManualMayday");
            OnPropertyChanged("CanDoManualMaydayVisibility");
        }

        public EmergencyState EmergencyState
        {
            get { return _emergencyState; }
            set
            {
                if (_emergencyState != value)
                {
                    OnPropertyChanging("EmergencyState");
                    _emergencyState = value;
                    OnPropertyChanged("EmergencyState");
                }
            }
        }

        public bool CanDoManualMayday
        {
            get { return _canDoManualMayday; }
            set
            {
                if (_canDoManualMayday != value)
                {
                    _canDoManualMayday = value;
                    OnPropertyChanged("CanDoManualMayday");
                    OnPropertyChanged("CanDoManualMaydayVisibility");
                }
            }
        }
        public System.Windows.Visibility CanDoManualMaydayVisibility { get { return CanDoManualMayday ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    }

    public class EmergencyRadioSignalItemModel : RadioSignalItemModel
    {
        private DateTime? _startedDT;
        private DateTime? _endedDT;

        public EmergencyRadioSignalItemModel(RadioSignalingItem signalItem)
            : base(signalItem)
        {
            _startedDT = null;
            _endedDT = null;
        }

        public override void UpdateLookups()
        {
            base.UpdateLookups();
            OnPropertyChanged("CanClear");
            OnPropertyChanged("CanClearVisibility");
        }

        public DateTime? StartedDT
        {
            get { return _startedDT; }
            set
            {
                if (_startedDT != value)
                {
                    OnPropertyChanging("StartedDT");
                    _startedDT = value;
                    OnPropertyChanged("StartedDT");
                }
            }
        }

        public DateTime? EndedDT
        {
            get { return _endedDT; }
            set
            {
                if (_endedDT != value)
                {
                    OnPropertyChanging("EndedDT");
                    _endedDT = value;
                    OnPropertyChanged("EndedDT");
                    OnPropertyChanged("CanClear");
                    OnPropertyChanged("CanClearVisibility");
                }
            }
        }

        public bool CanClear { get { return EndedDT == null; } }
        public System.Windows.Visibility CanClearVisibility { get { return CanClear ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden; } }
    }
}

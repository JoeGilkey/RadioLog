using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;

using Cinch;
using MEFedMVVM.Common;
using MEFedMVVM.ViewModelLocator;

namespace RadioLog.ViewModels
{
    public class SingleRadioEditorViewModel : RadioLog.WPFCommon.ThreadSafeViewModelBase
    {
        RadioSignalItemModel _radioSignal = null;
        Guid _unitId = Guid.Empty;
        Guid _agencyId = Guid.Empty;
        string _radioName = string.Empty;
        string _roleName = string.Empty;
        string _personnelName = string.Empty;
        string _unitName = string.Empty;
        string _agencyName = string.Empty;
        bool _excludeFromRollCall=false;
        Common.RadioTypeCode _radioType = Common.RadioTypeCode.Unknown;

        RadioLog.WPFCommon.ThreadSafeObservableCollection<SingleRadioEditorAgency> _agencies;
        RadioLog.WPFCommon.ThreadSafeObservableCollection<SingleRadioEditorUnit> _units;
        RadioLog.WPFCommon.ThreadSafeObservableCollection<SingleRadioEditorRadioType> _radioTypes;

        public SingleRadioEditorViewModel(RadioSignalItemModel radioSignal)
        {
            if (radioSignal == null)
                throw new ArgumentNullException();

            _radioSignal = radioSignal;
            SignalingFormat = _radioSignal.SignalingFormat;
            SignalingUnitId = _radioSignal.UnitId;
            _unitName = _radioSignal.UnitName;
            _agencyName = _radioSignal.AgencyName;

            _radioTypes = new WPFCommon.ThreadSafeObservableCollection<SingleRadioEditorRadioType>();
            _radioTypes.Add(new SingleRadioEditorRadioType() { RadioType = Common.RadioTypeCode.Unknown, RadioTypeName = "<UNASSIGNED>" });
            _radioTypes.Add(new SingleRadioEditorRadioType() { RadioType = Common.RadioTypeCode.Mobile, RadioTypeName = "Mobile" });
            _radioTypes.Add(new SingleRadioEditorRadioType() { RadioType = Common.RadioTypeCode.Portable, RadioTypeName = "Portable" });
            _radioTypes.Add(new SingleRadioEditorRadioType() { RadioType = Common.RadioTypeCode.BaseStation, RadioTypeName = "Base Station" });

            _agencies = new WPFCommon.ThreadSafeObservableCollection<SingleRadioEditorAgency>();
            _agencies.Add(new SingleRadioEditorAgency() { AgencyId = Guid.Empty, AgencyName = "<UNASSIGNED>" });
            foreach (Common.AgencyInfo aInfo in RadioLog.Common.RadioInfoLookupHelper.Instance.AgencyList.OrderBy(a => a.AgencyName))
            {
                _agencies.Add(new SingleRadioEditorAgency() { AgencyId = aInfo.AgencyId, AgencyName = aInfo.AgencyName });
            }

            _units = new WPFCommon.ThreadSafeObservableCollection<SingleRadioEditorUnit>();
            _units.Add(new SingleRadioEditorUnit() { UnitId = Guid.Empty, AgencyId = Guid.Empty, UnitName = "<UNASSIGNED>" });
            foreach (Common.UnitInfo uInfo in RadioLog.Common.RadioInfoLookupHelper.Instance.UnitList.OrderBy(u => u.UnitName))
            {
                _units.Add(new SingleRadioEditorUnit() { UnitId = uInfo.UnitKeyId, AgencyId = uInfo.AgencyKeyId, UnitName = uInfo.UnitName });
            }

            if (!string.IsNullOrWhiteSpace(_radioSignal.SignalingFormat)&&!string.IsNullOrWhiteSpace(_radioSignal.UnitId))
            {
                Common.RadioInfo radioInfo = Common.RadioInfoLookupHelper.Instance.RadioList.FirstOrDefault(r => r.SignalingLookupKey == _radioSignal.SignalingLookupKey);
                if (radioInfo != null)
                {
                    _unitId = radioInfo.UnitKeyId;
                    _agencyId = radioInfo.AgencyKeyId;
                    _radioName = radioInfo.RadioName;
                    _roleName = radioInfo.RoleName;
                    _personnelName = radioInfo.PersonnelName;
                    _radioType = radioInfo.RadioType;
                    SignalingFormat = radioInfo.SignalingFormat;
                    SignalingUnitId = radioInfo.SignalingUnitId;
                }
            }
        }

        public RadioLog.WPFCommon.ThreadSafeObservableCollection<SingleRadioEditorAgency> Agencies { get { return _agencies; } }
        public RadioLog.WPFCommon.ThreadSafeObservableCollection<SingleRadioEditorUnit> Units { get { return _units; } }
        public RadioLog.WPFCommon.ThreadSafeObservableCollection<SingleRadioEditorRadioType> RadioTypes { get { return _radioTypes; } }

        public string RadioName { get { return _radioName; } set { if (_radioName != value) { _radioName = value; OnPropertyChanged(); } } }
        public string RoleName { get { return _roleName; } set { if (_roleName != value) { _roleName = value; OnPropertyChanged(); } } }
        public string PersonnelName { get { return _personnelName; } set { if (_personnelName != value) { _personnelName = value; OnPropertyChanged(); } } }
        public Common.RadioTypeCode RadioType
        {
            get { return _radioType; }
            set
            {
                if (_radioType != value)
                {
                    _radioType = value;
                    OnPropertyChanged();

                    if(_radioType== Common.RadioTypeCode.Mobile||_radioType== Common.RadioTypeCode.BaseStation)
                    {
                        ExcludeFromRollCall = true;
                    }
                    else if (_radioType == Common.RadioTypeCode.Portable)
                    {
                        ExcludeFromRollCall = false;
                    }
                }
            }
        }
        public string SignalingFormat { get; private set; }
        public string SignalingUnitId { get; private set; }
        public string UnitName
        {
            get { return _unitName; }
            set
            {
                if (_unitName != value)
                {
                    _unitName = value;
                    OnPropertyChanged();
                }
            }
        }
        public string AgencyName
        {
            get { return _agencyName; }
            set
            {
                if (_agencyName != value)
                {
                    _agencyName = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool ExcludeFromRollCall { get { return _excludeFromRollCall; } set { if (_excludeFromRollCall != value) { _excludeFromRollCall = value; OnPropertyChanged(); } } }

        public Guid UnitId
        {
            get { return _unitId; }
            set
            {
                if (_unitId != value)
                {
                    _unitId = value;
                    if (value != Guid.Empty)
                    {
                        SingleRadioEditorUnit tmpUnit = Units.FirstOrDefault(u => u.UnitId == value);
                        if (tmpUnit != null)
                        {
                            AgencyId = tmpUnit.AgencyId;
                        }
                    }
                    OnPropertyChanged();
                    OnPropertyChanged("UnitNameEnabled");
                }
            }
        }
        public Guid AgencyId
        {
            get { return _agencyId; }
            set
            {
                if (_agencyId != value)
                {
                    _agencyId = value;
                    OnPropertyChanged();
                    OnPropertyChanged("AgencyNameEnabled");
                }
            }
        }

        public bool UnitNameEnabled { get { return _unitId == Guid.Empty; } }
        public bool AgencyNameEnabled { get { return _agencyId == Guid.Empty; } }

        public bool CanSave()
        {
            return true;
        }

        public bool PerformSave()
        {
            if (!CanSave())
                return false;

            Guid sAgencyId = AgencyId;
            Guid sUnitId = UnitId;
            if (AgencyId == Guid.Empty && !string.IsNullOrWhiteSpace(AgencyName))
            {
                Common.AgencyInfo aInfo = Common.RadioInfoLookupHelper.Instance.AgencyList.FirstOrDefault(a => a.AgencyName.Equals(AgencyName, StringComparison.InvariantCultureIgnoreCase));
                if (aInfo != null)
                {
                    sAgencyId = aInfo.AgencyId;
                }
                else
                {
                    sAgencyId = Guid.NewGuid();
                    Common.RadioInfoLookupHelper.Instance.AddOrUpdateAgency(sAgencyId, AgencyName);
                }
            }
            if (UnitId == Guid.Empty && !string.IsNullOrWhiteSpace(UnitName))
            {
                //find or create unit...
                Common.UnitInfo uInfo = Common.RadioInfoLookupHelper.Instance.UnitList.FirstOrDefault(u => u.UnitName.Equals(UnitName, StringComparison.InvariantCultureIgnoreCase));
                if (uInfo != null)
                {
                    sUnitId = uInfo.UnitKeyId;
                }
                else
                {
                    sUnitId = Guid.NewGuid();
                    Common.RadioInfoLookupHelper.Instance.AddOrUpdateUnit(sUnitId, sAgencyId, UnitName);
                }
            }
            string signalingLookupKey = Common.RadioInfo.GenerateLookupKey(SignalingFormat, SignalingUnitId);
            if (!string.IsNullOrWhiteSpace(signalingLookupKey))
            {
                Common.RadioInfo radioInfo = Common.RadioInfoLookupHelper.Instance.RadioList.FirstOrDefault(r => r.SignalingLookupKey == signalingLookupKey);
                radioInfo.AgencyKeyId = sAgencyId;
                radioInfo.PersonnelName = PersonnelName;
                radioInfo.RadioName = RadioName;
                radioInfo.RadioType = RadioType;
                radioInfo.RoleName = RoleName;
                radioInfo.UnitKeyId = sUnitId;
                radioInfo.ExcludeFromRollCall = ExcludeFromRollCall;
                Cinch.Mediator.Instance.NotifyColleaguesAsync<bool>("REFRESH_RADIO_LOOKUPS", true);
            }
            else
            {
                Common.RadioInfoLookupHelper.Instance.AddOrUpdateRadio(sUnitId, sAgencyId, SignalingFormat, SignalingUnitId, RadioName, RoleName, PersonnelName, RadioType, ExcludeFromRollCall);
            }
            Common.RadioInfoLookupHelper.Instance.SaveInfo();
            return true;
        }
    }

    public class SingleRadioEditorAgency
    {
        public Guid AgencyId { get; set; }
        public string AgencyName { get; set; }
    }

    public class SingleRadioEditorUnit
    {
        public Guid UnitId { get; set; }
        public Guid AgencyId { get; set; }
        public string UnitName { get; set; }
    }

    public class SingleRadioEditorRadioType
    {
        public Common.RadioTypeCode RadioType { get; set; }
        public string RadioTypeName { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RadioLog.Windows
{
    /// <summary>
    /// Interaction logic for RadioEditorDialog.xaml
    /// </summary>
    public partial class RadioEditorDialog : BaseRadioLogWindow
    {
        public RadioLog.WPFCommon.ThreadSafeObservableCollection<RadioInfoModel> Radios { get; private set; }
        public List<RadioLog.Common.AgencyInfo> Agencies { get; private set; }
        public List<RadioLog.Common.UnitInfo> Units { get; private set; }

        public RadioEditorDialog()
        {
            InitializeComponent();

            this.Agencies = new List<Common.AgencyInfo>();
            this.Agencies.Add(new Common.AgencyInfo(Guid.Empty, ""));
            foreach (Common.AgencyInfo aInfo in RadioLog.Common.RadioInfoLookupHelper.Instance.AgencyList.OrderBy(a => a.AgencyName))
            {
                this.Agencies.Add(aInfo);
            }

            this.Units = new List<Common.UnitInfo>();
            this.Units.Add(new Common.UnitInfo(Guid.Empty, Guid.Empty, ""));
            foreach (Common.UnitInfo uInfo in RadioLog.Common.RadioInfoLookupHelper.Instance.UnitList.OrderBy(u => u.UnitName))
            {
                this.Units.Add(uInfo);
            }

            Radios = new WPFCommon.ThreadSafeObservableCollection<RadioInfoModel>();
            foreach (Common.RadioInfo rInfo in RadioLog.Common.RadioInfoLookupHelper.Instance.RadioList.OrderBy(r => r.SignalingLookupKey))
            {
                this.Radios.Add(new RadioInfoModel(rInfo));
            }

            colAgency.ItemsSource = this.Agencies;
            colUnit.ItemsSource = this.Units;
            dgRadios.ItemsSource = this.Radios;

            colRadioName.Visibility = Common.AppSettings.Instance.ShowRadioName ? Visibility.Visible : Visibility.Collapsed;
            colRoleName.Visibility = Common.AppSettings.Instance.ShowAssignedRole ? Visibility.Visible : Visibility.Collapsed;
            colPersonName.Visibility = Common.AppSettings.Instance.ShowAssignedPersonnel ? Visibility.Visible : Visibility.Collapsed;

            colExcludeRollCall.Visibility = Common.AppSettings.Instance.WorkstationMode == Common.RadioLogMode.Fireground ? Visibility.Visible : Visibility.Collapsed;
            tbERC.Visibility = colExcludeRollCall.Visibility;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            bool bChangesMade = false;
            foreach (RadioInfoModel rInfo in this.Radios)
            {
                if (rInfo.ChangesMade)
                {
                    RadioLog.Common.RadioInfoLookupHelper.Instance.AddOrUpdateRadio(rInfo.UnitId, rInfo.AgencyId, rInfo.SignalingFormat, rInfo.SignalingUnitId, rInfo.RadioName, rInfo.RoleName, rInfo.PersonnelName, rInfo.RadioType, rInfo.ExcludeFromRollCall);
                    bChangesMade = true;
                }
            }
            if (bChangesMade)
            {
                RadioLog.Common.RadioInfoLookupHelper.Instance.SaveInfo();
                Cinch.Mediator.Instance.NotifyColleagues<bool>("REFRESH_RADIO_LOOKUPS", true);
            }
            this.DialogResult = true;
        }

        private void btnAddRadio_Click(object sender, RoutedEventArgs e)
        {
            this.Radios.Add(new RadioInfoModel() { RadioName = "NEW RADIO" });
        }
    }

    public class RadioInfoModel : RadioLog.WPFCommon.ThreadSafeNotificationObject
    {
        private Guid _unitId;
        private Guid _agencyId;
        private string _radioName;
        private string _signalingFormat;
        private string _signalingUnitId;
        private string _roleName;
        private string _personnelName;
        private RadioLog.Common.RadioTypeCode _radioTypeCode;
        private bool _changesMade = false;
        public string _firstHeardSourceName;
        private bool _excludeFromRollCall;

        public RadioInfoModel(RadioLog.Common.RadioInfo radio)
        {
            if (radio == null)
                throw new ArgumentNullException();
            this._unitId = radio.UnitKeyId;
            this._agencyId = radio.AgencyKeyId;
            this._radioName = radio.RadioName;
            this._signalingFormat = radio.SignalingFormat;
            this._signalingUnitId = radio.SignalingUnitId;
            this._roleName = radio.RoleName;
            this._personnelName = radio.PersonnelName;
            this._radioTypeCode = radio.RadioType;
            this._firstHeardSourceName = radio.FirstHeardSourceName;
            this._excludeFromRollCall = radio.ExcludeFromRollCall;
        }
        public RadioInfoModel()
        {
            _changesMade = true;
            _unitId = Guid.Empty;
            _agencyId = Guid.Empty;
            _radioName = string.Empty;
            _signalingFormat = RadioLog.Common.SignalingNames.MDC;
            _signalingUnitId = string.Empty;
            _roleName = string.Empty;
            _personnelName = string.Empty;
            _radioTypeCode = Common.RadioTypeCode.Unknown;
            _firstHeardSourceName = string.Empty;
            _excludeFromRollCall = false;
        }

        public void MarkChangesMade() { _changesMade = true; }
        public void ClearChangesMade() { _changesMade = false; }
        public bool ChangesMade { get { return _changesMade; } }

        public Guid UnitId
        {
            get { return _unitId; }
            set
            {
                if (_unitId != value)
                {
                    _unitId = value;
                    MarkChangesMade();
                    OnPropertyChanged();

                    Guid gAgencyId = RadioLog.Common.RadioInfoLookupHelper.Instance.GetAgencyIdForUnitId(_unitId);
                    if (gAgencyId != Guid.Empty)
                    {
                        AgencyId = gAgencyId;
                    }
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
                    MarkChangesMade();
                    OnPropertyChanged();
                }
            }
        }
        public string RadioName
        {
            get { return _radioName; }
            set
            {
                if (_radioName != value)
                {
                    _radioName = value;
                    MarkChangesMade();
                    OnPropertyChanged();
                }
            }
        }
        public string SignalingFormat
        {
            get { return _signalingFormat; }
            set
            {
                if (_signalingFormat != value)
                {
                    _signalingFormat = value;
                    MarkChangesMade();
                    OnPropertyChanged();
                }
            }
        }
        public string SignalingUnitId
        {
            get { return _signalingUnitId; }
            set
            {
                if (_signalingUnitId != value)
                {
                    _signalingUnitId = value;
                    MarkChangesMade();
                    OnPropertyChanged();
                }
            }
        }
        public string RoleName
        {
            get { return _roleName; }
            set
            {
                if (_roleName != value)
                {
                    _roleName = value;
                    MarkChangesMade();
                    OnPropertyChanged();
                }
            }
        }
        public string PersonnelName
        {
            get { return _personnelName; }
            set
            {
                if (_personnelName != value)
                {
                    _personnelName = value;
                    MarkChangesMade();
                    OnPropertyChanged();
                }
            }
        }
        public RadioLog.Common.RadioTypeCode RadioType
        {
            get { return _radioTypeCode; }
            set
            {
                if (_radioTypeCode != value)
                {
                    _radioTypeCode = value;
                    MarkChangesMade();
                    OnPropertyChanged();
                    if (_radioTypeCode == Common.RadioTypeCode.BaseStation || _radioTypeCode == Common.RadioTypeCode.Mobile)
                    {
                        ExcludeFromRollCall = true;
                    }
                    else if(_radioTypeCode== Common.RadioTypeCode.Portable)
                    {
                        ExcludeFromRollCall = false;
                    }
                }
            }
        }
        public string FirstHeardSourceName
        {
            get { return _firstHeardSourceName; }
        }
        public bool ExcludeFromRollCall
        {
            get { return _excludeFromRollCall; }
            set
            {
                if (_excludeFromRollCall != value)
                {
                    _excludeFromRollCall = value;
                    MarkChangesMade();
                    OnPropertyChanged();
                }
            }
        }
    }
}

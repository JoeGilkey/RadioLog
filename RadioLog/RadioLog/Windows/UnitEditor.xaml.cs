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
    /// Interaction logic for UnitEditor.xaml
    /// </summary>
    public partial class UnitEditor : BaseRadioLogWindow
    {
        public RadioLog.WPFCommon.ThreadSafeObservableCollection<UnitInfoModel> Units { get; private set; }
        public List<RadioLog.Common.AgencyInfo> Agencies { get; private set; }

        public UnitEditor()
        {
            InitializeComponent();

            this.Agencies = new List<Common.AgencyInfo>();
            this.Agencies.Add(new Common.AgencyInfo(Guid.Empty, ""));
            foreach (Common.AgencyInfo aInfo in RadioLog.Common.RadioInfoLookupHelper.Instance.AgencyList.OrderBy(a => a.AgencyName))
            {
                this.Agencies.Add(aInfo);
            }

            this.Units = new WPFCommon.ThreadSafeObservableCollection<UnitInfoModel>();
            foreach (RadioLog.Common.UnitInfo uInfo in RadioLog.Common.RadioInfoLookupHelper.Instance.UnitList.OrderBy(u => u.UnitName))
            {
                Units.Add(new UnitInfoModel(uInfo));
            }
            colAgency.ItemsSource = this.Agencies;
            dgUnits.ItemsSource = this.Units;
        }

        private void btnAddUnit_Click(object sender, RoutedEventArgs e)
        {
            this.Units.Add(new UnitInfoModel() { UnitName = "NEW UNIT" });
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            bool bChangesMade = false;
            foreach (UnitInfoModel uInfo in this.Units)
            {
                if (uInfo.ChangesMade)
                {
                    RadioLog.Common.RadioInfoLookupHelper.Instance.AddOrUpdateUnit(uInfo.UnitId, uInfo.AgencyId, uInfo.UnitName);
                    bChangesMade = true;
                }
            }
            if (bChangesMade)
            {
                RadioLog.Common.RadioInfoLookupHelper.Instance.SaveInfo();
                Cinch.Mediator.Instance.NotifyColleaguesAsync<bool>("REFRESH_RADIO_LOOKUPS", true);
            }
            this.DialogResult = true;
        }
    }

    public class UnitInfoModel : RadioLog.WPFCommon.ThreadSafeNotificationObject
    {
        private Guid _unitId;
        private Guid _agencyId;
        private string _unitName;
        private bool _changesMade;

        public UnitInfoModel(RadioLog.Common.UnitInfo unit)
        {
            if (unit == null)
                throw new ArgumentNullException();
            this._unitId = unit.UnitKeyId;
            this._agencyId = unit.AgencyKeyId;
            this._unitName = unit.UnitName;
        }
        public UnitInfoModel()
        {
            _unitId = Guid.NewGuid();
            _agencyId = Guid.Empty;
            _unitName = string.Empty;
            _changesMade = true;
        }

        public void MarkChangesMade() { _changesMade = true; }
        public void ClearChangesMade() { _changesMade = false; }
        public bool ChangesMade { get { return _changesMade; } }

        public Guid UnitId { get { return _unitId; } }
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
        public string UnitName
        {
            get { return _unitName; }
            set
            {
                if (_unitName != value)
                {
                    _unitName = value;
                    MarkChangesMade();
                    OnPropertyChanged();
                }
            }
        }
    }
}

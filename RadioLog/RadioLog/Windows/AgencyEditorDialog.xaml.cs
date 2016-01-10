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
    /// Interaction logic for AgencyEditorDialog.xaml
    /// </summary>
    public partial class AgencyEditorDialog : BaseRadioLogWindow
    {
        public RadioLog.WPFCommon.ThreadSafeObservableCollection<AgencyInfoModel> Agencies { get; private set; }

        public AgencyEditorDialog()
        {
            InitializeComponent();

            this.Agencies = new WPFCommon.ThreadSafeObservableCollection<AgencyInfoModel>();
            foreach (RadioLog.Common.AgencyInfo aInfo in RadioLog.Common.RadioInfoLookupHelper.Instance.AgencyList.OrderBy(a => a.AgencyName))
            {
                Agencies.Add(new AgencyInfoModel(aInfo));
            }
            dgAgencies.ItemsSource = this.Agencies;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            bool bChangesMade = false;
            foreach (AgencyInfoModel aInfo in this.Agencies)
            {
                if (aInfo.ChangesMade)
                {
                    RadioLog.Common.RadioInfoLookupHelper.Instance.AddOrUpdateAgency(aInfo.AgencyId, aInfo.AgencyName);
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

        private void btnAddAgency_Click(object sender, RoutedEventArgs e)
        {
            this.Agencies.Add(new AgencyInfoModel() { AgencyName = "NEW AGENCY" });
        }
    }

    public class AgencyInfoModel : RadioLog.WPFCommon.ThreadSafeNotificationObject
    {
        private Guid _agencyId;
        private string _agencyName;
        private bool _changesMade;

        public AgencyInfoModel(RadioLog.Common.AgencyInfo agency)
        {
            if (agency == null)
                throw new ArgumentNullException();
            this._agencyId = agency.AgencyId;
            this._agencyName = agency.AgencyName;
        }
        public AgencyInfoModel()
        {
            _agencyId = Guid.NewGuid();
            _agencyName = string.Empty;
            _changesMade = true;
        }

        public void MarkChangesMade() { _changesMade = true; }
        public void ClearChangesMade() { _changesMade = false; }
        public bool ChangesMade { get { return _changesMade; } }

        public Guid AgencyId { get { return _agencyId; } }
        public string AgencyName
        {
            get { return _agencyName; }
            set
            {
                if (_agencyName != value)
                {
                    _agencyName = value;
                    MarkChangesMade();
                    OnPropertyChanged();
                }
            }
        }
    }
}

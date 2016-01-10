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
using System.Threading.Tasks;

namespace RadioLog.Windows
{
    /// <summary>
    /// Interaction logic for FindFeedLinksDialog.xaml
    /// </summary>
    public partial class FindFeedLinksDialog : BaseRadioLogWindow
    {
        private delegate void DefaultObjectDelegate(object o);
        private delegate void BoolMethodDelegate(bool b);
        private delegate void BoolStringMethodDelegate(bool b, string s);
        private RadioLog.Broadcastify.FeedAPI _feedAPI = new Broadcastify.FeedAPI();

        private RadioLog.WPFCommon.ThreadSafeObservableCollection<FoundFeedHolder> _feeds = new WPFCommon.ThreadSafeObservableCollection<FoundFeedHolder>();
        private RadioLog.WPFCommon.ThreadSafeObservableCollection<RadioLog.Broadcastify.FeedItemHolder> _rrFeeds = new WPFCommon.ThreadSafeObservableCollection<Broadcastify.FeedItemHolder>();

        public FindFeedLinksDialog()
        {
            InitializeComponent();

            tbRRUserName.Text = Common.AppSettings.Instance.RRUserName;
            tbRRPassword.Password = Common.AppSettings.Instance.RRPassword;

            dgFeeds.ItemsSource = _feeds;
            dgRRFeeds.ItemsSource = _rrFeeds;

            cbGenre.ItemsSource = _feedAPI.GetGenreList();
            cbGenre.SelectedValue = _feedAPI.LastGenre;
            cbGenre.IsEnabled = true;
            LaunchGetCountryList(true);
        }

        private void LaunchGetCountryList(bool bCachedOnly)
        {
            ShowProgress(true);
            if (bCachedOnly)
            {
                ProcessCountryList(_feedAPI.GetCachedCountries());
            }
            else
            {
                if (!(string.IsNullOrEmpty(tbRRUserName.Text) || string.IsNullOrEmpty(tbRRPassword.Password)))
                {
                    _feedAPI.UpdateUserInfo(tbRRUserName.Text, tbRRPassword.Password);
                }
                System.Threading.Tasks.Task t = new Task(() =>
                {
                    string errorMsg = string.Empty;
                    object o = _feedAPI.GetAllCountries(out errorMsg);
                    ProcessCountryList(o);
                    ShowErrorMessage(o == null, errorMsg);
                });
                t.Start();
            }
        }
        private void LaunchGetStateList(string countryCode)
        {
            ShowProgress(true);
            if (!(string.IsNullOrEmpty(tbRRUserName.Text) || string.IsNullOrEmpty(tbRRPassword.Password)))
            {
                _feedAPI.UpdateUserInfo(tbRRUserName.Text, tbRRPassword.Password);
            }
            if (string.IsNullOrEmpty(countryCode))
            {
                ProcessStateList(null);
            }
            else
            {
                System.Threading.Tasks.Task t = new Task(() =>
                {
                    string errorMsg = string.Empty;
                    object o = _feedAPI.GetAllStatesForCountry(countryCode, out errorMsg);
                    ProcessStateList(o);
                    ShowErrorMessage(o == null, errorMsg);
                });
                t.Start();
            }
        }
        private void LaunchGetCountyList(string stateCode)
        {
            ShowProgress(true);
            if (!(string.IsNullOrEmpty(tbRRUserName.Text) || string.IsNullOrEmpty(tbRRPassword.Password)))
            {
                _feedAPI.UpdateUserInfo(tbRRUserName.Text, tbRRPassword.Password);
            }
            if (string.IsNullOrWhiteSpace(stateCode))
            {
                ProcessCountyList(null);
            }
            else
            {
                System.Threading.Tasks.Task t = new Task(() =>
                {
                    string errorMsg = string.Empty;
                    object o = _feedAPI.GetAllCountiesForState(stateCode, out errorMsg);
                    ProcessCountyList(o);
                    ShowErrorMessage(o == null, errorMsg);
                });
                t.Start();
            }
        }
        private void ShowErrorMessage(bool bMsgVisible, string errorMsg)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new BoolStringMethodDelegate(ShowErrorMessage), bMsgVisible, errorMsg);
            }
            else
            {
                tbErrorMsg.Text = errorMsg;
                tbErrorMsg.Visibility = bMsgVisible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
        }
        private void ShowProgress(bool bProgressVisible)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new BoolMethodDelegate(ShowProgress), bProgressVisible);
            }
            else
            {
                if (bProgressVisible)
                {
                    pbProcessing.IsIndeterminate = true;
                    pbProcessing.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    pbProcessing.Visibility = System.Windows.Visibility.Collapsed;
                    pbProcessing.IsIndeterminate = false;
                }
            }
        }
        private void LaunchRRCountyFeedSearch(string countyId)
        {
            if (string.IsNullOrWhiteSpace(tbRRUserName.Text) || string.IsNullOrWhiteSpace(tbRRPassword.Password) || string.IsNullOrWhiteSpace(countyId))
                return;
            ShowProgress(true);
            _feedAPI.UpdateUserInfo(tbRRUserName.Text, tbRRPassword.Password);
            System.Threading.Tasks.Task t = new Task(() =>
            {
                string errorMsg = string.Empty;
                List<Broadcastify.FeedItemHolder> _rrRslt = _feedAPI.SearchForFeedsInCounty(countyId, out errorMsg);
                if (_rrRslt != null)
                {
                    _rrFeeds.Clear();
                    foreach (Broadcastify.FeedItemHolder f in _rrRslt)
                    {
                        _rrFeeds.Add(f);
                    }
                }
                ShowProgress(false);
                ShowErrorMessage(_rrRslt == null, errorMsg);
            });
            t.Start();
        }
        private void LaunchRRSearch()
        {
            if (string.IsNullOrWhiteSpace(tbRRUserName.Text) || string.IsNullOrWhiteSpace(tbRRPassword.Password))
                return;
            ShowProgress(true);
            RadioLog.Broadcastify.ItemIdHolder cntryHldr = cbCountries.SelectedItem as RadioLog.Broadcastify.ItemIdHolder;
            RadioLog.Broadcastify.ItemIdHolder stateHldr = cbStates.SelectedItem as RadioLog.Broadcastify.ItemIdHolder;
            RadioLog.Broadcastify.GenreListHolder gnreHldr = cbGenre.SelectedItem as RadioLog.Broadcastify.GenreListHolder;

            RadioLog.Broadcastify.Genre g = Broadcastify.Genre.All;
            if (gnreHldr != null)
                g = gnreHldr.Value;
            string cId = string.Empty;
            string sId = string.Empty;
            if (cntryHldr != null)
                cId = cntryHldr.ItemId;
            if (stateHldr != null)
                sId = stateHldr.ItemId;
            string filter = tbRRFilter.Text;
            _feedAPI.UpdateUserInfo(tbRRUserName.Text, tbRRPassword.Password);
            System.Threading.Tasks.Task t = new Task(() =>
            {
                string errorMsg = string.Empty;
                List<Broadcastify.FeedItemHolder> _rrRslt = _feedAPI.SearchForFeeds(filter, cId, sId, string.Empty, string.Empty, g, out errorMsg);
                if (_rrRslt != null)
                {
                    _rrFeeds.Clear();
                    foreach (Broadcastify.FeedItemHolder f in _rrRslt)
                    {
                        _rrFeeds.Add(f);
                    }
                }
                ShowProgress(false);
                ShowErrorMessage(_rrRslt == null, errorMsg);
            });
            t.Start();
        }

        private void ProcessCountryList(object o)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new DefaultObjectDelegate(ProcessCountryList), o);
            }
            else
            {
                RadioLog.Broadcastify.ItemIdHolder allItem = new Broadcastify.ItemIdHolder(string.Empty, "All", string.Empty);
                List<RadioLog.Broadcastify.ItemIdHolder> lst = o as List<RadioLog.Broadcastify.ItemIdHolder>;
                if (lst != null)
                {
                    if (lst.Count > 0)
                        lst.Insert(0, allItem);
                    else
                        lst.Add(allItem);
                }
                else
                {
                    lst = new List<Broadcastify.ItemIdHolder>();
                    lst.Add(allItem);
                }
                cbCountries.ItemsSource = lst;
                cbCountries.IsEnabled = true;
                cbCountries.SelectedValue = _feedAPI.LastCountry;
                if (!string.IsNullOrWhiteSpace(_feedAPI.LastCountry) && !string.IsNullOrWhiteSpace(_feedAPI.LastState))
                {
                    LaunchGetStateList(_feedAPI.LastCountry);
                }
                ShowProgress(false);
            }
        }
        private void ProcessStateList(object o)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new DefaultObjectDelegate(ProcessStateList), o);
            }
            else
            {
                RadioLog.Broadcastify.ItemIdHolder allItem = new Broadcastify.ItemIdHolder(string.Empty, "All", string.Empty);
                List<RadioLog.Broadcastify.ItemIdHolder> lst = o as List<RadioLog.Broadcastify.ItemIdHolder>;
                if (lst != null)
                {
                    if (lst.Count > 0)
                        lst.Insert(0, allItem);
                    else
                        lst.Add(allItem);
                }
                else
                {
                    lst = new List<Broadcastify.ItemIdHolder>();
                    lst.Add(allItem);
                }
                cbStates.ItemsSource = lst;
                cbStates.IsEnabled = (lst.Count > 1);
                cbStates.SelectedValue = _feedAPI.LastState;
                ShowProgress(false);
            }
        }
        private void ProcessCountyList(object o)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new DefaultObjectDelegate(ProcessCountyList), o);
            }
            else
            {
                RadioLog.Broadcastify.ItemIdHolder allItem = new Broadcastify.ItemIdHolder(string.Empty, "All", string.Empty);
                List<RadioLog.Broadcastify.ItemIdHolder> lst = o as List<RadioLog.Broadcastify.ItemIdHolder>;
                if (lst != null)
                {
                    if (lst.Count > 0)
                        lst.Insert(0, allItem);
                    else
                        lst.Add(allItem);
                }
                else
                {
                    lst = new List<Broadcastify.ItemIdHolder>();
                    lst.Add(allItem);
                }
                cbCounties.ItemsSource = lst;
                cbCounties.IsEnabled = (lst.Count > 1);
                cbCounties.SelectedIndex = 0;
                ShowProgress(false);
            }
        }

        private void btnFind_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbURL.Text))
                return;
            tbStatus.Text = string.Format("Parsing {0}...", tbURL.Text);
            pbProcessing.Visibility = System.Windows.Visibility.Visible;
            btnOk.IsEnabled = false;
            btnFind.IsEnabled = false;
            RadioLog.FeedUtils.FeedFinder feedFinder = new FeedUtils.FeedFinder(tbURL.Text, ProcessResults);
            feedFinder.ParsePage();
        }

        private void ProcessResults(List<string> feedResults)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new RadioLog.FeedUtils.FeedFinder.ProcessUrlCompleteDelegate(ProcessResults), feedResults);
            }
            else
            {
                int iTotalCnt = 0;
                int iFilteredCnt = 0;
                pbProcessing.Visibility = System.Windows.Visibility.Collapsed;
                btnOk.IsEnabled = true;
                btnFind.IsEnabled = true;
                _feeds.Clear();
                if (feedResults != null)
                {
                    foreach (string s in feedResults)
                    {
                        iTotalCnt++;
                        if (Common.AppSettings.Instance.SignalSources.FirstOrDefault(f => f.SourceLocation == s && f.SourceType == Common.SignalingSourceType.Streaming) == null)
                        {
                            _feeds.Add(new FoundFeedHolder() { FeedURL = s });
                        }
                        else
                        {
                            iFilteredCnt++;
                        }
                    }
                }
                tbStatus.Text = string.Format("{0} Feeds found, {1} already setup...", iTotalCnt, iFilteredCnt);
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnAddFeed_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = e.Source as FrameworkElement;
            if (fe == null)
                return;
            FoundFeedHolder holder = fe.DataContext as FoundFeedHolder;
            if (holder == null)
                return;
            DialogResult = true;
            NewSourceDialog.ShowNewSourceDialog(holder.FeedURL);
        }

        private void btnAddRRFeed_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = e.Source as FrameworkElement;
            if (fe == null)
                return;
            RadioLog.Broadcastify.FeedItemHolder holder = fe.DataContext as RadioLog.Broadcastify.FeedItemHolder;
            if (holder == null)
                return;
            DialogResult = true;
            NewSourceDialog.ShowNewSourceDialog(holder.FeedURL, holder.FeedName);
        }

        private void cbCountries_DropDownOpened(object sender, EventArgs e)
        {
            if (cbCountries.IsEnabled == false || cbCountries.ItemsSource == null || ((IList<RadioLog.Broadcastify.ItemIdHolder>)cbCountries.ItemsSource).Count < 2)
            {
                LaunchGetCountryList(false);
            }
        }

        private void cbCountries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RadioLog.Broadcastify.ItemIdHolder idHldr = cbCountries.SelectedItem as RadioLog.Broadcastify.ItemIdHolder;
            if (idHldr == null)
                LaunchGetStateList(string.Empty);
            else
                LaunchGetStateList(idHldr.ItemId);
        }

        private void btnRRSearchClick(object sender, RoutedEventArgs e)
        {
            SaveLastRRInfo();
            LaunchRRSearch();
        }

        private void BaseRadioLogWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Common.AppSettings.Instance.RRUserName = tbRRUserName.Text;
            Common.AppSettings.Instance.RRPassword = tbRRPassword.Password;
            SaveLastRRInfo();
        }

        private void tbRRFilter_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LaunchRRSearch();
            }
        }

        private void cbStates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RadioLog.Broadcastify.ItemIdHolder idHldr = cbStates.SelectedItem as RadioLog.Broadcastify.ItemIdHolder;
            if (idHldr == null)
                LaunchGetCountyList(string.Empty);
            else
                LaunchGetCountyList(idHldr.ItemId);
        }

        private void cbCounties_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RadioLog.Broadcastify.ItemIdHolder idHldr = cbCounties.SelectedItem as RadioLog.Broadcastify.ItemIdHolder;
            if (idHldr != null)
                LaunchRRCountyFeedSearch(idHldr.ItemId);
        }

        private void SaveLastRRInfo()
        {
            RadioLog.Broadcastify.ItemIdHolder cntryHldr = cbCountries.SelectedItem as RadioLog.Broadcastify.ItemIdHolder;
            RadioLog.Broadcastify.ItemIdHolder stateHldr = cbStates.SelectedItem as RadioLog.Broadcastify.ItemIdHolder;
            RadioLog.Broadcastify.GenreListHolder gnreHldr = cbGenre.SelectedItem as RadioLog.Broadcastify.GenreListHolder;

            RadioLog.Broadcastify.Genre g = Broadcastify.Genre.All;
            if (gnreHldr != null)
                g = gnreHldr.Value;
            string cId = string.Empty;
            string sId = string.Empty;
            if (cntryHldr != null)
                cId = cntryHldr.ItemId;
            if (stateHldr != null)
                sId = stateHldr.ItemId;
            _feedAPI.UpdateUserInfo(tbRRUserName.Text, tbRRPassword.Password);
            _feedAPI.LastCountry = cId;
            _feedAPI.LastState = sId;
            _feedAPI.LastGenre = g;
        }
    }

    public class FoundFeedHolder
    {
        public string FeedURL { get; set; }
    }
}

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
    /// Interaction logic for RollCallView.xaml
    /// </summary>
    public partial class RollCallView : BaseRadioLogWindow
    {
        public RollCallView(ViewModels.MainViewModel mainViewModel)
        {
            InitializeComponent();

            DataContext = mainViewModel;

            this.Closed += (s, e) =>
            {
                RollCallService.RollCallActive = false;
            };

            RollCallService.RollCallActive = true;

            lbGood.ItemsSource = RollCallService.GoodList;
            lbWaiting.ItemsSource = RollCallService.WaitingList;

            RollCallService.DoRestartRollCall();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DoCloseClick();
        }

        private ViewModels.MainViewModel MainViewModel { get { return this.DataContext as ViewModels.MainViewModel; } }
        private Services.RollCallService RollCallService { get { return this.MainViewModel.RollCallService; } }

        private ViewModels.RollCallItem GetRollCallItemFromDrop(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(WPFCommon.DragDropProviderListBox.DEFAULT_FORMATSTRING))
                return null;
            else
                return e.Data.GetData(WPFCommon.DragDropProviderListBox.DEFAULT_FORMATSTRING) as ViewModels.RollCallItem;
        }

        void DoCloseClick()
        {
            if (RollCallService.ParticipantsExist)
            {
                Common.DoNotAskYesNo ask = Common.AppSettings.Instance.GetShouldAsk("ClearRollCallOnClose");
                if (ask == Common.DoNotAskYesNo.Ask)
                {
                    bool bDoNotShow = false;
                    MessageBoxResult rslt = Common.MessageBoxHelper.Show("Should the current list or radios be cleared?", "Close Roll Call", MessageBoxButton.YesNoCancel, MessageBoxResult.No, true, out bDoNotShow);
                    if (rslt == MessageBoxResult.Cancel)
                        return;
                    if (rslt == MessageBoxResult.Yes)
                    {
                        RollCallService.DoClearAll();
                        if (bDoNotShow)
                        {
                            Common.AppSettings.Instance.SetShouldAsk("ClearRollCallOnClose", Common.DoNotAskYesNo.Yes);
                        }
                    }
                    else if (rslt == MessageBoxResult.No)
                    {
                        if (bDoNotShow)
                        {
                            Common.AppSettings.Instance.SetShouldAsk("ClearRollCallOnClose", Common.DoNotAskYesNo.No);
                        }
                    }
                }
                else if (ask == Common.DoNotAskYesNo.Yes)
                {
                    RollCallService.DoClearAll();
                }
            }

            this.Close();
        }

        private void ListBox_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(WPFCommon.DragDropProviderListBox.DEFAULT_FORMATSTRING))
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void lbWaiting_Drop(object sender, DragEventArgs e)
        {
            ViewModels.RollCallItem rci = GetRollCallItemFromDrop(e);
            if (rci == null)
                return;
            RollCallService.AddToWaitingList(rci);
        }
        
        private void lbGood_Drop(object sender, DragEventArgs e)
        {
            ViewModels.RollCallItem rci = GetRollCallItemFromDrop(e);
            if (rci == null)
                return;
            RollCallService.AddToGoodList(rci);
        }

        private void lbWaiting_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewModels.RollCallItem item = lbWaiting.SelectedItem as ViewModels.RollCallItem;
            if (item == null)
                return;
            RollCallService.AddToGoodList(item);
        }

        private void lbGood_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewModels.RollCallItem item = lbGood.SelectedItem as ViewModels.RollCallItem;
            if (item == null)
                return;
            RollCallService.AddToWaitingList(item);
        }

        private void btnRemoveFromRollcallClick(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null)
                return;
            ViewModels.RollCallItem rci = btn.Tag as ViewModels.RollCallItem;
            if (rci == null)
                return;
            if (Common.MessageBoxHelper.Show(string.Format("Are you sure you wamt to remove {0} from roll call?", rci.DisplayName), "Remove from Roll Call", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                RollCallService.RemoveFromRollCall(rci);
            }
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (Common.MessageBoxHelper.Show("Are you sure you want to remove all radios from roll call?", "Clear Roll Call", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                RollCallService.DoClearAll();
            }
        }

        private void btnAddToRollCall_Click(object sender, RoutedEventArgs e)
        {
            Common.RadioInfo[] riList = SelectRadiosWindow.DoRadioSelection();
            if (riList != null && riList.Length > 0)
            {
                foreach (Common.RadioInfo ri in riList)
                {
                    if (RollCallService.GetItemFromLookupKey(ri.SignalingLookupKey) == null)
                    {
                        RollCallService.AddToWaitingList(new ViewModels.RollCallItem(ri, Common.EmergencyState.NonEmergency));
                    }
                }
            }
        }

        private void btnRestartRollCall_Click(object sender, RoutedEventArgs e)
        {
            RollCallService.DoRestartRollCall();
        }
    }
}

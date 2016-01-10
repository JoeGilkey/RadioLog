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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RadioLog.Views
{
    /// <summary>
    /// Interaction logic for LogDisplay.xaml
    /// </summary>
    public partial class LogDisplay : BaseMainWindowView
    {
        public LogDisplay()
        {
            InitializeComponent();
        }

        public override void SetupDisplay()
        {
            base.SetupDisplay();

            MainViewModel.RadioLog.ItemPropertyChanged += RadioLog_ItemPropertyChanged;

            SetupColumnVisibility();
        }

        public override void SetupColumnVisibility()
        {
            base.SetupColumnVisibility();
            colSourceName.Visibility = RadioLog.Common.AppSettings.Instance.ShowSourceName ? Visibility.Visible : Visibility.Collapsed;
            colUnitID.Visibility = RadioLog.Common.AppSettings.Instance.ShowUnitId ? Visibility.Visible : Visibility.Collapsed;
            colAgencyName.Visibility = RadioLog.Common.AppSettings.Instance.ShowAgencyName ? Visibility.Visible : Visibility.Collapsed;
            colUnitName.Visibility = RadioLog.Common.AppSettings.Instance.ShowUnitName ? Visibility.Visible : Visibility.Collapsed;
            colRadioName.Visibility = RadioLog.Common.AppSettings.Instance.ShowRadioName ? Visibility.Visible : Visibility.Collapsed;
            colAssignedRole.Visibility = RadioLog.Common.AppSettings.Instance.ShowAssignedRole ? Visibility.Visible : Visibility.Collapsed;
            colAssignedPersonnel.Visibility = RadioLog.Common.AppSettings.Instance.ShowAssignedPersonnel ? Visibility.Visible : Visibility.Collapsed;
            colSourceType.Visibility = RadioLog.Common.AppSettings.Instance.ShowSourceType ? Visibility.Visible : Visibility.Collapsed;
            colDescription.Visibility = RadioLog.Common.AppSettings.Instance.ShowDescription ? Visibility.Visible : Visibility.Collapsed;

            if (RadioLog.Common.AppSettings.Instance.WorkstationMode == Common.RadioLogMode.Fireground)
            {
                colBlank.Visibility = System.Windows.Visibility.Visible;
                colStartMayday.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                colBlank.Visibility = System.Windows.Visibility.Collapsed;
                colStartMayday.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
        public override void SetupColumnWidths()
        {
            base.SetupColumnWidths();
            colTime.Width = MainViewModel.ColTimeWidth;
            colSourceName.Width = MainViewModel.ColSourceNameWidth;
            colUnitID.Width = MainViewModel.ColUnitIdWidth;
            colAgencyName.Width = MainViewModel.ColAgencyNameWidth;
            colUnitName.Width = MainViewModel.ColUnitNameWidth;
            colRadioName.Width = MainViewModel.ColRadioNameWidth;
            colAssignedRole.Width = MainViewModel.ColAssignedRoleWidth;
            colAssignedPersonnel.Width = MainViewModel.ColAssignedPersonnelWidth;
            colSourceType.Width = MainViewModel.ColSourceTypedWidth;
            colDescription.Width = MainViewModel.ColDescriptionWidth;
        }
        public override void SaveColumnWidths()
        {
            base.SaveColumnWidths();

            MainViewModel.ColTimeWidth = colTime.Width;
            MainViewModel.ColSourceNameWidth = colSourceName.Width;
            MainViewModel.ColUnitIdWidth = colUnitID.Width;
            MainViewModel.ColAgencyNameWidth = colAgencyName.Width;
            MainViewModel.ColUnitNameWidth = colUnitName.Width;
            MainViewModel.ColRadioNameWidth = colRadioName.Width;
            MainViewModel.ColAssignedRoleWidth = colAssignedRole.Width;
            MainViewModel.ColAssignedPersonnelWidth = colAssignedPersonnel.Width;
            MainViewModel.ColSourceTypedWidth = colSourceType.Width;
            MainViewModel.ColDescriptionWidth = colDescription.Width;
        }
        public override void ClearSelectedItem()
        {
            base.ClearSelectedItem();
            dgLog.SelectedItem = null;
        }

        Brush GetRowBackgroundColor(Common.EmergencyState emergencyState, bool bIsSelected)
        {
            if (Common.AppSettings.Instance.WorkstationMode != Common.RadioLogMode.Fireground)
                return Brushes.Transparent;
            if (emergencyState == Common.EmergencyState.EmergencyActive)
                return Brushes.Red;
            else if (bIsSelected)
                return Brushes.Orange;
            else
                return Brushes.Transparent;
        }
        Brush GetRowForegroundColor(Common.EmergencyState emergencyState, bool bIsSelected, Brush curBrush)
        {
            if (emergencyState == Common.EmergencyState.EmergencyActive && Common.AppSettings.Instance.WorkstationMode == Common.RadioLogMode.Fireground)
                return Brushes.White;
            else if (bIsSelected)
                return Brushes.Black;
            else
                return curBrush;
        }
        private void HandleEmergencyGridView(object sender, DataGridRowEventArgs e, bool bIsSelected)
        {
            Common.IRadioContactEmergency emer = e.Row.DataContext as Common.IRadioContactEmergency;
            if (emer != null)
            {
                e.Row.Background = GetRowBackgroundColor(emer.EmergencyState, bIsSelected);
                e.Row.Foreground = GetRowForegroundColor(emer.EmergencyState, bIsSelected, e.Row.Foreground);
            }
        }
        private void UpdateRadioCallSelection(Common.IRadioContactEmergency rcvm, bool bSelected)
        {
            if (rcvm != null)
            {
                rcvm.CanDoManualMayday = bSelected && rcvm.EmergencyState != Common.EmergencyState.EmergencyActive;
                DataGridRow dgr = dgLog.ItemContainerGenerator.ContainerFromItem(rcvm) as DataGridRow;
                if (dgr != null)
                {
                    dgr.Background = GetRowBackgroundColor(rcvm.EmergencyState, bSelected);
                    dgr.Foreground = GetRowForegroundColor(rcvm.EmergencyState, bSelected, dgr.Foreground);
                }
            }
        }

        void RadioLog_ItemPropertyChanged(object sender, WPFCommon.ItemPropertyChangedEventArgs<ViewModels.RadioSignalItemModel> e)
        {
            if (e.PropertyName == "EmergencyState")
            {
                DataGridRow dgr = dgLog.ItemContainerGenerator.ContainerFromItem(e.Item) as DataGridRow;
                if (dgr != null)
                {
                    dgr.Background = GetRowBackgroundColor(e.Item.EmergencyState, dgr.IsSelected);
                    dgr.Foreground = GetRowForegroundColor(e.Item.EmergencyState, dgr.IsSelected, dgr.Foreground);
                }
            }
        }

        private void dtEditRadio_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            if (fe == null || fe.Tag == null)
                return;
            ViewModels.RadioSignalItemModel radioModel = fe.Tag as ViewModels.RadioSignalItemModel;
            if (radioModel == null)
                return;
            Windows.SingleRadioEditorWindow editor = new Windows.SingleRadioEditorWindow(radioModel);
            editor.ShowDialog();
        }
        private void StartMaydayButton_Click(object sender, RoutedEventArgs e)
        {
            dgLog.SelectedItem = null;
            FrameworkElement fe = sender as FrameworkElement;
            if (fe == null || fe.Tag == null)
                return;
            Common.IRadioContactEmergency emer = fe.Tag as Common.IRadioContactEmergency;
            if (emer == null)
                return;
            ViewModels.MainViewModel.StartMaydayForRadio(emer.SignalingLookupKey);
        }

        private void dgLog_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            HandleEmergencyGridView(sender, e, e.Row.IsSelected);
        }
        private void dgLog_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems != null && e.RemovedItems.Count > 0)
            {
                foreach (object o in e.RemovedItems)
                {
                    UpdateRadioCallSelection(o as Common.IRadioContactEmergency, false);
                }
            }
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                foreach (object o in e.AddedItems)
                {
                    UpdateRadioCallSelection(o as Common.IRadioContactEmergency, true);
                }
            }
        }
    }
}

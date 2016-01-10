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
    /// Interaction logic for AlarmsDisplay.xaml
    /// </summary>
    public partial class AlarmsDisplay : BaseMainWindowView
    {
        public AlarmsDisplay()
        {
            InitializeComponent();
        }

        public override void SetupDisplay()
        {
            base.SetupDisplay();

            bEmergencies.Visibility = (this.MainViewModel.EmergencyRadioLog.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
            bClearEmergencies.Visibility = bEmergencies.Visibility;

            MainViewModel.EmergencyRadioLog.CollectionChanged += EmergencyRadioLog_CollectionChanged;
            MainViewModel.EmergencyRadioLog.ItemPropertyChanged += EmergencyRadioLog_ItemPropertyChanged;

            SetupColumnVisibility();
        }

        public override void SetupColumnWidths()
        {
            base.SetupColumnWidths();

            colAlarmTime.Width = MainViewModel.ColAlarmTimeWidth;
            colAlarmUnitName.Width = MainViewModel.ColAlarmUnitNameWidth;
            colAlarmRadioName.Width = MainViewModel.ColAlarmRadioNameWidth;
            colAlarmAssignedRole.Width = MainViewModel.ColAlarmAssignedRoleWidth;
            colAlarmAssignedPersonnel.Width = MainViewModel.ColAlarmAssignedPersonnelWidth;
        }
        public override void SaveColumnWidths()
        {
            base.SaveColumnWidths();

            MainViewModel.ColAlarmTimeWidth = colAlarmTime.Width;
            MainViewModel.ColAlarmUnitNameWidth = colAlarmUnitName.Width;
            MainViewModel.ColAlarmRadioNameWidth = colAlarmRadioName.Width;
            MainViewModel.ColAlarmAssignedRoleWidth = colAlarmAssignedRole.Width;
            MainViewModel.ColAlarmAssignedPersonnelWidth = colAlarmAssignedPersonnel.Width;
        }

        public override void ClearSelectedItem()
        {
            base.ClearSelectedItem();
            dgAlarms.SelectedItem = null;
        }
        
        public override void SetupColumnVisibility()
        {
            base.SetupColumnVisibility();

            colAlarmUnitName.Visibility = RadioLog.Common.AppSettings.Instance.ShowUnitName ? Visibility.Visible : Visibility.Collapsed;
            colAlarmRadioName.Visibility = RadioLog.Common.AppSettings.Instance.ShowRadioName ? Visibility.Visible : Visibility.Collapsed;
            colAlarmAssignedRole.Visibility = RadioLog.Common.AppSettings.Instance.ShowAssignedRole ? Visibility.Visible : Visibility.Collapsed;
            colAlarmAssignedPersonnel.Visibility = RadioLog.Common.AppSettings.Instance.ShowAssignedPersonnel ? Visibility.Visible : Visibility.Collapsed;
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
                DataGridRow dgr = dgAlarms.ItemContainerGenerator.ContainerFromItem(rcvm) as DataGridRow;
                if (dgr != null)
                {
                    dgr.Background = GetRowBackgroundColor(rcvm.EmergencyState, bSelected);
                    dgr.Foreground = GetRowForegroundColor(rcvm.EmergencyState, bSelected, dgr.Foreground);
                }
            }
        }

        void EmergencyRadioLog_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            bEmergencies.Visibility = (MainViewModel.EmergencyRadioLog.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
            bClearEmergencies.Visibility = bEmergencies.Visibility;
        }
        void EmergencyRadioLog_ItemPropertyChanged(object sender, WPFCommon.ItemPropertyChangedEventArgs<ViewModels.EmergencyRadioSignalItemModel> e)
        {
            if (e.PropertyName == "EmergencyState")
            {
                DataGridRow dgr = dgAlarms.ItemContainerGenerator.ContainerFromItem(e.Item) as DataGridRow;
                if (dgr != null)
                {
                    dgr.Background = GetRowBackgroundColor(e.Item.EmergencyState, dgr.IsSelected);
                    dgr.Foreground = GetRowForegroundColor(e.Item.EmergencyState, dgr.IsSelected, dgr.Foreground);
                }
            }
        }
        private void bClearEmergencies_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.MainViewModel.MarkAllEmergenciesCleared();
        }
        private void bEmergencies_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.MainViewModel.RemoveAllEmergencies();
        }
        private void dg_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            HandleEmergencyGridView(sender, e, false);
        }
        private void dg_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dgAlarms.SelectedItem = null;
        }
        private void ClearIndividualButton_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            if (fe == null || fe.Tag == null)
                return;
            ViewModels.EmergencyRadioSignalItemModel emer = fe.Tag as ViewModels.EmergencyRadioSignalItemModel;
            if (emer == null)
                return;
            ViewModels.MainViewModel.ClearMaydayForRadio(emer);
            UpdateRadioCallSelection(emer, true);
        }
    }
}

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
    /// Interaction logic for FiregroundDisplay.xaml
    /// </summary>
    public partial class FiregroundDisplay : BaseMainWindowView
    {
        private BaseMainWindowView _mainView = null;

        public FiregroundDisplay()
        {
            InitializeComponent();
        }

        public override void SetupDisplay()
        {
            if (_mainView == null)
            {
                switch (Common.AppSettings.Instance.MainDisplayStyle)
                {
                    case Common.DisplayStyle.TabbedInterface: _mainView = new Views.TabbedLayoutView(); break;
                    case Common.DisplayStyle.GridsOnlyInterface: _mainView = new Views.GridsOnlyView(); break;
                    case Common.DisplayStyle.AllInOne: _mainView = new Views.AllInOneView(); break;
                    case Common.DisplayStyle.GridsAutoOpenSources: _mainView = new Views.GridsOnlyView(); break;
                }

                if (_mainView != null)
                {
                    Grid.SetRow(_mainView, 1);
                    Grid.SetColumn(_mainView, 0);
                    _mainView.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                    _mainView.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                    mainGrid.Children.Add(_mainView);
                }
            }

            base.SetupDisplay();

            vwUnitsOnScene.SetupDisplay();

            if (_mainView != null)
            {
                _mainView.SetupDisplay();
            }
        }
        public override void SetupColumnVisibility()
        {
            base.SetupColumnVisibility();
            vwUnitsOnScene.SetupColumnVisibility();
            if (_mainView != null)
            {
                _mainView.SetupColumnVisibility();
            }
        }
        public override void SetupColumnWidths()
        {
            base.SetupColumnWidths();
            vwUnitsOnScene.SetupColumnWidths();
            if (_mainView != null)
            {
                _mainView.SetupColumnWidths();
            }
        }
        public override void SaveColumnWidths()
        {
            base.SaveColumnWidths();
            vwUnitsOnScene.SaveColumnWidths();
            if (_mainView != null)
            {
                _mainView.SaveColumnWidths();
            }
        }
        public override void ClearSelectedItem()
        {
            base.ClearSelectedItem();
            vwUnitsOnScene.ClearSelectedItem();
            if (_mainView != null)
            {
                _mainView.ClearSelectedItem();
            }
        }

        private void bAllRadios_Click(object sender, RoutedEventArgs e)
        {
            Windows.RadioEditorDialog dlg = new Windows.RadioEditorDialog();
            dlg.ShowDialog();
        }

        private void bBeginRollCall_Click(object sender, RoutedEventArgs e)
        {
            if (!MainViewModel.RollCallService.RollCallActive)
            {
                Windows.RollCallView rcv = new Windows.RollCallView(MainViewModel);
                rcv.Show();
            }
        }

        private void bClearScreen_Click(object sender, RoutedEventArgs e)
        {
            if (Common.MessageBoxHelper.Show("Are you sure you want to clear the screen?", "Clear Screen", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                MainViewModel.DoClearDisplay();
            }
        }

        private void bManualMayday_Click(object sender, RoutedEventArgs e)
        {
            Common.RadioInfo[] riList = Windows.SelectRadiosWindow.DoRadioSelection();
            if (riList != null)
            {
                foreach (Common.RadioInfo ri in riList)
                {
                    ViewModels.MainViewModel.StartMaydayForRadio(ri.SignalingLookupKey);
                }
            }
        }
    }
}

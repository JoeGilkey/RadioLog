using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;

using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace RadioLog
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow, Common.IMainWindowDisplayProvider
    {
        private int _currentOverlayCount = 0;
        private bool _settingsErrorDisplayed = false;

        private BaseMainWindowView _mainView = null;
        private Windows.SourcesDisplayWindow _sourcesDisplay = null;

        delegate void SimpleDelegate();
        delegate void SimpleBoolDelegate(bool b);
        delegate void SimpleStringDelegate(string s);
        delegate void AppUpdateActiveDelegate(bool bActive);

        public MainWindow()
        {
            InitializeComponent();

            this.Title = App.AssemblyDisplayName;

            Cinch.Mediator.Instance.RegisterHandler<bool>("SETTINGS_CHANGED", (b) =>
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(new SimpleDelegate(SetupColumnVisibility));
                }
                else
                {
                    SetupColumnVisibility();

                    if (Common.AppSettings.HasSaveError)
                    {
                        ProcessSettingsError();
                    }
                }
            });

            Cinch.Mediator.Instance.RegisterHandler<bool>("OVERLAY_VISIBLE_CHANGED", (b) =>
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(new SimpleBoolDelegate(OverlayVisibleChanged), b);
                }
                else
                {
                    OverlayVisibleChanged(b);
                }
            });

            Cinch.Mediator.Instance.RegisterHandler<string>("CLIPBOARD_TEXT_CHANGED", (s) =>
            {
                ProcessClipboardStreamURL(s);
            });

            Common.MessageBoxHelper.SetMessageBoxProvider(new Windows.MessageBoxProvider());

#if DEBUG
            this.Loaded += (s, e) =>
            {
                this.WindowState = System.Windows.WindowState.Normal;
                this.Width = 1024;
                this.Height = 768;
            };

            this.SizeChanged += (s, e) =>
            {
                this.Title = string.Format("{0} - (w:{1},h{2})", App.AssemblyDisplayName, this.Width, this.Height);
            };
#endif

            SetupMainView();
        }

        void ProcessSettingsError()
        {
            if (!Common.AppSettings.HasSaveError||_settingsErrorDisplayed)
                return;
            if (Common.AppSettings.HasSaveError)
            {
                _settingsErrorDisplayed = true;
                Common.MessageBoxHelper.Show("There was an error saving your settings, please make sure you installed with administrative rights.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
            }
        }

        public BaseMainWindowView MainView { get { return _mainView; } }
        void SetupMainView()
        {
            if (_mainView == null)
            {
                if (Common.AppSettings.Instance.WorkstationMode == Common.RadioLogMode.Fireground)
                {
                    _mainView = new Views.FiregroundDisplay();
                }
                else
                {
                    switch (Common.AppSettings.Instance.MainDisplayStyle)
                    {
                        case Common.DisplayStyle.TabbedInterface: _mainView = new Views.TabbedLayoutView(); break;
                        case Common.DisplayStyle.GridsOnlyInterface: _mainView = new Views.GridsOnlyView(); break;
                        case Common.DisplayStyle.AllInOne: _mainView = new Views.AllInOneView(); break;
                        case Common.DisplayStyle.GridsAutoOpenSources: _mainView = new Views.GridsOnlyView(); break;
                        case Common.DisplayStyle.Columns: _mainView = new Views.ColumnLayoutView(); break;
                    }
                }
            }

            if (_mainView != null)
            {
                _mainView.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                _mainView.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                mainGrid.Children.Add(_mainView);

                _mainView.SetupDisplay();
                _mainView.SetupColumnVisibility();
            }

            if(Common.AppSettings.Instance.MainDisplayStyle== Common.DisplayStyle.GridsAutoOpenSources)
            {
                ShowSourcesDisplay();
            }
        }
        void OverlayVisibleChanged(bool bVisible)
        {
            if (bVisible)
            {
                if (_currentOverlayCount <= 0)
                {
                    _currentOverlayCount = 0;
                    this.ShowOverlay();
                }
                _currentOverlayCount++;
            }
            else
            {
                _currentOverlayCount--;
                if (_currentOverlayCount <= 0)
                {
                    _currentOverlayCount = 0;
                    this.HideOverlay();
                }
            }
        }

        internal void ProcessClipboardStreamURL(string s)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new SimpleStringDelegate(ProcessClipboardStreamURL), s);
            }
            else
            {
                Windows.NewSourceDialog.ShowNewSourceDialog(s);
            }
        }
        
        private void SetupColumnVisibility()
        {
            if (_mainView != null)
            {
                _mainView.SetupColumnVisibility();
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            settingsView.IsOpen = !settingsView.IsOpen;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseSourcesDisplay();

            (this.DataContext as ViewModels.MainViewModel).SaveVisuals(this);
            (this.DataContext as ViewModels.MainViewModel).ShutdownSources();
        }

        private void btnNewSource_Click(object sender, RoutedEventArgs e)
        {
            Windows.NewSourceDialog.ShowNewSourceDialog();
        }
        
        private void StartMaydayButton_Click(object sender, RoutedEventArgs e)
        {
            _mainView.ClearSelectedItem();
            FrameworkElement fe = sender as FrameworkElement;
            if (fe == null || fe.Tag == null)
                return;
            Common.IRadioContactEmergency emer = fe.Tag as Common.IRadioContactEmergency;
            if (emer == null)
                return;
            ViewModels.MainViewModel.StartMaydayForRadio(emer.SignalingLookupKey);
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
        
        private void btnFindFeeds_Click(object sender, RoutedEventArgs e)
        {
            Windows.FindFeedLinksDialog dlg = new Windows.FindFeedLinksDialog();
            dlg.ShowDialog();
        }

        internal void CloseSourcesDisplay()
        {
            if (_sourcesDisplay == null)
                return;

            try
            {
                if (_sourcesDisplay.IsVisible)
                    _sourcesDisplay.Close();
            }
            finally
            {
                _sourcesDisplay = null;
            }
        }
        internal void ShowSourcesDisplay()
        {
            if (_sourcesDisplay == null)
            {
                _sourcesDisplay = new Windows.SourcesDisplayWindow(this.DataContext as ViewModels.MainViewModel);
            }
            _sourcesDisplay.Show();
            _sourcesDisplay.Focus();
            _sourcesDisplay.Visibility = System.Windows.Visibility.Visible;
        }

        private void btnSources_Click(object sender, RoutedEventArgs e)
        {
            ShowSourcesDisplay();
        }
    }
}
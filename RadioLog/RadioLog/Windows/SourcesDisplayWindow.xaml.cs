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
    /// Interaction logic for SourcesDisplayWindow.xaml
    /// </summary>
    public partial class SourcesDisplayWindow : BasePositionSavingWindow, Common.IMainWindowDisplayProvider
    {
        protected override string WindowSavingName { get { return "SourcesWindow"; } }

        public SourcesDisplayWindow(ViewModels.MainViewModel mainViewModel)
            : base()
        {
            this.DataContext = mainViewModel;

            InitializeComponent();

            vwSources.SetupDisplay();
            vwSources.SetViewSplitBottom();
        }
        
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = (App.Current.MainWindow as MainWindow);
            if (mw == null)
            {
                this.Close();
            }
            else
            {
                mw.CloseSourcesDisplay();
            }
        }
    }
}

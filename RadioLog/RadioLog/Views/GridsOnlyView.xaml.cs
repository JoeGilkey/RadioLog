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
    /// Interaction logic for GridsOnlyView.xaml
    /// </summary>
    public partial class GridsOnlyView : BaseMainWindowView
    {
        public GridsOnlyView()
        {
            InitializeComponent();
        }

        public override void SetupDisplay()
        {
            base.SetupDisplay();

            vwAlarmsDisplay.SetupDisplay();
            vwLogDisplay.SetupDisplay();
        }
        public override void SetupColumnVisibility()
        {
            base.SetupColumnVisibility();

            if (RadioLog.Common.AppSettings.Instance.WorkstationMode == Common.RadioLogMode.Fireground)
            {
                vwAlarmsDisplay.Visibility = System.Windows.Visibility.Visible;
                gsLog.Visibility = System.Windows.Visibility.Visible;
                Grid.SetRowSpan(vwLogDisplay, 1);
            }
            else
            {
                vwAlarmsDisplay.Visibility = System.Windows.Visibility.Collapsed;
                gsLog.Visibility = System.Windows.Visibility.Collapsed;
                Grid.SetRowSpan(vwLogDisplay, 3);
            }

            vwAlarmsDisplay.SetupColumnVisibility();
            vwLogDisplay.SetupColumnVisibility();
        }
        public override void SetupColumnWidths()
        {
            base.SetupColumnWidths();

            vwAlarmsDisplay.SetupColumnWidths();
            vwLogDisplay.SetupColumnWidths();
        }
        public override void SaveColumnWidths()
        {
            base.SaveColumnWidths();

            vwAlarmsDisplay.SaveColumnWidths();
            vwLogDisplay.SaveColumnWidths();
        }
        public override void ClearSelectedItem()
        {
            base.ClearSelectedItem();

            vwAlarmsDisplay.ClearSelectedItem();
            vwLogDisplay.ClearSelectedItem();
        }
    }
}

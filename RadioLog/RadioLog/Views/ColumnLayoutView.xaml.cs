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
    /// Interaction logic for ColumnLayoutView.xaml
    /// </summary>
    public partial class ColumnLayoutView : BaseMainWindowView
    {
        public ColumnLayoutView()
        {
            InitializeComponent();
        }

        public override void SetupDisplay()
        {
            base.SetupDisplay();

            vwSources.SetupDisplay();
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

        RadioLog.ViewModels.BaseSourceModel GetTargetModelFromDrop(object sender, DragEventArgs e)
        {
            ItemsControl ic = sender as ItemsControl;
            if (ic == null)
            {
                GroupBox gb = sender as GroupBox;
                if (gb != null)
                {
                    ic = gb.Content as ItemsControl;
                }
            }
            if (ic != null && (sender as IInputElement) != null)
            {
                IInputElement ii = ic.InputHitTest(e.GetPosition(sender as IInputElement));
                if (ii != null)
                {
                    return GetSourceModelDataContext(ii);
                }
            }
            return null;
        }
        RadioLog.ViewModels.BaseSourceModel GetSourceModelDataContext(object o)
        {
            FrameworkElement fe = o as FrameworkElement;
            if (fe == null)
                return null;
            RadioLog.ViewModels.BaseSourceModel src = fe.DataContext as RadioLog.ViewModels.BaseSourceModel;
            if (src == null)
            {
                return GetSourceModelDataContext(fe.Parent);
            }
            else
            {
                return src;
            }
        }
        RadioLog.ViewModels.BaseSourceModel GetSourceModelFromDrop(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(RadioLog.WPFCommon.DragDropProviderItemsControl.DEFAULT_FORMATSTRING))
                return null;
            else
                return e.Data.GetData(RadioLog.WPFCommon.DragDropProviderItemsControl.DEFAULT_FORMATSTRING) as RadioLog.ViewModels.BaseSourceModel;
        }
        RadioLog.ViewModels.SourceGroupModel GetGroupModelFromSender(object sender)
        {
            if (sender == null)
                return null;
            FrameworkElement fe = sender as FrameworkElement;
            if (fe == null)
                return null;
            return fe.DataContext as RadioLog.ViewModels.SourceGroupModel;
        }
        private object GetDataContectFromObject(object o)
        {
            if (o == null)
                return null;
            FrameworkElement fe = o as FrameworkElement;
            if (fe == null)
                return null;
            return fe.DataContext;
        }
        private object GetDataContextFromButtonClick(object sender, RoutedEventArgs e)
        {
            object dataCtx = GetDataContectFromObject(sender);
            if (dataCtx == null)
            {
                dataCtx = GetDataContectFromObject(e.Source);
            }
            if (dataCtx == null)
            {
                dataCtx = GetDataContectFromObject(e.OriginalSource);
            }
            return dataCtx;
        }
    }
}

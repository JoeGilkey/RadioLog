using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;

namespace RadioLog.WPFCommon
{
    public class DragDropProviderDataGrid : DataGrid
    {
        public const string DEFAULT_FORMATSTRING = "RadioLogGridCommonData";

        public DragDropProviderDataGrid()
            : base()
        {
            this.DragDropFormatString = DEFAULT_FORMATSTRING;
        }

        public string DragDropFormatString { get; set; }

        public static T FindAnchestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private Point _startPoint;
        private DataGridRow _dragListItem = null;

        protected override void OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            if (string.IsNullOrEmpty(DragDropFormatString))
                return;
            _startPoint = e.GetPosition(null);
            DependencyObject d = e.OriginalSource as DependencyObject;
            if (d == null)
                return;
            _dragListItem = FindAnchestor<DataGridRow>(d);
        }
        protected override void OnPreviewMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            if (string.IsNullOrEmpty(DragDropFormatString) || _dragListItem == null)
                return;
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                //object dataObj = this.ItemContainerGenerator.ItemFromContainer(_dragListItem);
                object dataObj = _dragListItem.DataContext;
                DataObject dragDataObj = new DataObject(DragDropFormatString, dataObj);
                DragDrop.DoDragDrop(_dragListItem, dragDataObj, DragDropEffects.Move);
            }
        }
        protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);

            e.UseDefaultCursors = true;
        }
    }
}

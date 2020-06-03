using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RadioLog.WPFCommon
{
    public class DragDropProviderItemsControl : ItemsControl
    {
        public const string DEFAULT_FORMATSTRING = "RadioLogCommonData";

        public DragDropProviderItemsControl()
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
        private FrameworkElement GetAncestorWithData(FrameworkElement current)
        {
            if (current == null)
                return null;
            do
            {
                if (current.DataContext != null)
                    return current;
                current = VisualTreeHelper.GetParent(current) as FrameworkElement;
            }
            while (current != null);
            return null;
        }

        private Point _startPoint;
        private FrameworkElement _dragListItem = null;

        private bool IsAcceptableType(DependencyObject current)
        {
            if (current == null)
                return false;
            System.Windows.Controls.Slider slider = FindAnchestor<System.Windows.Controls.Slider>(current);
            if (slider != null)
                return false;
            System.Windows.Controls.Button button = FindAnchestor<System.Windows.Controls.Button>(current);
            if (button != null)
                return false;
            return true;
        }
        protected override void OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            if (string.IsNullOrEmpty(DragDropFormatString))
                return;
            _startPoint = e.GetPosition(null);
            FrameworkElement d = e.OriginalSource as FrameworkElement;
            if (d == null)
            {
                _dragListItem = null;
                return;
            }
            if (!IsAcceptableType(d))
            {
                _dragListItem = null;
                return;
            }
            _dragListItem = GetAncestorWithData(d);
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
                DragDrop.DoDragDrop(_dragListItem, dragDataObj, DragDropEffects.All);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace ResponderApps.EditorDisplay
{
    public class CheckBoxEditorItem:BaseEditorItem
    {
        CheckBox _cbEditor;

        public CheckBoxEditorItem(string itemKey, string itemTitle, bool isChecked)
            : this(itemKey, itemTitle)
        {
            this.IsChecked = isChecked;
        }
        public CheckBoxEditorItem(string itemKey, string itemTitle)
            : base(itemKey, itemTitle)
        {
            _cbEditor = new CheckBox();
            _cbEditor.Content = itemTitle;
            _cbEditor.IsChecked = false;
            _cbEditor.Margin = new System.Windows.Thickness(4, 2, 4, 2);
            _cbEditor.IsEnabled = this.IsEnabled;
            _cbEditor.Checked += (s, e) => { RaiseValueChanged(); };
            _cbEditor.Unchecked += (s, e) => { RaiseValueChanged(); };
        }
        
        protected override void OnIsEnabledChanged()
        {
            base.OnIsEnabledChanged();
            _cbEditor.IsEnabled = this.IsEnabled;
        }

        public override System.Windows.FrameworkElement GenerateItem()
        {
            Grid.SetRow(_cbEditor, 0);
            Grid.SetColumn(_cbEditor, 1);
            _cbEditor.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            _cbEditor.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            Grid grd = GenerateBaseGrid();
            grd.Children.Add(_cbEditor);
            return grd;
        }

        protected override void OnItemTitleChanged()
        {
            base.OnItemTitleChanged();

            _cbEditor.Content = this.ItemTitle;
        }

        public bool IsChecked
        {
            get { return _cbEditor.IsChecked == true; }
            set { _cbEditor.IsChecked = value; }
        }
        public System.Windows.Thickness CheckboxMargin
        {
            get { return _cbEditor.Margin; }
            set { _cbEditor.Margin = value; }
        }
    }
}

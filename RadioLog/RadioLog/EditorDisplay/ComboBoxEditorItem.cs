using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace ResponderApps.EditorDisplay
{
    public class ComboBoxEditorItem:BaseTitledEditorItem
    {
        private ComboBox _cbEditor;

        public ComboBoxEditorItem(string itemKey, string itemTitle, string displayMemberPath, string selectedValuePath, IEnumerable itemsSource, object selectedValue)
            : this(itemKey, itemTitle)
        {
            this.ItemsSource = itemsSource;
            this.DisplayMemberPath = displayMemberPath;
            this.SelectedValuePath = selectedValuePath;
            this.SelectedValue = selectedValue;
        }
        public ComboBoxEditorItem(string itemKey, string itemTitle)
            : base(itemKey, itemTitle)
        {
            _cbEditor = new ComboBox();
            Grid.SetRow(_cbEditor, 0);
            Grid.SetColumn(_cbEditor, 1);
            _cbEditor.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            _cbEditor.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            _cbEditor.IsEnabled = this.IsEnabled;
            _cbEditor.Margin = new System.Windows.Thickness(4, 2, 4, 2);
            _cbEditor.SelectionChanged += (s, e) => { RaiseValueChanged(); };
        }

        protected override void OnIsEnabledChanged()
        {
            base.OnIsEnabledChanged();
            _cbEditor.IsEnabled = this.IsEnabled;
        }
        protected override System.Windows.FrameworkElement GenerateEditorItem()
        {
            return _cbEditor;
        }

        public System.Windows.Thickness ComboBoxMargin
        {
            get { return _cbEditor.Margin; }
            set { _cbEditor.Margin = value; }
        }
        public IEnumerable ItemsSource
        {
            get { return _cbEditor.ItemsSource; }
            set { _cbEditor.ItemsSource = value; }
        }
        public string DisplayMemberPath
        {
            get { return _cbEditor.DisplayMemberPath; }
            set { _cbEditor.DisplayMemberPath = value; }
        }
        public object SelectedValue
        {
            get { return _cbEditor.SelectedValue; }
            set { _cbEditor.SelectedValue = value; }
        }
        public object SelectedItem
        {
            get { return _cbEditor.SelectedItem; }
            set { _cbEditor.SelectedItem = value; }
        }
        public int SelectedIndex
        {
            get { return _cbEditor.SelectedIndex; }
            set { _cbEditor.SelectedIndex = value; }
        }
        public string SelectedValuePath
        {
            get { return _cbEditor.SelectedValuePath; }
            set { _cbEditor.SelectedValuePath = value; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace ResponderApps.EditorDisplay
{
    public class TextboxEditorItem:BaseTitledEditorItem
    {
        private TextBox _editorTextBox;

        public TextboxEditorItem(string itemKey, string itemTitle, string initialText)
            : base(itemKey, itemTitle)
        {
            _editorTextBox = new TextBox();
            _editorTextBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            _editorTextBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            _editorTextBox.Text = initialText;
            _editorTextBox.Margin = new System.Windows.Thickness(4, 2, 4, 2);
            _editorTextBox.IsEnabled = this.IsEnabled;
            _editorTextBox.TextChanged += (s, e) => { RaiseValueChanged(); };
        }

        protected override void OnIsEnabledChanged()
        {
            base.OnIsEnabledChanged();
            _editorTextBox.IsEnabled = this.IsEnabled;
        }
        protected override System.Windows.FrameworkElement GenerateEditorItem()
        {
            return _editorTextBox;
        }

        public string Text
        {
            get { return _editorTextBox.Text; }
            set { _editorTextBox.Text = value; }
        }
        public System.Windows.Thickness EditorMargin
        {
            get { return _editorTextBox.Margin; }
            set { _editorTextBox.Margin = value; }
        }
    }
}

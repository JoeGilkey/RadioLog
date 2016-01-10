using System;
using System.Collections;
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
    /// Interaction logic for BaseEditDisplayWindow.xaml
    /// </summary>
    public partial class BaseEditDisplayWindow : BaseRadioLogWindow
    {
        private object _editObjectHolder = null;

        public BaseEditDisplayWindow(object editObject = null)
        {
            InitializeComponent();

            _editObjectHolder = editObject;

            this.SetupInputs();

            this.editControl.GenerateDisplay();

            btnDelete.Visibility = this.ShowDeleteButton ? Visibility.Visible : Visibility.Collapsed;

            tbWarning.Text = this.WarningText;
            tbWarning.Visibility = string.IsNullOrWhiteSpace(this.WarningText) ? Visibility.Collapsed : Visibility.Visible;

            this.editControl.OnEditValueChanged += editControl_OnEditValueChanged;
        }

        void editControl_OnEditValueChanged(object sender, ResponderApps.EditorDisplay.EditValueChangedArgs e)
        {
            HandleEditValueChanged(e.ItemKey);
        }

        public object EditObject { get { return _editObjectHolder; } }
        protected virtual string GetItemName() { return string.Empty; }
        protected virtual void SetupInputs() { }
        protected virtual bool SaveInputs() { return true; }
        protected virtual void DeleteItem() { }
        protected virtual bool IsValid() { return true; }
        protected virtual bool ShowDeleteButton { get { return false; } }
        protected virtual string WarningText { get { return string.Empty; } }

        protected virtual void HandleEditValueChanged(string itemKey) { }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Common.MessageBoxHelper.Show(string.Format("Are you sure you want to delete {0}?", GetItemName()), "Delete", MessageBoxButton.YesNo, MessageBoxImage.Stop, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                DeleteItem();
                DialogResult = false;
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValid())
                return;
            this.DialogResult = SaveInputs();
        }

        protected void AddTextBoxItem(string itemKey, string itemName, string curText)
        {
            ResponderApps.EditorDisplay.TextboxEditorItem item = new ResponderApps.EditorDisplay.TextboxEditorItem(itemKey, itemName, curText);
            this.editControl.AddEditorItem(item);
        }
        protected void AddCheckBoxItem(string itemKey, string itemTitle, bool isChecked)
        {
            ResponderApps.EditorDisplay.CheckBoxEditorItem item = new ResponderApps.EditorDisplay.CheckBoxEditorItem(itemKey, itemTitle, isChecked);
            this.editControl.AddEditorItem(item);
        }
        protected void AddComboBoxItem(string itemKey, string itemTitle, string displayMemberPath, string selectedValuePath, IEnumerable itemsSource, object selectedValue)
        {
            ResponderApps.EditorDisplay.ComboBoxEditorItem item = new ResponderApps.EditorDisplay.ComboBoxEditorItem(itemKey, itemTitle, displayMemberPath, selectedValuePath, itemsSource, selectedValue);
            this.editControl.AddEditorItem(item);
        }
        protected void AddQRCodeItem(string itemKey, string itemURL)
        {
            ResponderApps.EditorDisplay.QRCodeEditorItem item = new ResponderApps.EditorDisplay.QRCodeEditorItem(itemKey, string.Empty, itemURL);
            this.editControl.AddEditorItem(item);
        }
        protected void AddColorSelectionCombo(string itemKey, string itemTitle)
        {
            RadioLog.WPFCommon.BrushSelectionHolder[] colors = RadioLog.WPFCommon.ColorHelper.GetBrushSelectionItems();
            System.Collections.ObjectModel.ObservableCollection<RadioLog.WPFCommon.BrushSelectionHolder> colorList = new System.Collections.ObjectModel.ObservableCollection<WPFCommon.BrushSelectionHolder>(colors);
            colorList.Insert(0, new WPFCommon.BrushSelectionHolder("Default", null));

            ResponderApps.EditorDisplay.ComboBoxEditorItem cbColor = new ResponderApps.EditorDisplay.ComboBoxEditorItem(itemKey, itemTitle);
            cbColor.ItemsSource = colorList;
            cbColor.DisplayMemberPath = "BrushName";
            cbColor.SelectedValuePath = "BrushKey";
            cbColor.SelectedIndex = 0;
            this.editControl.AddEditorItem(cbColor);
        }

        protected void SetItemEnabled(string itemKey, bool enabled)
        {
            ResponderApps.EditorDisplay.BaseEditorItem item = editControl.GetEditorItem(itemKey);
            if (item != null)
                item.IsEnabled = enabled;
        }
        protected void SetItemVisibility(string itemKey, Visibility visibility)
        {
            ResponderApps.EditorDisplay.BaseEditorItem item = editControl.GetEditorItem(itemKey);
            if (item != null)
                item.Visibility = visibility;
        }

        protected string GetTextBoxText(string itemKey)
        {
            ResponderApps.EditorDisplay.TextboxEditorItem t = editControl.GetEditorItem(itemKey) as ResponderApps.EditorDisplay.TextboxEditorItem;
            if (t != null)
                return t.Text;
            else
                return string.Empty;
        }
        protected void SetTextBoxText(string itemKey, string itemText)
        {
            ResponderApps.EditorDisplay.TextboxEditorItem t = editControl.GetEditorItem(itemKey) as ResponderApps.EditorDisplay.TextboxEditorItem;
            if (t != null)
                t.Text = itemText;
        }

        protected bool GetCheckBoxChecked(string itemKey)
        {
            ResponderApps.EditorDisplay.CheckBoxEditorItem cb = editControl.GetEditorItem(itemKey) as ResponderApps.EditorDisplay.CheckBoxEditorItem;
            if (cb != null)
                return cb.IsChecked;
            else
                return false;
        }
        protected void SetCheckBoxChecked(string itemKey, bool isChecked)
        {
            ResponderApps.EditorDisplay.CheckBoxEditorItem cb = editControl.GetEditorItem(itemKey) as ResponderApps.EditorDisplay.CheckBoxEditorItem;
            if (cb != null)
                cb.IsChecked = isChecked;
        }
    }
}

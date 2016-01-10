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
    /// Interaction logic for GroupEditorDialog.xaml
    /// </summary>
    public partial class GroupEditorDialog : BaseRadioLogWindow
    {
        private Common.SignalGroup _group = null;

        public GroupEditorDialog(Common.SignalGroup group)
        {
            InitializeComponent();

            RadioLog.WPFCommon.BrushSelectionHolder[] colors = RadioLog.WPFCommon.ColorHelper.GetBrushSelectionItems();
            System.Collections.ObjectModel.ObservableCollection<RadioLog.WPFCommon.BrushSelectionHolder> colorList = new System.Collections.ObjectModel.ObservableCollection<WPFCommon.BrushSelectionHolder>(colors);
            colorList.Insert(0, new WPFCommon.BrushSelectionHolder("Default", null));
            cbGroupColor.ItemsSource = colorList;
            
            _group = group;
            SetupForCurrentGroup();
        }

        public override Common.PopupScreenType ScreenSizeType { get { return Common.PopupScreenType.ManualSize; } }

        public static void ShowNewGroupDialog()
        {
            GroupEditorDialog dlg = new GroupEditorDialog(null);
            dlg.ShowDialog();
        }
        public static void ShowGroupEditorDialog(Common.SignalGroup group)
        {
            GroupEditorDialog dlg = new GroupEditorDialog(group);
            dlg.ShowDialog();
        }

        private void SetupForCurrentGroup()
        {
            if (_group == null)
            {
                this.Title = "New Group";
                tbGroupName.Text = string.Empty;
                cbGroupColor.SelectedIndex = 0;
                btnDelete.Visibility = System.Windows.Visibility.Collapsed;
                sDisplayOrder.Value = 10;
            }
            else
            {
                this.Title = _group.GroupName;
                tbGroupName.Text = _group.GroupName;
                sDisplayOrder.Value = _group.DisplayOrder;
                if (string.IsNullOrWhiteSpace(_group.GroupColorString))
                    cbGroupColor.SelectedIndex = 0;
                else
                    cbGroupColor.SelectedValue = _group.GroupColorString;
                btnDelete.Visibility = (_group.GroupId == Guid.Empty) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }
            VerifySourceInputs();
        }

        private bool VerifySourceInputs()
        {

            bool bGood = true;
            if (_group == null || _group.GroupId != Guid.Empty)
            {
                bGood = !string.IsNullOrEmpty(tbGroupName.Text);
            }
            btnOk.IsEnabled = bGood;
            return bGood;
        }

        private void tbTextChanged(object sender, TextChangedEventArgs e)
        {
            VerifySourceInputs();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_group == null || _group.GroupId == Guid.Empty)
            {
                DialogResult = false;
            }
            else
            {
                RadioLog.Common.AppSettings.Instance.SignalGroups.Remove(_group);
                Cinch.Mediator.Instance.NotifyColleagues<Guid>("GROUP_DELETED", _group.GroupId);
                DialogResult = false;
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!VerifySourceInputs())
                return;
            string colorName = string.Empty;
            if (cbGroupColor.SelectedIndex > 0)
            {
                colorName = cbGroupColor.SelectedValue as string;
            }
            if (_group == null)
            {
                int dispOrder = (int)sDisplayOrder.Value;
                Common.SignalGroup highestGroup = Common.AppSettings.Instance.SignalGroups.OrderByDescending(g => g.DisplayOrder).FirstOrDefault();
                if (highestGroup != null)
                    dispOrder = highestGroup.DisplayOrder + 10;
                _group = new Common.SignalGroup() { GroupId = Guid.NewGuid(), GroupMuted = false, GroupColorString = colorName, GroupName = tbGroupName.Text, DisplayOrder = dispOrder };
                RadioLog.Common.AppSettings.Instance.SignalGroups.Add(_group);
                Cinch.Mediator.Instance.NotifyColleagues<RadioLog.Common.SignalGroup>("NEW_GROUP_ADDED", _group);
            }
            else
            {
                _group.GroupName = tbGroupName.Text;
                _group.GroupColorString = colorName;
                _group.DisplayOrder = (int)sDisplayOrder.Value;
                Cinch.Mediator.Instance.NotifyColleagues<Guid>("GROUP_CHANGED", _group.GroupId);
            }
            RadioLog.Common.AppSettings.Instance.SaveSettingsFile();
            DialogResult = true;
        }
    }
}

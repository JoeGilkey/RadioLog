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
    /// Interaction logic for SourcesListView.xaml
    /// </summary>
    public partial class SourcesListView : BaseMainWindowView
    {
        public SourcesListView()
        {
            InitializeComponent();
        }

        public override void SetupDisplay()
        {
            base.SetupDisplay();
            bool bIsXP = !(Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 6);
            if (bIsXP || !Common.AppSettings.Instance.UseGroups)
            {
                svGroups.Visibility = System.Windows.Visibility.Collapsed;
                lbGroups.ItemsSource = null;
                ViewModels.SourceGroupModel grp = (this.DataContext as ViewModels.MainViewModel).SignalGroups.FirstOrDefault();
                if (grp == null)
                {
                    Common.SignalGroup sigGrp = new Common.SignalGroup() { GroupId = Guid.Empty, DisplayOrder = 0, GroupName = "Default", GroupMuted = false, GroupColorString = string.Empty };
                    Common.AppSettings.Instance.SignalGroups.Add(sigGrp);
                    Common.AppSettings.Instance.SaveSettingsFile();
                    grp = new ViewModels.SourceGroupModel(sigGrp);
                }
                xpSourcesView.ItemsSource = grp.SignalSources;
                svXp.Visibility = Visibility.Visible;
            }
            else
            {
                svGroups.Visibility = System.Windows.Visibility.Visible;
                lbGroups.ItemsSource = (this.DataContext as ViewModels.MainViewModel).SignalGroups;
                xpSourcesView.Visibility = System.Windows.Visibility.Collapsed;
                svXp.Visibility = System.Windows.Visibility.Collapsed;
            }
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

        private void lbGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.AddedItems != null)
                {
                    foreach (object o in e.AddedItems)
                    {
                        ListBoxItem lbi = lbGroups.ItemContainerGenerator.ContainerFromItem(o) as ListBoxItem;
                        lbi.IsSelected = false;
                    }
                }
            }
            catch { }
        }

        private void DragDropProviderItemsControl_DragOver(object sender, DragEventArgs e)
        {
            bool bOk = true;
            if (GetGroupModelFromSender(sender) == null)
            {
                bOk = false;
            }

            if (!e.Data.GetDataPresent(RadioLog.WPFCommon.DragDropProviderItemsControl.DEFAULT_FORMATSTRING))
            {
                bOk = false;
            }
            if (!bOk)
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DragDropProviderItemsControl_Drop(object sender, DragEventArgs e)
        {
            ViewModels.SourceGroupModel grp = GetGroupModelFromSender(sender);
            if (grp == null)
                return;

            RadioLog.ViewModels.BaseSourceModel src = GetSourceModelFromDrop(e);
            if (src == null)
                return;

            e.Handled = true;

            if (src.SrcInfo.GroupId == GetGroupModelFromSender(sender).GroupId)
            {
                //could be re-ordering...
                RadioLog.ViewModels.BaseSourceModel tgt = GetTargetModelFromDrop(sender, e);
                if (tgt != null)
                {
                    ViewModels.BaseSourceModel[] curModels = grp.SignalSources.ToArray();
                    List<ViewModels.BaseSourceModel> tmpList = new List<ViewModels.BaseSourceModel>(curModels);
                    if (tmpList.Contains(src) && tmpList.Contains(tgt))
                    {
                        tmpList.Remove(src);
                        int indx = tmpList.IndexOf(tgt);
                        indx = Math.Max(indx, 0);
                        if (indx < (tmpList.Count - 2))
                            tmpList.Insert(indx, src);
                        else
                            tmpList.Add(src);
                        for (int i = 0; i < tmpList.Count; i++)
                        {
                            tmpList[i].DisplayOrder = ((i + 1) * 10);
                        }
                        Common.AppSettings.Instance.SaveSettingsFile();
                        grp.SignalSources.Sort();
                    }
                }

                return;
            }

            src.SrcInfo.GroupId = grp.GroupId;
            src.GroupInfo = grp.GroupInfo;
            Common.AppSettings.Instance.SaveSettingsFile();
            Common.SignalSourceGroupChangeHolder changeHolder = new Common.SignalSourceGroupChangeHolder() { SignalSourceId = src.SrcInfo.SourceId, NewGroupId = grp.GroupId };
            Cinch.Mediator.Instance.NotifyColleagues<Common.SignalSourceGroupChangeHolder>("SOURCE_GROUP_CHANGE", changeHolder);
        }

        private void btnGroupSettings_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.SourceGroupModel grp = GetDataContextFromButtonClick(sender, e) as ViewModels.SourceGroupModel;
            if (grp == null)
                return;
            Windows.GroupEditorDialog.ShowGroupEditorDialog(grp.GroupInfo);
        }

        private void btnGroupAdd_Click(object sender, RoutedEventArgs e)
        {
            Windows.GroupEditorDialog.ShowNewGroupDialog();
        }
    }
}

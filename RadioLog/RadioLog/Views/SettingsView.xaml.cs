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
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : MahApps.Metro.Controls.Flyout
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        void CloseIfUnchanged()
        {
            if (!(this.DataContext as ViewModels.SettingsViewModel).ChangesMade)
            {
                IsOpen = false;
            }
        }
        void DisplayClosingDialog(Window dlgWnd)
        {
            if (dlgWnd == null)
                return;
            CloseIfUnchanged();
            dlgWnd.ShowDialog();
        }

        private void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as ViewModels.SettingsViewModel).SaveChanges();
            IsOpen = false;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as ViewModels.SettingsViewModel).CancelChanges();
            IsOpen = false;
        }

        private void btnManageAgencies_Click(object sender, RoutedEventArgs e)
        {
            DisplayClosingDialog(new Windows.AgencyEditorDialog());
        }

        private void btnManageUnits_Click(object sender, RoutedEventArgs e)
        {
            DisplayClosingDialog(new Windows.UnitEditor());
        }

        private void btnManageRadios_Click(object sender, RoutedEventArgs e)
        {
            DisplayClosingDialog(new Windows.RadioEditorDialog());
        }

        private void btnAddNewSource_Click(object sender, RoutedEventArgs e)
        {
            DisplayClosingDialog(new Windows.NewSourceDialog());
        }

        private void btnSelectLogFileDir_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.SelectedPath = (this.DataContext as ViewModels.SettingsViewModel).LogFileDirectory;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                (this.DataContext as ViewModels.SettingsViewModel).LogFileDirectory = dlg.SelectedPath;
            }
        }

        private void btnSelectRecordFileDir_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.SelectedPath = (this.DataContext as ViewModels.SettingsViewModel).RecordFileDirectory;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                (this.DataContext as ViewModels.SettingsViewModel).RecordFileDirectory = dlg.SelectedPath;
            }
        }
        
        private void btnOpenLogFileDir_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as ViewModels.SettingsViewModel).OpenFolder((DataContext as ViewModels.SettingsViewModel).LogFileDirectory);
            this.IsOpen = false;
        }

        private void btnOpenRecordingFileDir_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as ViewModels.SettingsViewModel).OpenFolder((DataContext as ViewModels.SettingsViewModel).RecordFileDirectory);
            this.IsOpen = false;
        }

        private void btnImportSettings_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Title = "Import Settings";
            ofd.Filter = "RadioLog Export (*.rlog)|*.rlog|XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
            ofd.DefaultExt = "rlog";
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK && System.IO.File.Exists(ofd.FileName))
            {
                Common.ImportExportHelper helper = new Common.ImportExportHelper();
                bool bImported = helper.ImportInfo(ofd.FileName);
                this.IsOpen = false;

                if(bImported)
                {
                    App.PerformAppRestart();
                }
            }
        }

        private void btnExportSettings_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Title = "Export Settings";
            sfd.Filter = "RadioLog Export (*.rlog)|*.rlog|XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
            sfd.DefaultExt = "rlog";
            sfd.CheckPathExists = true;
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Common.ImportExportHelper helper = new Common.ImportExportHelper();
                helper.ExportInfo(sfd.FileName);
                this.IsOpen = false;
            }
        }

        private void btnSelectEmergencySoundFile_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Filter = "Wave Files (*.wav)|*.wav";
            dlg.Multiselect = false;
            dlg.ValidateNames = true;
            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            dlg.AutoUpgradeEnabled = true;

            if(string.IsNullOrWhiteSpace((this.DataContext as ViewModels.SettingsViewModel).EmergencyAlarmSoundFile))
            {
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic);
            }
            else
            {
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName((this.DataContext as ViewModels.SettingsViewModel).EmergencyAlarmSoundFile);
            }
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                (this.DataContext as ViewModels.SettingsViewModel).EmergencyAlarmSoundFile = dlg.FileName;
            }
        }
    }
}

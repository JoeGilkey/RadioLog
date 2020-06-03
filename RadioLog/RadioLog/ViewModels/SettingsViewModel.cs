using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;

using Cinch;
using MEFedMVVM.Common;
using MEFedMVVM.ViewModelLocator;

namespace RadioLog.ViewModels
{
    [ExportViewModel("SettingsViewModel")]
    [PartCreationPolicy(System.ComponentModel.Composition.CreationPolicy.NonShared)]
    public class SettingsViewModel : RadioLog.WPFCommon.ThreadSafeViewModelBase
    {
        private IViewAwareStatus _viewAwareStatus = null;
        private bool _changesMade = false;
        private bool _restartChangesMade = false;
        private bool _emergencyAlarmSoundChangesMade = false;

        #region Theme
        private string _theme = RadioLog.Common.AppSettings.Instance.CurrentTheme;
        private ObservableCollection<ThemeListItem> _themeList = null;
        private ObservableCollection<ModeListItem> _modeList = null;
        private ObservableCollection<DisplayStyleListItem> _displayList = null;
        private ObservableCollection<ViewSizeListItem> _viewSizeList = null;
        private Common.ProcessorViewSize _viewSize = Common.AppSettings.Instance.ViewSize;
        #endregion

        #region Globals
        private Common.RadioLogMode _mode = RadioLog.Common.AppSettings.Instance.WorkstationMode;
        private Common.DisplayStyle _mainDisplayStyle = RadioLog.Common.AppSettings.Instance.MainDisplayStyle;
        private bool _useGroups = RadioLog.Common.AppSettings.Instance.UseGroups;
        private bool _shouldAutoSaveContacts = RadioLog.Common.AppSettings.Instance.ShouldAutoSaveContacts;
        private bool _enableClipboardStreamURLIntegration = RadioLog.Common.AppSettings.Instance.EnableClipboardStreamURLIntegration;
        private string _logFileDirectory = RadioLog.Common.AppSettings.Instance.LogFileDirectory;
        private string _recordFileDirectory = RadioLog.Common.AppSettings.Instance.RecordFileDirectory;
        private int _gridFontSize = RadioLog.Common.AppSettings.Instance.GridFontSize;
        private ObservableCollection<IntListItem> _fileRecordingRateList = null;
        private ObservableCollection<IntListItem> _fileBitsPerSampleList = null;
        private int _fileRecordingRate = RadioLog.AudioProcessing.AudioProcessingGlobals.RecordingFileSampleRate;
        private int _fileRecordingBitsPerSample = RadioLog.AudioProcessing.AudioProcessingGlobals.RecordingFileBitsPerSample;
        private bool _diagnosticsMode = RadioLog.Common.AppSettings.Instance.DiagnosticMode;
        private bool _globalLoggingEnabled = RadioLog.Common.AppSettings.Instance.GlobalLoggingEnabled;
        private bool _playSoundOnEmergencyAlarm = RadioLog.Common.AppSettings.Instance.PlaySoundOnEmergencyAlarm;
        private string _emergencyAlarmSoundFile = RadioLog.Common.AppSettings.Instance.EmergencyAlarmSoundFile;
        private bool _enableAutoMute = RadioLog.Common.AppSettings.Instance.EnableAutoMute;
        private int _autoMuteHangTime = RadioLog.Common.AppSettings.Instance.AutoMuteHangTime;
        #endregion

        #region Grid Columns
        private bool _showSourceName = RadioLog.Common.AppSettings.Instance.ShowSourceName;
        private bool _showUnitId = RadioLog.Common.AppSettings.Instance.ShowUnitId;
        private bool _showAgencyName = RadioLog.Common.AppSettings.Instance.ShowAgencyName;
        private bool _showUnitName = RadioLog.Common.AppSettings.Instance.ShowUnitName;
        private bool _showRadioName = RadioLog.Common.AppSettings.Instance.ShowRadioName;
        private bool _showAssignedRole = RadioLog.Common.AppSettings.Instance.ShowAssignedRole;
        private bool _showAssignedPersonnel = RadioLog.Common.AppSettings.Instance.ShowAssignedPersonnel;
        private bool _showSourceType = RadioLog.Common.AppSettings.Instance.ShowSourceType;
        private bool _showDescription = RadioLog.Common.AppSettings.Instance.ShowDescription;
        #endregion

        private void SetDefaultsForMode(Common.RadioLogMode mode)
        {
            if (mode == Common.RadioLogMode.Fireground)
            {
                ShowSourceName = false;
                ShowUnitId = false;
                ShowAgencyName = true;
                ShowUnitName = true;
                ShowRadioName = false;
                ShowAssignedRole = true;
                ShowAssignedPersonnel = true;
                ShowSourceType = false;
                ShowDescription = false;
                GlobalLoggingEnabled = true;
                MainDisplayStyle = Common.DisplayStyle.GridsOnlyInterface;
                UseGroups = false;
            }
            else
            {
                ShowSourceName = true;
                ShowUnitId = true;
                ShowAgencyName = false;
                ShowUnitName = false;
                ShowRadioName = true;
                ShowAssignedRole = false;
                ShowAssignedPersonnel = false;
                ShowSourceType = false;
                ShowDescription = true;
                GlobalLoggingEnabled = false;
                MainDisplayStyle = Common.DisplayStyle.TabbedInterface;
                UseGroups = true;
            }
        }

        private void DisplaySelectedTheme()
        {
            ControlzEx.Theming.ThemeManager.Current.ChangeTheme(App.Current, CurrentTheme);
            
            RadioLog.WPFCommon.AccentColorBrushConverter.CleanupBrushes();
            Cinch.Mediator.Instance.NotifyColleaguesAsync<bool>("REFRESH_VISUALS", true);
        }

        [ImportingConstructor]
        public SettingsViewModel(IViewAwareStatus viewAwareStatus)
        {
            _viewAwareStatus = viewAwareStatus;
            _themeList = new System.Collections.ObjectModel.ObservableCollection<ThemeListItem>();
            foreach (var theme in ControlzEx.Theming.ThemeManager.Current.Themes.OrderBy(t => t.DisplayName))
            {
                _themeList.Add(new ThemeListItem() { Name = theme.DisplayName, Value = theme.Name });
            }
            
            _modeList = new ObservableCollection<ModeListItem>();
            _modeList.Add(new ModeListItem() { Mode = Common.RadioLogMode.Normal, ModeName = "Normal" });
            _modeList.Add(new ModeListItem() { Mode = Common.RadioLogMode.Fireground, ModeName = "Fireground" });

            _displayList = new ObservableCollection<DisplayStyleListItem>();
            _displayList.Add(new DisplayStyleListItem() { DisplayName = "Tabbed Interface", DisplayStyle = Common.DisplayStyle.TabbedInterface });
            _displayList.Add(new DisplayStyleListItem() { DisplayName = "Grid Interface", DisplayStyle = Common.DisplayStyle.GridsOnlyInterface });
            _displayList.Add(new DisplayStyleListItem() { DisplayName = "All-in-One", DisplayStyle = Common.DisplayStyle.AllInOne });
            _displayList.Add(new DisplayStyleListItem() { DisplayName = "Grids and Auto-Open Sources", DisplayStyle = Common.DisplayStyle.GridsAutoOpenSources });
            _displayList.Add(new DisplayStyleListItem() { DisplayName = "Column Interface", DisplayStyle = Common.DisplayStyle.Columns });

            _viewSizeList = new ObservableCollection<ViewSizeListItem>();
            _viewSizeList.Add(new ViewSizeListItem() { Value = Common.ProcessorViewSize.Normal, Name = "Normal" });
            _viewSizeList.Add(new ViewSizeListItem() { Value = Common.ProcessorViewSize.Small, Name = "Small" });
            _viewSizeList.Add(new ViewSizeListItem() { Value = Common.ProcessorViewSize.Wide, Name = "Wide" });
            _viewSizeList.Add(new ViewSizeListItem() { Value = Common.ProcessorViewSize.ExtraWide, Name = "Extra Wide" });

            _fileBitsPerSampleList = new ObservableCollection<IntListItem>();
            _fileBitsPerSampleList.Add(new IntListItem() { Value = 8, Name = "8-bits" });
            _fileBitsPerSampleList.Add(new IntListItem() { Value = 16, Name = "16-bits" });
            _fileBitsPerSampleList.Add(new IntListItem() { Value = 24, Name = "24-bits" });
            _fileBitsPerSampleList.Add(new IntListItem() { Value = 32, Name = "32-bits" });

            _fileRecordingRateList = new ObservableCollection<IntListItem>();
            _fileRecordingRateList.Add(new IntListItem() { Value = 8000, Name = "8000Hz" });
            _fileRecordingRateList.Add(new IntListItem() { Value = 16000, Name = "16000Hz" });
            _fileRecordingRateList.Add(new IntListItem() { Value = 22050, Name = "22050Hz" });
            _fileRecordingRateList.Add(new IntListItem() { Value = 32000, Name = "32000Hz" });
            _fileRecordingRateList.Add(new IntListItem() { Value = 44100, Name = "44100Hz" });
            _fileRecordingRateList.Add(new IntListItem() { Value = 48000, Name = "48000Hz" });
            _fileRecordingRateList.Add(new IntListItem() { Value = 88200, Name = "88200Hz" });
            _fileRecordingRateList.Add(new IntListItem() { Value = 96000, Name = "96000Hz" });
        }

        public void CancelChanges()
        {
            _theme = RadioLog.Common.AppSettings.Instance.CurrentTheme;
            _showSourceName = RadioLog.Common.AppSettings.Instance.ShowSourceName;
            _showUnitId = RadioLog.Common.AppSettings.Instance.ShowUnitId;
            _showAgencyName = RadioLog.Common.AppSettings.Instance.ShowAgencyName;
            _showUnitName = RadioLog.Common.AppSettings.Instance.ShowUnitName;
            _showRadioName = RadioLog.Common.AppSettings.Instance.ShowRadioName;
            _showAssignedRole = RadioLog.Common.AppSettings.Instance.ShowAssignedRole;
            _showAssignedPersonnel = RadioLog.Common.AppSettings.Instance.ShowAssignedPersonnel;
            _showSourceType = RadioLog.Common.AppSettings.Instance.ShowSourceType;
            _showDescription = RadioLog.Common.AppSettings.Instance.ShowDescription;
            _shouldAutoSaveContacts = RadioLog.Common.AppSettings.Instance.ShouldAutoSaveContacts;
            _mode = RadioLog.Common.AppSettings.Instance.WorkstationMode;
            _mainDisplayStyle = RadioLog.Common.AppSettings.Instance.MainDisplayStyle;
            _useGroups = RadioLog.Common.AppSettings.Instance.UseGroups;
            _logFileDirectory = RadioLog.Common.AppSettings.Instance.LogFileDirectory;
            _recordFileDirectory = RadioLog.Common.AppSettings.Instance.RecordFileDirectory;
            _enableClipboardStreamURLIntegration = RadioLog.Common.AppSettings.Instance.EnableClipboardStreamURLIntegration;
            _gridFontSize = RadioLog.Common.AppSettings.Instance.GridFontSize;
            _fileRecordingRate = RadioLog.AudioProcessing.AudioProcessingGlobals.RecordingFileSampleRate;
            _fileRecordingBitsPerSample = RadioLog.AudioProcessing.AudioProcessingGlobals.RecordingFileBitsPerSample;
            _restartChangesMade = false;
            _diagnosticsMode = RadioLog.Common.AppSettings.Instance.DiagnosticMode;
            _globalLoggingEnabled = RadioLog.Common.AppSettings.Instance.GlobalLoggingEnabled;
            _viewSize = RadioLog.Common.AppSettings.Instance.ViewSize;

            _emergencyAlarmSoundChangesMade = false;
            _playSoundOnEmergencyAlarm = RadioLog.Common.AppSettings.Instance.PlaySoundOnEmergencyAlarm;
            _emergencyAlarmSoundFile = RadioLog.Common.AppSettings.Instance.EmergencyAlarmSoundFile;

            _enableAutoMute = RadioLog.Common.AppSettings.Instance.EnableAutoMute;
            _autoMuteHangTime = RadioLog.Common.AppSettings.Instance.AutoMuteHangTime;

            ChangesMade = false;

            DisplaySelectedTheme();
        }

        public void SaveChanges()
        {
            RadioLog.Common.AppSettings.Instance.CurrentTheme = CurrentTheme;
            RadioLog.Common.AppSettings.Instance.ShowSourceName = ShowSourceName;
            RadioLog.Common.AppSettings.Instance.ShowUnitId = ShowUnitId;
            RadioLog.Common.AppSettings.Instance.ShowAgencyName = ShowAgencyName;
            RadioLog.Common.AppSettings.Instance.ShowUnitName = ShowUnitName;
            RadioLog.Common.AppSettings.Instance.ShowRadioName = ShowRadioName;
            RadioLog.Common.AppSettings.Instance.ShowAssignedRole = ShowAssignedRole;
            RadioLog.Common.AppSettings.Instance.ShowAssignedPersonnel = ShowAssignedPersonnel;
            RadioLog.Common.AppSettings.Instance.ShowSourceType = ShowSourceType;
            RadioLog.Common.AppSettings.Instance.ShowDescription = ShowDescription;
            RadioLog.Common.AppSettings.Instance.ShouldAutoSaveContacts = ShouldAutoSaveContacts;
            RadioLog.Common.AppSettings.Instance.WorkstationMode = WorkstationMode;
            RadioLog.Common.AppSettings.Instance.MainDisplayStyle = MainDisplayStyle;
            RadioLog.Common.AppSettings.Instance.UseGroups = UseGroups;
            RadioLog.Common.AppSettings.Instance.LogFileDirectory = LogFileDirectory;
            RadioLog.Common.AppSettings.Instance.RecordFileDirectory = RecordFileDirectory;
            RadioLog.Common.AppSettings.Instance.EnableClipboardStreamURLIntegration = EnableClipboardStreamURLIntegration;
            RadioLog.Common.AppSettings.Instance.GridFontSize = GridFontSize;
            RadioLog.AudioProcessing.AudioProcessingGlobals.RecordingFileSampleRate = FileRecordingRate;
            RadioLog.AudioProcessing.AudioProcessingGlobals.RecordingFileBitsPerSample = FileRecordingBitsPerSample;
            RadioLog.Common.AppSettings.Instance.DiagnosticMode = DiagnisticsMode;
            RadioLog.Common.AppSettings.Instance.GlobalLoggingEnabled = GlobalLoggingEnabled;
            RadioLog.Common.AppSettings.Instance.ViewSize = ViewSize;
            RadioLog.Common.AppSettings.Instance.PlaySoundOnEmergencyAlarm = PlaySoundOnEmergencyAlarm;
            RadioLog.Common.AppSettings.Instance.EmergencyAlarmSoundFile = EmergencyAlarmSoundFile;
            RadioLog.Common.AppSettings.Instance.EnableAutoMute = EnableAutoMute;
            RadioLog.Common.AppSettings.Instance.AutoMuteHangTime = AutoMuteHangTime;
            RadioLog.Common.AppSettings.Instance.SaveSettingsFile();

            if (_emergencyAlarmSoundChangesMade)
            {
                MainViewModel.UpdateEmergencyAlarmSoundPlayer();
            }

            ChangesMade = false;

            Common.DirectoryHelper.SetupDirectory(LogFileDirectory);
            Common.DirectoryHelper.SetupDirectory(RecordFileDirectory);

            if (RestartChangesMade)
            {
                App.PerformAppRestart();
            }
            else
            {
                RadioLog.Common.DebugHelper.SetShouldOutputNonErrorsToEventLog(DiagnisticsMode);

                RadioLog.Common.RadioInfoLookupHelper.Instance.ReloadInfo();

                Cinch.Mediator.Instance.NotifyColleaguesAsync<bool>("SETTINGS_CHANGED", true);
            }
        }

        public bool ChangesMade
        {
            get { return _changesMade; }
            set
            {
                _changesMade = value;
                OnPropertyChanged("ChangesMade");
            }
        }
        public bool RestartChangesMade
        {
            get { return _restartChangesMade; }
            set
            {
                _restartChangesMade |= value;
                OnPropertyChanged("RestartChangesMade");
            }
        }

        public string CurrentTheme
        {
            get { return _theme; }
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    OnPropertyChanged("CurrentTheme");
                    ChangesMade = true;
                    DisplaySelectedTheme();
                }
            }
        }

        public bool ShowSourceName { get { return _showSourceName; } set { if (_showSourceName != value) { _showSourceName = value; OnPropertyChanged("ShowSourceName"); ChangesMade = true; } } }
        public bool ShowUnitId { get { return _showUnitId; } set { if (_showUnitId != value) { _showUnitId = value; OnPropertyChanged("ShowUnitId"); ChangesMade = true; } } }
        public bool ShowAgencyName { get { return _showAgencyName; } set { if (_showAgencyName != value) { _showAgencyName = value; OnPropertyChanged("ShowAgencyName"); ChangesMade = true; } } }
        public bool ShowUnitName { get { return _showUnitName; } set { if (_showUnitName != value) { _showUnitName = value; OnPropertyChanged("ShowUnitName"); ChangesMade = true; } } }
        public bool ShowRadioName { get { return _showRadioName; } set { if (_showRadioName != value) { _showRadioName = value; OnPropertyChanged("ShowRadioName"); ChangesMade = true; } } }
        public bool ShowAssignedRole { get { return _showAssignedRole; } set { if (_showAssignedRole != value) { _showAssignedRole = value; OnPropertyChanged("ShowAssignedRole"); ChangesMade = true; } } }
        public bool ShowAssignedPersonnel { get { return _showAssignedPersonnel; } set { if (_showAssignedPersonnel != value) { _showAssignedPersonnel = value; OnPropertyChanged("ShowAssignedPersonnel"); ChangesMade = true; } } }
        public bool ShowSourceType { get { return _showSourceType; } set { if (_showSourceType != value) { _showSourceType = value; OnPropertyChanged("ShowSourceType"); ChangesMade = true; } } }
        public bool ShowDescription { get { return _showDescription; } set { if (_showDescription != value) { _showDescription = value; OnPropertyChanged("ShowDescription"); ChangesMade = true; } } }
        public bool ShouldAutoSaveContacts { get { return _shouldAutoSaveContacts; } set { if (_shouldAutoSaveContacts != value) { _shouldAutoSaveContacts = value; OnPropertyChanged("ShouldAutoSaveContacts"); ChangesMade = true; } } }
        public Common.RadioLogMode WorkstationMode { get { return _mode; } set { if (_mode != value) { _mode = value; OnPropertyChanged("WorkstationMode"); SetDefaultsForMode(value); ChangesMade = true; RestartChangesMade = true; } } }
        public Common.DisplayStyle MainDisplayStyle { get { return _mainDisplayStyle; } set { if (_mainDisplayStyle != value) { _mainDisplayStyle = value; OnPropertyChanged("MainDisplayStyle"); ChangesMade = true; RestartChangesMade = true; } } }
        public bool UseGroups { get { return _useGroups; } set { if (_useGroups != value) { _useGroups = value; OnPropertyChanged("UseGroups"); ChangesMade = true; RestartChangesMade = true; } } }
        public string LogFileDirectory { get { return _logFileDirectory; } set { if (_logFileDirectory != value) { _logFileDirectory = value; OnPropertyChanged("LogFileDirectory"); ChangesMade = true; RestartChangesMade = true; } } }
        public string RecordFileDirectory { get { return _recordFileDirectory; } set { if (_recordFileDirectory != value) { _recordFileDirectory = value; OnPropertyChanged("RecordFileDirectory"); ChangesMade = true; RestartChangesMade = true; } } }
        public bool EnableClipboardStreamURLIntegration { get { return _enableClipboardStreamURLIntegration; } set { if (_enableClipboardStreamURLIntegration != value) { _enableClipboardStreamURLIntegration = value; OnPropertyChanged("EnableClipboardStreamURLIntegration"); ChangesMade = true; RestartChangesMade = true; } } }
        public int GridFontSize { get { return _gridFontSize; } set { if (_gridFontSize != value) { _gridFontSize = value; OnPropertyChanged("GridFontSize"); ChangesMade = true; } } }
        public int FileRecordingRate { get { return _fileRecordingRate; } set { if (_fileRecordingRate != value) { _fileRecordingRate = value; OnPropertyChanged("FileRecordingRate"); ChangesMade = true; RestartChangesMade = true; } } }
        public int FileRecordingBitsPerSample { get { return _fileRecordingBitsPerSample; } set { if (_fileRecordingBitsPerSample != value) { _fileRecordingBitsPerSample = value; OnPropertyChanged("FileRecordingBitsPerSample"); ChangesMade = true; RestartChangesMade = true; } } }
        public bool DiagnisticsMode { get { return _diagnosticsMode; } set { if (_diagnosticsMode != value) { _diagnosticsMode = value; OnPropertyChanged(); ChangesMade = true; } } }
        public bool GlobalLoggingEnabled { get { return _globalLoggingEnabled; } set { if (_globalLoggingEnabled != value) { _globalLoggingEnabled = value; OnPropertyChanged(); ChangesMade = true; } } }
        public bool PlaySoundOnEmergencyAlarm { get { return _playSoundOnEmergencyAlarm; } set { if (_playSoundOnEmergencyAlarm != value) { _playSoundOnEmergencyAlarm = value; OnPropertyChanged(); ChangesMade = true; _emergencyAlarmSoundChangesMade = true; } } }
        public string EmergencyAlarmSoundFile { get { return _emergencyAlarmSoundFile; } set { if (_emergencyAlarmSoundFile != value) { _emergencyAlarmSoundFile = value; OnPropertyChanged(); ChangesMade = true; _emergencyAlarmSoundChangesMade = true; } } }
        public bool EnableAutoMute { get { return _enableAutoMute; } set { if (_enableAutoMute != value) { _enableAutoMute = value; OnPropertyChanged(); ChangesMade = true; } } }
        public int AutoMuteHangTime { get { return _autoMuteHangTime; } set { if (_autoMuteHangTime != value) { _autoMuteHangTime = value; OnPropertyChanged(); ChangesMade = true; } } }
        public Common.ProcessorViewSize ViewSize
        {
            get { return _viewSize; }
            set
            {
                if (_viewSize != value)
                {
                    _viewSize = value;
                    OnPropertyChanged();
                    ChangesMade = true;
                }
            }
        }

        public ObservableCollection<ThemeListItem> ThemeList { get { return _themeList; } }
        public ObservableCollection<ModeListItem> ModeList { get { return _modeList; } }
        public ObservableCollection<DisplayStyleListItem> DisplayList { get { return _displayList; } }
        public ObservableCollection<ViewSizeListItem> ViewSizeList { get { return _viewSizeList; } }
        public ObservableCollection<IntListItem> FileRecordingRateList { get { return _fileRecordingRateList; } }
        public ObservableCollection<IntListItem> FileRecordingBitsPerSampleList { get { return _fileBitsPerSampleList; } }

        public void OpenFolder(string folderPath)
        {
            if (Common.DirectoryHelper.SetupDirectory(folderPath))
            {
                System.Diagnostics.ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = folderPath, UseShellExecute = true, Verb = "open" };
                System.Diagnostics.Process.Start(startInfo);
            }
        }
    }

    public class ThemeListItem
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class ModeListItem
    {
        public Common.RadioLogMode Mode { get; set; }
        public string ModeName { get; set; }
    }

    public class DisplayStyleListItem
    {
        public Common.DisplayStyle DisplayStyle { get; set; }
        public string DisplayName { get; set; }
    }

    public class ViewSizeListItem
    {
        public Common.ProcessorViewSize Value { get; set; }
        public string Name { get; set; }
    }

    public class IntListItem
    {
        public int Value { get; set; }
        public string Name { get; set; }
    }
}

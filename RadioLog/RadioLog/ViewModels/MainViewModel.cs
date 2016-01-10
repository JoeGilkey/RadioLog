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
    [ExportViewModel("MainViewModel")]
    [PartCreationPolicy(System.ComponentModel.Composition.CreationPolicy.NonShared)]
    public class MainViewModel:RadioLog.WPFCommon.ThreadSafeViewModelBase
    {
        #region static
        private static Services.RollCallService _rollCallService = new Services.RollCallService();
        private static RadioLog.WPFCommon.ThreadSafeObservableCollection<RadioSignalItemModel> _radioLog = new WPFCommon.ThreadSafeObservableCollection<RadioSignalItemModel>();
        private static RadioLog.WPFCommon.ThreadSafeObservableCollection<EmergencyRadioSignalItemModel> _emergencyRadioLog = new WPFCommon.ThreadSafeObservableCollection<EmergencyRadioSignalItemModel>();
        private static int _maxLogDisplayItems = 200;
        private static WPFCommon.BackgroundSoundPlayer _emergencyAlarmSound = null;

        internal static void ProcessRadioSignal(RadioSignalItemModel mdl)
        {
            if (mdl == null)
                return;

            _rollCallService.HandleRadioCall(mdl);

            if(_radioLog.Count>0)
            {
                _radioLog.Insert(0, mdl);
                while (_radioLog.Count > _maxLogDisplayItems)
                {
                    _radioLog.RemoveAt(_radioLog.Count - 1);
                }
            }
            else
            {
                _radioLog.Add(mdl);
            }
            if (mdl.EmergencyState == Common.EmergencyState.EmergencyActive)
            {
                StartMaydayForRadio(mdl.SignalingLookupKey);
                if (_emergencyAlarmSound != null)
                {
                    _emergencyAlarmSound.PlaySound();
                }
            }
            else
            {
                EmergencyRadioSignalItemModel eMdl = _emergencyRadioLog.FirstOrDefault(m => m.SignalingLookupKey == mdl.SignalingLookupKey);
                if (eMdl != null)
                {
                    mdl.EmergencyState = eMdl.EmergencyState;
                }
            }
        }
        internal static void UpdateEmergencyAlarmSoundPlayer()
        {
            _emergencyAlarmSound = null;
            if(Common.AppSettings.Instance.PlaySoundOnEmergencyAlarm&&!string.IsNullOrWhiteSpace(Common.AppSettings.Instance.EmergencyAlarmSoundFile)&&System.IO.File.Exists(Common.AppSettings.Instance.EmergencyAlarmSoundFile))
            {
                _emergencyAlarmSound = new WPFCommon.BackgroundSoundPlayer(Common.AppSettings.Instance.EmergencyAlarmSoundFile);
            }
        }
        internal static void RemoveAllEmergencies()
        {
            MarkAllEmergenciesCleared();
            _emergencyRadioLog.Clear();
        }
        internal static void MarkAllEmergenciesCleared()
        {
            foreach (EmergencyRadioSignalItemModel emerModel in _emergencyRadioLog.Where(e => e.EmergencyState == Common.EmergencyState.EmergencyActive))
            {
                ClearMaydayForRadio(emerModel);
            }
        }
        internal static void StartMaydayForRadio(string signalingLookupKey)
        {
            if (string.IsNullOrWhiteSpace(signalingLookupKey))
                return;
            RadioSignalItemModel rItemModel = _radioLog.FirstOrDefault(r => r.SignalingLookupKey == signalingLookupKey);
            if (rItemModel == null)
                return;
            EmergencyRadioSignalItemModel mdl = _emergencyRadioLog.FirstOrDefault(r => r.SignalingLookupKey == signalingLookupKey);
            if (mdl != null)
            {
                mdl.EmergencyState = Common.EmergencyState.EmergencyActive;
                mdl.StartedDT = DateTime.Now;
                mdl.EndedDT = null;
            }
            else
            {
                mdl = new EmergencyRadioSignalItemModel(rItemModel.RawSignalItem);
                mdl.EmergencyState = Common.EmergencyState.EmergencyActive;
                mdl.StartedDT = DateTime.Now;
                mdl.EndedDT = null;
                if (_emergencyRadioLog.Count > 0)
                    _emergencyRadioLog.Insert(0, mdl);
                else
                    _emergencyRadioLog.Add(mdl);
            }
            foreach (RadioSignalItemModel rItem in _radioLog.Where(r => r.SignalingLookupKey == signalingLookupKey))
            {
                rItem.EmergencyState = Common.EmergencyState.EmergencyActive;
            }
        }
        internal static void ClearMaydayForRadio(EmergencyRadioSignalItemModel emerModel)
        {
            if (emerModel == null)
                return;
            emerModel.EndedDT = DateTime.Now;
            emerModel.EmergencyState = Common.EmergencyState.EmergencyCleared;
            foreach (RadioSignalItemModel rItem in _radioLog.Where(r => r.SignalingLookupKey == emerModel.SignalingLookupKey))
            {
                rItem.EmergencyState = Common.EmergencyState.NonEmergency;
            }
        }
        #endregion

        private bool _shouldRunThreads = true;
        private System.Threading.Thread _tAudioCheck;
        private bool _isInPowerSave = false;
        private bool _powerSaveSetup = false;
        private int _gridFontSize = Common.AppSettings.Instance.GridFontSize;
        private int _gridSmallFontSize = Common.AppSettings.Instance.GridFontSize - 4;
        private int _gridLargeFontSize = Common.AppSettings.Instance.GridFontSize + 4;
        private Guid? _unmutedSignalSourceId = null;
        private IViewAwareStatus _viewStatus = null;
        private WPFCommon.ThreadSafeObservableCollection<SourceGroupModel> _signalGroups = new WPFCommon.ThreadSafeObservableCollection<SourceGroupModel>();

        public void ReorderAll()
        {
            int iFeedSortOrder = 10;
            for (int i = 0; i < _signalGroups.Count; i++)
            {
                SourceGroupModel grp = _signalGroups[i];
                grp.DisplayOrder = iFeedSortOrder;
                iFeedSortOrder += 10;
                for (int j = 0; i < grp.SignalSources.Count; j++)
                {
                    BaseSourceModel src = grp.SignalSources[i];
                    src.DisplayOrder = iFeedSortOrder;
                    iFeedSortOrder += 10;
                }

                grp.SignalSources.RaiseCollectionChanged();
            }

            this.SignalGroups.RaiseCollectionChanged();

            Common.AppSettings.Instance.SaveSettingsFile();
        }

        public RadioLog.WPFCommon.ThreadSafeObservableCollection<SourceGroupModel> SignalGroups { get { return _signalGroups; } }
        public RadioLog.WPFCommon.ThreadSafeObservableCollection<RadioSignalItemModel> RadioLog { get { return _radioLog; } }
        public Services.RollCallService RollCallService { get { return _rollCallService; } }
        public RadioLog.WPFCommon.ThreadSafeObservableCollection<EmergencyRadioSignalItemModel> EmergencyRadioLog { get { return _emergencyRadioLog; } }
        public int MaxLogDisplayItems
        {
            get { return _maxLogDisplayItems; }
            set
            {
                if (_maxLogDisplayItems != value)
                {
                    _maxLogDisplayItems = value;
                    OnPropertyChanged();
                }
            }
        }

        [ImportingConstructor]
        public MainViewModel(IViewAwareStatus viewAwareStatus)
        {
            if (MEFedMVVM.Common.Designer.IsInDesignMode)
                return;

            _tAudioCheck = new System.Threading.Thread(() =>
            {
                while (_shouldRunThreads)
                {
                    foreach (SourceGroupModel grp in SignalGroups)
                    {
                        grp.UpdateHasAudio();
                    }
                    System.Threading.Thread.Sleep(1500);
                }
            });
            _tAudioCheck.IsBackground = true;
            _tAudioCheck.Name = "Audio Check Thread";

            if (Common.AppSettings.Instance.UseGroups)
            {
                foreach (Common.SignalGroup grp in Common.AppSettings.Instance.SignalGroups)
                {
                    AddSignalGroupToList(grp);
                }
            }
            else
            {
                Common.SignalGroup mainGroup = Common.AppSettings.Instance.SignalGroups.FirstOrDefault(g => g.GroupId == Guid.Empty);
                if (mainGroup != null)
                {
                    AddSignalGroupToList(mainGroup);
                }
            }

            _viewStatus = viewAwareStatus;
            _viewStatus.ViewLoaded += viewAwareStatus_ViewLoaded;
            Cinch.Mediator.Instance.RegisterHandler<Common.SignalSourceGroupChangeHolder>("SOURCE_GROUP_CHANGE", ProcessSourceGroupChange);
            Cinch.Mediator.Instance.RegisterHandler<Common.SignalSource>("NEW_SIGNAL_SOURCE", ProcessNewSignalSource);
            Cinch.Mediator.Instance.RegisterHandler<Guid>("DELETE_SIGNAL_SOURCE", ProcessRemoveSignalSource);
            Cinch.Mediator.Instance.RegisterHandler<Guid>("UPDATE_SIGNAL_SOURCE", ProcessRefreshSignalSource);
            Cinch.Mediator.Instance.RegisterHandler<Guid>("GROUP_CHANGED", ProcessGroupUpdated);
            Cinch.Mediator.Instance.RegisterHandler<Guid>("GROUP_DELETED", ProcessGroupDeleted);
            Cinch.Mediator.Instance.RegisterHandler<Common.SignalGroup>("NEW_GROUP_ADDED", ProcessGroupAdded);
            Cinch.Mediator.Instance.RegisterHandler<Guid>("TOGGLE_SOURCE_MUTE_OTHERS", ProcessToggleSourceMuteOthers);
            Cinch.Mediator.Instance.RegisterHandler<Guid>("END_IF_NON_MUTED_SOURCE", ProcessShutdownSourceMuteOthers);
            Cinch.Mediator.Instance.RegisterHandler<bool>("AUDIO_MUTE_STATE_CHANGED", ProcessAudioMuteStateChanged);

            Cinch.Mediator.Instance.RegisterHandler<bool>("REFRESH_RADIO_LOOKUPS", (b) =>
            {
                foreach (RadioSignalItemModel rSig in RadioLog) { rSig.UpdateLookups(); }
                foreach (EmergencyRadioSignalItemModel eSig in EmergencyRadioLog) { eSig.UpdateLookups(); }
            });
            Cinch.Mediator.Instance.RegisterHandler<bool>("SETTINGS_CHANGED", (b) =>
            {
                GridFontSize = Common.AppSettings.Instance.GridFontSize;
                GridSmallFontSize = Common.AppSettings.Instance.GridFontSize - 4;
                GridLargeFontSize = Common.AppSettings.Instance.GridFontSize + 4;
                foreach (SourceGroupModel grp in _signalGroups)
                {
                    grp.UpdateForSystemSettingsChanged();
                }
            });

            UpdateEmergencyAlarmSoundPlayer();
        }

        void viewAwareStatus_ViewLoaded()
        {
            MainWindow mw = _viewStatus.View as MainWindow;
            if (mw != null)
            {
                mw.MainView.SetupColumnWidths();
            }

            if (!_powerSaveSetup)
            {
                _powerSaveSetup = true;
                Microsoft.Win32.SystemEvents.PowerModeChanged += (s, a) =>
                {
                    switch (a.Mode)
                    {
                        case Microsoft.Win32.PowerModes.Resume:
                            {
                                if (_isInPowerSave)
                                {
                                    _isInPowerSave = false;
                                    StartupSources();
                                }
                                break;
                            }
                        case Microsoft.Win32.PowerModes.Suspend:
                            {
                                if (!_isInPowerSave)
                                {
                                    _isInPowerSave = true;
                                    Common.AppSettings.Instance.SaveSettingsFile();
                                    ShutdownSources();
                                }
                                break;
                            }
                    }
                };
            }

            Services.AudioMuteService.Instance.Start();
        }
        
        private void AddSignalSourceToList(Common.SignalSource src)
        {
            SourceGroupModel grp = null;
            if (Common.AppSettings.Instance.UseGroups)
                grp = _signalGroups.FirstOrDefault(g => g.GroupId == src.GroupId);
            else
                grp = _signalGroups.FirstOrDefault(g => g.GroupId == Guid.Empty);
            if (grp != null)
            {
                grp.AddSignalSourceToList(src);

                _signalGroups.Sort();
            }
        }
        private void AddSignalGroupToList(Common.SignalGroup grp)
        {
            SourceGroupModel processor = new SourceGroupModel(grp);
            if (processor != null)
            {
                _signalGroups.Add(processor);

                _signalGroups.Sort();
            }
        }
        
        delegate void SignalSourceChangeDelegate(Guid id);
        delegate void NewSignalSourceDelegate(Common.SignalSource newSrc);
        delegate void SourceGroupChangeDelegate(Common.SignalSourceGroupChangeHolder info);

        private void ProcessGroupUpdated(Guid id)
        {
            if (!Common.AppSettings.Instance.UseGroups && id != Guid.Empty)
                return;
            foreach (SourceGroupModel grp in _signalGroups.Where(g => g.GroupId == id))
            {
                grp.RefreshGroupInfo();
            }
            _signalGroups.Sort();
        }
        private void ProcessGroupAdded(Common.SignalGroup grp)
        {
            if (!Common.AppSettings.Instance.UseGroups && grp.GroupId != Guid.Empty)
                return;
            if (grp == null || _signalGroups.FirstOrDefault(g => g.GroupId == grp.GroupId) != null)
                return;
            AddSignalGroupToList(grp);
        }
        private void ProcessGroupDeleted(Guid id)
        {
            List<BaseSourceModel> sources = new List<BaseSourceModel>();
            List<SourceGroupModel> grps = new List<SourceGroupModel>();
            foreach (SourceGroupModel grp in _signalGroups.Where(g => g.GroupId == id))
            {
                grps.Add(grp);
                foreach (BaseSourceModel src in grp.SignalSources)
                {
                    sources.Add(src);
                }
            }
            SourceGroupModel dfltGrp = _signalGroups.FirstOrDefault(g => g.GroupId == Guid.Empty);
            if (dfltGrp == null)
                return;
            foreach (BaseSourceModel src in sources)
            {
                src.SrcInfo.GroupId = Guid.Empty;
                src.GroupInfo = dfltGrp.GroupInfo;
                dfltGrp.SignalSources.Add(src);
            }
            foreach (SourceGroupModel grp in grps)
            {
                _signalGroups.Remove(grp);
            }
            Common.AppSettings.Instance.SaveSettingsFile();
        }
        private void ProcessSourceGroupChange(Common.SignalSourceGroupChangeHolder info)
        {
            if (_viewStatus != null && _viewStatus.View != null && !(_viewStatus.View as DispatcherObject).Dispatcher.CheckAccess())
            {
                (_viewStatus.View as DispatcherObject).Dispatcher.BeginInvoke(new SourceGroupChangeDelegate(ProcessSourceGroupChange), info);
            }
            else
            {
                BaseSourceModel src = null;
                foreach (SourceGroupModel grp in _signalGroups)
                {
                    BaseSourceModel tmp = grp.SignalSources.FirstOrDefault(s => s.SignalSourceId == info.SignalSourceId);
                    if (tmp != null)
                    {
                        src = tmp;
                        grp.SignalSources.Remove(tmp);
                        break;
                    }
                }
                if (src != null)
                {
                    SourceGroupModel grp = _signalGroups.FirstOrDefault(g => g.GroupId == info.NewGroupId);
                    if (grp != null && grp.SignalSources.FirstOrDefault(s => s.SignalSourceId == info.SignalSourceId) == null)
                    {
                        grp.AddSignalSourceModelToList(src);
                    }
                }
            }
        }
        private void ProcessNewSignalSource(Common.SignalSource newSrc)
        {
            if (_viewStatus != null && _viewStatus.View != null && !(_viewStatus.View as DispatcherObject).Dispatcher.CheckAccess())
            {
                (_viewStatus.View as DispatcherObject).Dispatcher.BeginInvoke(new NewSignalSourceDelegate(ProcessNewSignalSource), newSrc);
            }
            else
            {
                AddSignalSourceToList(newSrc);
            }
        }
        private void ProcessRemoveSignalSource(Guid id)
        {
            foreach (SourceGroupModel mdl in _signalGroups)
            {
                mdl.ProcessRemoveSignalSource(id);
            }
        }
        private void ProcessRefreshSignalSource(Guid id)
        {
            if (_viewStatus != null && _viewStatus.View != null && !(_viewStatus.View as DispatcherObject).Dispatcher.CheckAccess())
            {
                (_viewStatus.View as DispatcherObject).Dispatcher.BeginInvoke(new SignalSourceChangeDelegate(ProcessRefreshSignalSource), id);
            }
            else
            {
                bool bDone = false;
                foreach (SourceGroupModel mdl in _signalGroups)
                {
                    bDone |= mdl.ProcessRefreshSignalSource(id);
                    if (bDone)
                        break;
                }
                if (!bDone)
                {
                    foreach (SourceGroupModel mdl in _signalGroups)
                    {
                        mdl.ProcessRemoveSignalSource(id);
                    }
                    Common.SignalSource src = Common.AppSettings.Instance.SignalSources.FirstOrDefault(s => s.SourceId == id);
                    if (src != null)
                    {
                        ProcessNewSignalSource(src);
                    }
                }
            }
        }
        private void ProcessAudioMuteStateChanged(bool b)
        {
            switch (Services.AudioMuteService.Instance.MuteState)
            {
                case Common.MuteState.Normal:
                    {
                        foreach (ViewModels.SourceGroupModel group in this.SignalGroups)
                        {
                            if (!group.GroupMuted)
                            {
                                foreach (ViewModels.BaseSourceModel source in group.SignalSources)
                                {
                                    source.IsNonMutedSource = false;
                                    source.IsMuted = false;
                                }
                            }
                        }
                        break;
                    }
                default:
                    {
                        _unmutedSignalSourceId = Services.AudioMuteService.Instance.ActiveAudioFeed;
                        foreach (ViewModels.SourceGroupModel group in this.SignalGroups)
                        {
                            foreach (ViewModels.BaseSourceModel mdl in group.SignalSources)
                            {
                                if (mdl.SignalSourceId == Services.AudioMuteService.Instance.ActiveAudioFeed)
                                {
                                    mdl.IsNonMutedSource = true;
                                    mdl.IsMuted = false;
                                }
                                else
                                {
                                    mdl.IsNonMutedSource = false;
                                    mdl.IsMuted = true;
                                }
                            }
                        }
                        break;
                    }
            }
        }
        private void ProcessShutdownSourceMuteOthers(Guid id)
        {
            if (_unmutedSignalSourceId == id)
            {
                ProcessToggleSourceMuteOthers(id);
            }
        }
        private void ProcessToggleSourceMuteOthers(Guid id)
        {
            if (_unmutedSignalSourceId == id)
            {
                _unmutedSignalSourceId = null;
                foreach (ViewModels.SourceGroupModel group in this.SignalGroups)
                {
                    if (!group.GroupMuted)
                    {
                        foreach (ViewModels.BaseSourceModel mdl in group.SignalSources)
                        {
                            mdl.IsMuted = false;
                            mdl.IsNonMutedSource = false;
                        }
                    }
                }
            }
            else
            {
                _unmutedSignalSourceId = id;
                foreach (ViewModels.SourceGroupModel group in this.SignalGroups)
                {
                    foreach (ViewModels.BaseSourceModel mdl in group.SignalSources)
                    {
                        if (mdl.SignalSourceId == id)
                        {
                            mdl.IsNonMutedSource = true;
                            mdl.IsMuted = false;
                        }
                        else
                        {
                            mdl.IsNonMutedSource = false;
                            mdl.IsMuted = true;
                        }
                    }
                }
            }
        }

        public DataGridLength ColTimeWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColTimeWidth); } set { Common.AppSettings.Instance.ColTimeWidth = value.Value; } }
        public DataGridLength ColSourceNameWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColSourceNameWidth); } set { Common.AppSettings.Instance.ColSourceNameWidth = value.Value; } }
        public DataGridLength ColUnitIdWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColUnitIdWidth); } set { Common.AppSettings.Instance.ColUnitIdWidth = value.Value; } }
        public DataGridLength ColAgencyNameWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColAgencyNameWidth); } set { Common.AppSettings.Instance.ColAgencyNameWidth = value.Value; } }
        public DataGridLength ColUnitNameWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColUnitNameWidth); } set { Common.AppSettings.Instance.ColUnitNameWidth = value.Value; } }
        public DataGridLength ColRadioNameWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColRadioNameWidth); } set { Common.AppSettings.Instance.ColRadioNameWidth = value.Value; } }
        public DataGridLength ColAssignedRoleWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColAssignedRoleWidth); } set { Common.AppSettings.Instance.ColAssignedRoleWidth = value.Value; } }
        public DataGridLength ColAssignedPersonnelWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColAssignedPersonnelWidth); } set { Common.AppSettings.Instance.ColAssignedPersonnelWidth = value.Value; } }
        public DataGridLength ColSourceTypedWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColSourceTypedWidth); } set { Common.AppSettings.Instance.ColSourceTypedWidth = value.Value; } }
        public DataGridLength ColDescriptionWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColDescriptionWidth); } set { Common.AppSettings.Instance.ColDescriptionWidth = value.Value; } }

        public DataGridLength ColAlarmTimeWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColAlarmTimeWidth); } set { Common.AppSettings.Instance.ColAlarmTimeWidth = value.Value; } }
        public DataGridLength ColAlarmUnitNameWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColAlarmUnitNameWidth); } set { Common.AppSettings.Instance.ColAlarmUnitNameWidth = value.Value; } }
        public DataGridLength ColAlarmRadioNameWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColAlarmRadioNameWidth); } set { Common.AppSettings.Instance.ColAlarmRadioNameWidth = value.Value; } }
        public DataGridLength ColAlarmAssignedRoleWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColAlarmAssignedRoleWidth); } set { Common.AppSettings.Instance.ColAlarmAssignedRoleWidth = value.Value; } }
        public DataGridLength ColAlarmAssignedPersonnelWidth { get { return new DataGridLength(Common.AppSettings.Instance.ColAlarmAssignedPersonnelWidth); } set { Common.AppSettings.Instance.ColAlarmAssignedPersonnelWidth = value.Value; } }

        public void DoClearDisplay()
        {
            RadioLog.Clear();
            EmergencyRadioLog.Clear();
            RollCallService.ClearRollCallParticipants();

            //clear units
            //start new logging session
        }

        public int GridFontSize
        {
            get { return _gridFontSize; }
            set
            {
                if (_gridFontSize != value)
                {
                    _gridFontSize = value;
                    OnPropertyChanged();
                }
            }
        }
        public int GridSmallFontSize
        {
            get { return _gridSmallFontSize; }
            set
            {
                if (_gridSmallFontSize != value)
                {
                    _gridSmallFontSize = value;
                    OnPropertyChanged();
                }
            }
        }
        public int GridLargeFontSize
        {
            get { return _gridLargeFontSize; }
            set
            {
                if (_gridLargeFontSize != value)
                {
                    _gridLargeFontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        internal void SaveVisuals(MainWindow mw)
        {
            if (mw == null)
                return;

            mw.MainView.SaveColumnWidths();

            Common.AppSettings.Instance.SaveSettingsFile();
        }

        internal void ShutdownSources()
        {
            foreach (SourceGroupModel processor in _signalGroups)
            {
                processor.Stop();
            }
        }

        internal void StartupSources()
        {
            foreach (SourceGroupModel processor in _signalGroups)
            {
                processor.Start();
            }
        }
    }
}

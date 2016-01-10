using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.ViewModels
{
    public abstract class BaseSourceModel : RadioLog.WPFCommon.ThreadSafeViewModelBase, RadioLog.Common.ISourceStatus, IComparable<BaseSourceModel>
    {
        private Common.SignalSource _src;
        private Common.SignalGroup _group;
        private RadioLog.Common.RadioSignalLogger _logger = null;
        private string _sourceTitle = string.Empty;
        private string _lastValidSourceTitle = string.Empty;
        private bool _hasAudio = false;
        private bool _isMuted = false;
        private bool _isConnected = false;
        private bool _feedActive = true;
        private bool _isNonMutedSource = false;
        private bool _loggingEnabled = RadioLog.Common.AppSettings.Instance.GlobalLoggingEnabled;
        private Common.ProcessorViewSize _viewSize = RadioLog.Common.AppSettings.Instance.ViewSize;

        protected BaseSourceModel(Common.SignalSource src, Common.SignalGroup group)
        {
            if (src == null)
                throw new ArgumentNullException();
            _src = src;
            _group = group;

            Cinch.Mediator.Instance.RegisterHandler<bool>("REFRESH_VISUALS", (b) =>
            {
                DoRefreshVisuals();
            });
        }

        public void GroupColorChanged()
        {
            OnPropertyChanged("SourceColor");
            OnPropertyChanged("CalculatedSourceColor");
        }

        public void UpdateForSystemSettingsChanged()
        {
            _loggingEnabled = RadioLog.Common.AppSettings.Instance.GlobalLoggingEnabled;
            _viewSize = RadioLog.Common.AppSettings.Instance.ViewSize;

            OnPropertyChanged("ViewSize");
            OnPropertyChanged("FontSize");
            OnPropertyChanged("SmallFontSize");
            OnPropertyChanged("ViewWidth");
            OnPropertyChanged("AuxControlsVisible");
            OnPropertyChanged("RecordingButtonVisible");
            OnPropertyChanged("IsNonMutedSourceEnabled");
        }

        internal virtual void DoRefreshVisuals()
        {
            DoRefreshStreamStatusVisuals();
            OnPropertyChanged("SourceTitle");
            OnPropertyChanged("Volume");
            OnPropertyChanged("RecordAudio");
            OnPropertyChanged("IsEnabled");
            OnPropertyChanged("IsMuted");
            OnPropertyChanged("SourceName");
            OnPropertyChanged("HasAudio");
            OnPropertyChanged("SourceColor");
            OnPropertyChanged("CalculatedSourceColor");
        }

        internal virtual void DoRefreshStreamStatusVisuals()
        {
            OnPropertyChanged("StreamStatus");
            OnPropertyChanged("StreamStatusBrush");
            OnPropertyChanged("StreamStatusString");
            OnPropertyChanged("IsNonMutedSourceEnabled");
        }

        public Common.SignalGroup GroupInfo { get { return _group; } set { _group = value; OnPropertyChanged("GroupInfo"); OnPropertyChanged("CalculatedSourceColor"); } }
        public Guid SignalSourceId
        {
            get
            {
                if (_src != null)
                    return _src.SourceId;
                else
                    throw new ArgumentNullException();
            }
        }
        public bool IsPriority { get { return GetSourcePriority(); } }
        public Common.SignalSource SrcInfo { get { return _src; } }
        public virtual string SourceName
        {
            get { return _src.SourceName; }
        }
        public virtual string SourceColor
        {
            get { return _src.SourceColor; }
        }
        public string CalculatedSourceColor
        {
            get
            {
                string selfColor = this.SourceColor;
                if (string.IsNullOrWhiteSpace(selfColor) && _group != null)
                {
                    return _group.GroupColorString;
                }
                return selfColor;
            }
        }

        public virtual BaseSourceModel[] GetSubSourceModels() { return null; }
        public virtual bool SupportsSubSources { get { return false; } }
        public virtual bool IsSubSource { get { return false; } }
        public virtual Guid? ParentSourceId { get { return null; } }

        #region Value Values
        public Common.ProcessorViewSize ViewSize { get { return _viewSize; } }
        public int FontSize { get { return (ViewSize == Common.ProcessorViewSize.Small) ? 12 : 14; } }
        public int SmallFontSize { get { return (ViewSize == Common.ProcessorViewSize.Small) ? 10 : 12; } }
        public int ViewWidth
        {
            get
            {
                switch (ViewSize)
                {
                    case Common.ProcessorViewSize.Small: return 220;
                    case Common.ProcessorViewSize.Wide: return 420;
                    case Common.ProcessorViewSize.ExtraWide: return 620;
                    default: return 340;
                }
            }
        }
        public System.Windows.Visibility AuxControlsVisible { get { return (ViewSize == Common.ProcessorViewSize.Small) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible; } }
        public System.Windows.Visibility EnabledButtonVisible { get { return System.Windows.Visibility.Visible; } }
        public System.Windows.Visibility RecordingButtonVisible { get { return RecordingSupported ? AuxControlsVisible : System.Windows.Visibility.Collapsed; } }
        #endregion

        public virtual Common.SignalingSourceType SourceType { get { if (SrcInfo != null) return SrcInfo.SourceType; else throw new ArgumentNullException(); } }
        public string SourceTitle { get { return _sourceTitle; } set { if (_sourceTitle != value) { _sourceTitle = value; OnPropertyChanged(); } } }

        #region SourceSetGetMethods
        protected virtual void SetSourceEnabled(bool bEnabled) { _src.IsEnabled = bEnabled; }
        protected virtual void SetSourceRecordAudio(bool bEnabled) { _src.RecordAudio = bEnabled; }

        protected virtual bool GetSourceEnabled() { return _src.IsEnabled; }
        protected virtual bool GetSourcePriority() { return _src.IsPriority; }
        protected virtual bool GetSourceRecordAudio() { return _src.RecordAudio; }
        #endregion

        public bool HasAudio
        {
            get { return _hasAudio; }
            set
            {
                if (_hasAudio != value)
                {
                    _hasAudio = value;

                    OnPropertyChanged();
                    DoRefreshStreamStatusVisuals();

                    Services.AudioMuteService.Instance.NotifyAudioActive(this.SignalSourceId, value);
                }
            }
        }
        public bool FeedActive
        {
            get { return _feedActive; }
            set
            {
                if (_feedActive != value)
                {
                    _feedActive = value;
                    OnPropertyChanged();
                    if (!_feedActive)
                    {
                        //Cinch.Mediator.Instance.NotifyColleagues<Guid>("END_IF_NON_MUTED_SOURCE", this.SignalSourceId);
                        Services.AudioMuteService.Instance.ProcessShutdownSourceMuteOthers(this.SignalSourceId);
                    }
                    DoRefreshStreamStatusVisuals();
                }
            }
        }
        public string LastValidSourceTitle { get { return _lastValidSourceTitle; } set { if (_lastValidSourceTitle != value) { _lastValidSourceTitle = value; OnPropertyChanged(); } } }
        public bool IsEnabled
        {
            get { return GetSourceEnabled(); }
            set
            {
                if (GetSourceEnabled() != value)
                {
                    SetSourceEnabled(value);
                    //RadioLog.Common.AppSettings.Instance.MarkModified();
                    RadioLog.Common.AppSettings.Instance.SaveSettingsFile();
                    OnPropertyChanged();
                    if (GetSourceEnabled())
                        Start();
                    else
                        Stop();
                }
            }
        }
        public bool RecordAudio
        {
            get
            {
                if (RecordingSupported)
                    return GetSourceRecordAudio();
                else
                    return false;
            }
            set
            {
                if (!RecordingSupported)
                    return;
                if (GetSourceRecordAudio() != value)
                {
                    SetSourceRecordAudio(value);
                    //RadioLog.Common.AppSettings.Instance.MarkModified();
                    RadioLog.Common.AppSettings.Instance.SaveSettingsFile();
                    OnPropertyChanged();
                    InternalSetRecordAudio(GetSourceRecordAudio());
                }
            }
        }
        public bool IsNonMutedSourceEnabled
        {
            get
            {
                if (IsNonMutedSource)
                    return true;
                if (!IsEnabled)
                    return false;
                switch (StreamStatus)
                {
                    case Common.SignalingSourceStatus.Disabled:
                    case Common.SignalingSourceStatus.Disconnected:
                    case Common.SignalingSourceStatus.FeedNotActive:
                        return false;
                    default:
                        return true;
                }
            }
        }
        public bool IsNonMutedSource
        {
            get { return _isNonMutedSource; }
            set
            {
                if (_isNonMutedSource != value)
                {
                    _isNonMutedSource = value;
                    OnPropertyChanged();
                    OnPropertyChanged("IsNonMutedSourceEnabled");
                }
            }
        }
        public bool IsMuted
        {
            get { return _isMuted; }
            set
            {
                _isMuted = value;
                OnPropertyChanged();
                DoRefreshStreamStatusVisuals();
                InternalSetProcessorVolume(_src.Volume, _src.MaxVolume, IsMuted ? Common.MuteState.Muted : Common.MuteState.Normal);
            }
        }
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    DoRefreshStreamStatusVisuals();
                }
            }
        }

        protected abstract bool ProviderHasAudio { get; }

        private void BackgroundThreadDoStart()
        {
            try
            {
                Stop();
            }
            finally
            {
                System.Threading.Thread.Sleep(500);
            }
            DoStart();
        }

        public void Start()
        {
            if (_logger == null)
            {
                _logger = new Common.RadioSignalLogger(SourceName, Common.AppSettings.Instance.LogFileDirectory);
            }
            if (IsEnabled)
            {
                System.Threading.Thread t = new System.Threading.Thread(BackgroundThreadDoStart);
                t.IsBackground = true;
                t.Start();
            }

            DoRefreshStreamStatusVisuals();
        }

        protected virtual void ProcessRadioSignalItem(RadioLog.Common.RadioSignalingItem sigItem)
        {
            if (sigItem == null)
                return;
            sigItem.Latitude = Services.GeoService.Instance.CurrentLatitude;
            sigItem.Longitude = Services.GeoService.Instance.CurrentLongitude;
            Common.RadioInfoLookupHelper.Instance.PerformLookupsOnRadioSignalItem(sigItem);
            if (_loggingEnabled)
            {
                _logger.LogRadioSignal(sigItem);
            }
            RadioSignalItemModel mdl = new RadioSignalItemModel(sigItem);
            MainViewModel.ProcessRadioSignal(mdl);

            SourceTitle = mdl.Description;
            LastValidSourceTitle = mdl.Description;
        }

        public virtual void Stop()
        {
            try
            {
                if (IsRunning)
                {
                    Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Red, "Stopping {0}", SourceName);
                }
            }
            finally
            {
                DoRefreshStreamStatusVisuals();
                //Cinch.Mediator.Instance.NotifyColleagues<Guid>("END_IF_NON_MUTED_SOURCE", this.SignalSourceId);
                Services.AudioMuteService.Instance.ProcessShutdownSourceMuteOthers(this.SignalSourceId);
            }
        }

        public int Volume
        {
            get
            {
                if (VolumeControlSupported)
                    return _src.Volume;
                else
                    return 0;
            }
            set
            {
                if (!VolumeControlSupported)
                    return;
                if (value != _src.Volume)
                {
                    _src.Volume = value;
                    RadioLog.Common.AppSettings.Instance.MarkModified();
                    OnPropertyChanged();
                }
                InternalSetProcessorVolume(_src.Volume, _src.MaxVolume, IsMuted ? Common.MuteState.Muted : Common.MuteState.Normal);
            }
        }
        public int MaxVolume
        {
            get { return _src.MaxVolume; }
        }

        public abstract bool VolumeControlSupported { get; }
        public abstract bool RecordingSupported { get; }
        public abstract bool IsRunning { get; }
        protected abstract void InternalSetProcessorVolume(int newVol, int maxVol, Common.MuteState muteState);
        protected abstract void InternalSetRecordAudio(bool bRecord);
        protected abstract void DoStart();
        protected abstract void InternalRefreshSource();

        public virtual int DisplayOrder
        {
            get { return SrcInfo.DisplayOrder; }
            set { SrcInfo.DisplayOrder = value; }
        }

        public void DoRefreshSource()
        {
            InternalRefreshSource();
            OnPropertyChanged("SourceName");
            OnPropertyChanged("SourceColor");
        }

        public virtual RadioLog.Common.SignalingSourceStatus StreamStatus
        {
            get
            {
                if (IsEnabled == false)
                    return Common.SignalingSourceStatus.Disabled;
                if (!FeedActive)
                    return Common.SignalingSourceStatus.FeedNotActive;
                if (!IsConnected)
                    return Common.SignalingSourceStatus.Disconnected;
                if (IsMuted)
                    return Common.SignalingSourceStatus.Muted;
                if (HasAudio)
                    return Common.SignalingSourceStatus.AudioActive;
                return Common.SignalingSourceStatus.Idle;
            }
        }
        public virtual string StreamStatusString
        {
            get
            {
                switch (StreamStatus)
                {
                    case Common.SignalingSourceStatus.Disabled: return "Disabled";
                    case Common.SignalingSourceStatus.Disconnected: return "Disconnected";
                    case Common.SignalingSourceStatus.FeedNotActive: return "Feed Not Active";
                    case Common.SignalingSourceStatus.Muted: return "Muted";
                    default:
                        {
                            if (this.IsSubSource && this.SrcInfo != null)
                                return this.SrcInfo.SourceName;
                            return "Connected";
                        }
                }
            }
        }
        public System.Windows.Media.Brush StreamStatusBrush
        {
            get
            {
                return RadioLog.WPFCommon.GroupColorToBrushConverter.GetStatusBrush(StreamStatus, CalculatedSourceColor);
            }
        }

        public int CompareTo(BaseSourceModel other)
        {
            if (other == null || other.SrcInfo == null)
                return 1;
            if (this.SrcInfo == null)
                return -1;
            return this.DisplayOrder.CompareTo(other.DisplayOrder);
        }

        protected abstract bool GetInternalHasAudio();

        public void UpdateHasAudio()
        {
            bool bHasAudio = GetInternalHasAudio();
            this.HasAudio = bHasAudio;
            Services.AudioMuteService.Instance.NotifyAudioActive(this.SignalSourceId, bHasAudio);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Services
{
    public class AudioMuteService:Common.BaseTaskObject
    {
        private static AudioMuteService _instance = null;
        public static AudioMuteService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AudioMuteService();
                }
                return _instance;
            }
        }

        private List<Guid> _prioritySources = new List<Guid>();
        private bool _autoMuteEnabled = Common.AppSettings.Instance.EnableAutoMute;
        private Guid? _activeAudioFeed = null;
        private long _lastAudioTick = 0;
        private Common.MuteState _muteState = Common.MuteState.Normal;
        private long _maxWaitAudioTick = TimeSpan.FromSeconds(Common.AppSettings.Instance.AutoMuteHangTime).Ticks;

        public Guid? ActiveAudioFeed { get { return _activeAudioFeed; } }
        public Common.MuteState MuteState { get { return _muteState; } }

        private AudioMuteService()
            : base(100)
        {
            RefreshSettings();

            Cinch.Mediator.Instance.RegisterHandler<bool>("SETTINGS_CHANGED", (b) =>
            {
                RefreshSettings();
            });
        }

        private void RefreshSettings()
        {
            bool oldAutoMuteEnabled = _autoMuteEnabled;
            _autoMuteEnabled = Common.AppSettings.Instance.EnableAutoMute;
            _maxWaitAudioTick = TimeSpan.FromSeconds(Common.AppSettings.Instance.AutoMuteHangTime).Ticks;
            RefreshPrioritySources();

            if (oldAutoMuteEnabled != _autoMuteEnabled)
            {
                _activeAudioFeed = null;
                _lastAudioTick = 0;
                _muteState = Common.MuteState.Normal;
                NotifyMuteChanged();
            }
        }
        private void RefreshPrioritySources()
        {
            _prioritySources.Clear();
            foreach (Common.SignalSource src in Common.AppSettings.Instance.SignalSources.Where(ss => ss.IsEnabled && ss.IsPriority == true && ss.SourceType == Common.SignalingSourceType.Streaming))
            {
                _prioritySources.Add(src.SourceId);
            }
        }
        private bool IsFeedPriority(Guid? id)
        {
            if (id == null || !id.HasValue)
                return false;
            return _prioritySources.Contains(id.Value);
        }
        private bool IsActiveFeedPriority() { return IsFeedPriority(_activeAudioFeed); }

        private void DoClockTickCheck(long ticks)
        {
            if (_muteState == Common.MuteState.Normal)
            {
                _activeAudioFeed = null;
                _lastAudioTick = 0;
                NotifyMuteChanged();
                return;
            }
            if(_muteState== Common.MuteState.Muted)
            {
                _lastAudioTick = 0;
                NotifyMuteChanged();
                return;
            }
            if (!_autoMuteEnabled || _lastAudioTick == 0)
            {
                _activeAudioFeed = null;
                _lastAudioTick = 0;
                _muteState = Common.MuteState.Normal;
                NotifyMuteChanged();
                return;
            }
            if (ticks - _lastAudioTick > _maxWaitAudioTick)
            {
                _activeAudioFeed = null;
                _lastAudioTick = 0;
                _muteState = Common.MuteState.Normal;
                NotifyMuteChanged();
            }
        }
        public void NotifyAudioActive(Guid id, bool hasAudio)
        {
            if (!_autoMuteEnabled || _muteState == Common.MuteState.Muted)
                return;
            if (id == _activeAudioFeed)
            {
                if (hasAudio)
                    _lastAudioTick = DateTime.Now.Ticks;
            }
            else if (hasAudio)
            {
                if ((_activeAudioFeed.HasValue && (!IsActiveFeedPriority() || IsFeedPriority(id))) || !_activeAudioFeed.HasValue)
                {
                    _activeAudioFeed = id;
                    _lastAudioTick = DateTime.Now.Ticks;
                    _muteState = Common.MuteState.AutoMuted;
                    NotifyMuteChanged();
                }
            }
        }

        public void ProcessToggleSourceMuteOthers(Guid? id)
        {
            if (id == null && _muteState != Common.MuteState.AutoMuted)
            {
                _activeAudioFeed = null;
                _muteState = Common.MuteState.Normal;
            }
            else
            {
                if (id == null || _activeAudioFeed == id)
                {
                    _activeAudioFeed = null;
                    _muteState = Common.MuteState.Normal;
                }
                else
                {
                    _activeAudioFeed = id;
                    _muteState = Common.MuteState.Muted;
                }
            }
            NotifyMuteChanged();
        }
        public void ProcessShutdownSourceMuteOthers(Guid id)
        {
            if (_activeAudioFeed == id)
            {
                _activeAudioFeed = null;
                _muteState = Common.MuteState.Normal;
                NotifyMuteChanged();
            }
        }

        private void NotifyMuteChanged() { Cinch.Mediator.Instance.NotifyColleagues<bool>("AUDIO_MUTE_STATE_CHANGED", _muteState != Common.MuteState.Normal); }

        protected override void ThreadProc()
        {
            DoClockTickCheck(DateTime.Now.Ticks);
        }
    }
}

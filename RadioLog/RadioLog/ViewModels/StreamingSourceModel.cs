using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.ViewModels
{
    public class StreamingSourceModel:BaseSourceModel
    {
        private RadioLog.AudioProcessing.WaveStreamProcessor _processor;

        public StreamingSourceModel(Common.SignalSource src, Common.SignalGroup group) : base(src, group) { }

        public override bool VolumeControlSupported { get { return true; } }
        public override bool RecordingSupported { get { return true; } }
        public override bool IsRunning
        {
            get
            {
                if (_processor == null)
                    return false;
                else
                    return _processor.IsStreamActive;
            }
        }

        protected override void InternalSetProcessorVolume(int newVol, int maxVol, Common.MuteState muteState)
        {
            if (_processor != null)
            {
                switch (muteState)
                {
                    case Common.MuteState.Normal:
                        {
                            if (newVol < 1)
                                _processor.CurrentVolume = 0.0f; //mute...
                            else
                            {
                                float tmpVol = (float)Math.Min(newVol, maxVol);
                                _processor.CurrentVolume = (float)(tmpVol / maxVol);
                            }
                            break;
                        }
                    case Common.MuteState.Muted:
                        {
                            _processor.CurrentVolume = 0f;
                            break;
                        }
                    case Common.MuteState.AutoMuted:
                        {
                            _processor.CurrentVolume = 0f;
                            break;
                        }
                }
            }
        }

        protected override void InternalSetRecordAudio(bool bRecord)
        {
            if (_processor != null)
            {
                _processor.SetRecordingEnabled(bRecord);
            }
        }

        protected override bool ProviderHasAudio { get { return (_processor != null) ? _processor.HasSound : false; } }
        private void HasPropertyChanged(bool bUpdateTitle)
        {
            if (_processor != null)
            {
                IsConnected = _processor.IsStreamActive;
                FeedActive = _processor.FeedActive;
                if (IsEnabled && IsConnected)
                {
                    HasAudio = _processor.HasSound;
                }
                else
                {
                    HasAudio = false;
                }
                if (bUpdateTitle)
                {
                    SourceTitle = _processor.StreamTitle;
                    LastValidSourceTitle = _processor.LastValidStreamTitle;
                }
            }
            OnPropertyChanged("StreamStatus");
        }

        protected override void DoStart()
        {
            if (_processor == null && SrcInfo != null)
            {
                float tmpVol = 0.0f;
                if (Volume > 0)
                {
                    tmpVol = (float)(Math.Min(Volume, MaxVolume) / MaxVolume);
                }
                _processor = new AudioProcessing.WaveStreamProcessor(SrcInfo.SourceLocation, SrcInfo.SourceName, ProcessRadioSignalItem, HasPropertyChanged, tmpVol, SrcInfo.RecordAudio, SrcInfo.RecordingType, SrcInfo.RecordingKickTime, SrcInfo.NoiseFloor, SrcInfo.CustomNoiseFloor, SrcInfo.RemoveNoise, SrcInfo.DecodeMDC1200, SrcInfo.DecodeGEStar, SrcInfo.DecodeFleetSync, false /*SrcInfo.DecodeP25*/, SrcInfo.WaveOutDeviceName);
                InternalSetProcessorVolume(Volume, MaxVolume, IsMuted ? Common.MuteState.Muted : Common.MuteState.Normal);
                OnPropertyChanged("Volume");
                OnPropertyChanged("IsMuted");
                OnPropertyChanged("IsEnabled");
            }
        }

        public override void Stop()
        {
            try
            {
                if (_processor != null)
                {
                    _processor.StopStream();
                    _processor = null;
                }
            }
            finally
            {
                base.Stop();
            }
        }

        protected override void InternalRefreshSource()
        {
            if (_processor == null || SrcInfo == null)
                return;

            bool bRestartNeeded = !string.Equals(_processor.StreamURL, SrcInfo.SourceLocation, StringComparison.InvariantCultureIgnoreCase);
            bRestartNeeded |= !string.Equals(_processor.WaveOutDeviceName, SrcInfo.WaveOutDeviceName, StringComparison.InvariantCultureIgnoreCase);

            if (bRestartNeeded)
            {
                this.Stop();
                System.Threading.Thread.Sleep(50);
                this.Start();
            }
            else
            {
                _processor.StreamName = SrcInfo.SourceName;
                _processor.SetRecordingEnabled(SrcInfo.RecordAudio);
                _processor.UpdateRecordingKickTime(SrcInfo.RecordingType, SrcInfo.RecordingKickTime);
                _processor.UpdateNoiseFloor(SrcInfo.NoiseFloor, SrcInfo.CustomNoiseFloor);
                _processor.UpdateRemoveNoise(SrcInfo.RemoveNoise);
                _processor.UpdateDecodeMDC1200(SrcInfo.DecodeMDC1200);
                _processor.UpdateDecodeGEStar(SrcInfo.DecodeGEStar);
                _processor.UpdateDecodeFleetSync(SrcInfo.DecodeFleetSync);
                //_processor.UpdateDecodeP25(SrcInfo.DecodeP25);
            }
        }

        protected override bool GetInternalHasAudio()
        {
            if (_processor != null)
            {
                return _processor.HasSound;
            }
            else
            {
                return false;
            }
        }
    }
}

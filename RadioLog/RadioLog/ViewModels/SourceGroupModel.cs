using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.ViewModels
{
    public class SourceGroupModel : RadioLog.WPFCommon.ThreadSafeViewModelBase, IComparable<SourceGroupModel>
    {
        private Common.SignalGroup _groupInfo = null;
        private WPFCommon.ThreadSafeObservableCollection<BaseSourceModel> _signalSources = new WPFCommon.ThreadSafeObservableCollection<BaseSourceModel>();

        public SourceGroupModel(Common.SignalGroup group)
        {
            if (group == null)
                throw new ArgumentNullException();
            this._groupInfo = group;

            if (Common.AppSettings.Instance.UseGroups)
            {
                foreach (Common.SignalSource src in Common.AppSettings.Instance.SignalSources.Where(ss => ss.GroupId == _groupInfo.GroupId).OrderBy(ss => ss.DisplayOrder))
                {
                    AddSignalSourceToList(src);
                }
            }
            else if (_groupInfo.GroupId == Guid.Empty)
            {
                foreach (Common.SignalSource src in Common.AppSettings.Instance.SignalSources.OrderBy(ss => ss.DisplayOrder))
                {
                    AddSignalSourceToList(src);
                }
            }

            Cinch.Mediator.Instance.RegisterHandler<bool>("REFRESH_VISUALS", (b) =>
            {
                OnPropertyChanged("GroupColor");
            });
        }

        public Common.SignalGroup GroupInfo { get { return _groupInfo; } }
        public Guid GroupId { get { return _groupInfo.GroupId; } }
        public string GroupName
        {
            get { return _groupInfo.GroupName; }
            set
            {
                if (value != _groupInfo.GroupName)
                {
                    _groupInfo.GroupName = value;
                    Common.AppSettings.Instance.MarkModified();
                    OnPropertyChanged();
                }
            }
        }
        public bool GroupMuted
        {
            get { return _groupInfo.GroupMuted; }
            set
            {
                if (value != _groupInfo.GroupMuted)
                {
                    _groupInfo.GroupMuted = value;
                    Common.AppSettings.Instance.MarkModified();
                    OnPropertyChanged();
                    foreach (BaseSourceModel processor in _signalSources)
                    {
                        processor.IsMuted = value;
                    }
                }
            }
        }
        public string GroupColor { get { return _groupInfo.GroupColorString; } }
        public RadioLog.WPFCommon.ThreadSafeObservableCollection<BaseSourceModel> SignalSources { get { return _signalSources; } }

        public virtual int DisplayOrder
        {
            get { return _groupInfo.DisplayOrder; }
            set { _groupInfo.DisplayOrder = value; }
        }

        public void UpdateForSystemSettingsChanged()
        {
            foreach (BaseSourceModel mdl in _signalSources)
            {
                mdl.UpdateForSystemSettingsChanged();
            }
        }

        private BaseSourceModel GetProcessorForStreamingSource(Common.SignalSource src)
        {
            if (src == null)
                return null;
            BaseSourceModel processor = null;
            switch (src.SourceType)
            {
                case Common.SignalingSourceType.Streaming: processor = new StreamingSourceModel(src, _groupInfo); break;
                case Common.SignalingSourceType.WaveInChannel: processor = new WaveInChannelSourceModel(src, _groupInfo); break;
            }
            return processor;
        }
        public void AddSignalSourceToList(Common.SignalSource src)
        {
            BaseSourceModel primarySrc = GetProcessorForStreamingSource(src);
            if (primarySrc != null)
            {
                AddSignalSourceModelToList(primarySrc);
                //sub-items...
                BaseSourceModel[] subSources = primarySrc.GetSubSourceModels();
                if (subSources != null)
                {
                    foreach (BaseSourceModel subSrc in subSources)
                    {
                        AddSignalSourceModelToList(subSrc);
                    }
                }
            }
        }
        public void AddSignalSourceModelToList(BaseSourceModel processor)
        {
            if (processor == null)
                return;
            _signalSources.Add(processor);
            if (!processor.IsRunning)
            {
                processor.Start();
            }
            processor.IsMuted = this.GroupMuted;
            processor.GroupInfo = this.GroupInfo;
            _signalSources.Sort();
        }
        public void ProcessRemoveSignalSource(Guid id)
        {
            List<BaseSourceModel> srcToRemove = new List<BaseSourceModel>(_signalSources.Where(s => s.SignalSourceId == id).ToArray());
            foreach (BaseSourceModel src in srcToRemove)
            {
                _signalSources.Remove(src);
                src.Stop();
            }
        }
        private void ProcessRefreshSubItems(BaseSourceModel curProcessor)
        {
            if (curProcessor == null || !curProcessor.SupportsSubSources)
                return;
            BaseSourceModel[] curSubSources = _signalSources.Where(s => s.ParentSourceId == curProcessor.SignalSourceId).ToArray();
            BaseSourceModel[] cfgSubSources = curProcessor.GetSubSourceModels();
            if (curSubSources == null || curSubSources.Length <= 0)
                return;
            if (cfgSubSources == null || cfgSubSources.Length <= 0)
            {
                foreach (BaseSourceModel subSrc in curSubSources)
                    _signalSources.Remove(subSrc);
            }
            else
            {
                foreach (BaseSourceModel subSrc in curSubSources)
                {
                    if (!cfgSubSources.Contains(subSrc))
                        _signalSources.Remove(subSrc);
                }
                foreach (BaseSourceModel cgfSrc in cfgSubSources)
                {
                    if (!_signalSources.Contains(cgfSrc))
                        _signalSources.Add(cgfSrc);
                }
            }
            _signalSources.Sort();
            foreach (BaseSourceModel subSrc in _signalSources)
            {
                subSrc.DoRefreshSource();
            }
            curProcessor.DoRefreshVisuals();
        }
        public bool ProcessRefreshSignalSource(Guid id)
        {
            bool bDone = false;
            BaseSourceModel curProcessor = _signalSources.FirstOrDefault(s => s.SignalSourceId == id && s.ParentSourceId == null);
            Common.SignalSource src = Common.AppSettings.Instance.SignalSources.FirstOrDefault(s => s.SourceId == id);
            if (src != null && curProcessor != null)
            {
                curProcessor.DoRefreshSource();
                bDone = true;
                _signalSources.Sort();

                ProcessRefreshSubItems(curProcessor);
            }
            return bDone;
        }

        public void RefreshGroupInfo()
        {
            OnPropertyChanged("GroupInfo");
            OnPropertyChanged("GroupName");
            OnPropertyChanged("GroupColor");

            foreach (BaseSourceModel src in _signalSources)
            { 
                src.GroupColorChanged();
            }
        }

        public void Stop()
        {
            foreach (BaseSourceModel src in _signalSources)
            {
                src.Stop();
            }
        }

        public void Start()
        {
            foreach (BaseSourceModel src in _signalSources)
            {
                src.Start();
            }
        }

        public int CompareTo(SourceGroupModel other)
        {
            if (other == null || other.GroupInfo == null)
                return 1;
            if (this.GroupInfo == null)
                return -1;
            return this.GroupInfo.DisplayOrder.CompareTo(other.GroupInfo.DisplayOrder);
        }

        public void UpdateHasAudio()
        {
            foreach (BaseSourceModel src in SignalSources)
            {
                src.UpdateHasAudio();
            }
        }
    }
}

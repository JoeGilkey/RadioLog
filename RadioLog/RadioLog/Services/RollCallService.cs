using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RadioLog.Common;
using RadioLog.ViewModels;

namespace RadioLog.Services
{
    public class RollCallService
    {
        private bool _firegroundEnabled = Common.AppSettings.Instance.WorkstationMode == RadioLogMode.Fireground;
        private bool _rollCallActive = false;
        private WPFCommon.ThreadSafeObservableCollection<RollCallItem> _rollCallParticipants = new WPFCommon.ThreadSafeObservableCollection<RollCallItem>();
        private WPFCommon.ThreadSafeObservableCollection<RollCallItem> _rollCallGood = new WPFCommon.ThreadSafeObservableCollection<RollCallItem>();
        private WPFCommon.ThreadSafeObservableCollection<RollCallItem> _rollCallWaiting = new WPFCommon.ThreadSafeObservableCollection<RollCallItem>();

        public WPFCommon.ThreadSafeObservableCollection<RollCallItem> WaitingList { get { return _rollCallWaiting; } }
        public WPFCommon.ThreadSafeObservableCollection<RollCallItem> GoodList { get { return _rollCallGood; } }

        public bool RollCallActive
        {
            get { return _rollCallActive; }
            set
            {
                if (_rollCallActive != value)
                {
                    _rollCallActive = value;
                    RollCallItem[] rciArray = _rollCallGood.ToArray();
                    foreach (RollCallItem rci in rciArray)
                    {
                        AddToWaitingList(rci);
                    }
                }
            }
        }

        public void ClearRollCallParticipants()
        {
            _rollCallParticipants.Clear();
            _rollCallGood.Clear();
            _rollCallWaiting.Clear();
        }
        public void ClearAllEmergencies()
        {
            RollCallItem[] emergencyCalls = _rollCallParticipants.Where(rci => rci.EmergencyState == EmergencyState.EmergencyActive).ToArray();
            if (emergencyCalls != null)
            {
                foreach (RollCallItem rci in emergencyCalls)
                {
                    rci.EmergencyState = EmergencyState.EmergencyCleared;
                    //broadcast it...
                }
            }
        }
        public void DoClearAll()
        {
            _rollCallGood.Clear();
            _rollCallWaiting.Clear();
            ClearRollCallParticipants();
        }
        public RollCallItem GetItemFromLookupKey(string lookupKey)
        {
            return _rollCallParticipants.FirstOrDefault(rci => rci.SignalingLookupKey == lookupKey);
        }

        private void AddToList(RollCallItem rci, ICollection<RollCallItem> rciList)
        {
            if (!_firegroundEnabled)
                return;
            if (rci == null || rciList == null)
                return;
            if (rciList.FirstOrDefault(r => r.SignalingLookupKey == rci.SignalingLookupKey) == null)
            {
                rciList.Add(rci);
            }
        }
        private void RemoveFromList(RollCallItem rci, ICollection<RollCallItem> rciList)
        {
            if (!_firegroundEnabled)
                return;
            if (rci == null || rciList == null)
                return;
            RollCallItem[] rciArray = rciList.Where(r => r.SignalingLookupKey == rci.SignalingLookupKey).ToArray();
            if (rciArray != null && rciArray.Length > 0)
            {
                foreach (RollCallItem r in rciArray)
                {
                    rciList.Remove(r);
                }
            }
        }

        public void AddToParticipantsList(RollCallItem rci)
        {
            AddToList(rci, _rollCallParticipants);
        }
        public void AddToGoodList(RollCallItem rci)
        {
            AddToParticipantsList(rci);
            RemoveFromList(rci, _rollCallWaiting);
            AddToList(rci, _rollCallGood);
        }
        public void AddToWaitingList(RollCallItem rci)
        {
            AddToParticipantsList(rci);
            RemoveFromList(rci, _rollCallGood);
            AddToList(rci, _rollCallWaiting);
        }

        public bool ParticipantsExist { get { return _rollCallGood.Count > 0 || _rollCallWaiting.Count > 0; } }

        public void RemoveFromRollCall(RollCallItem rci)
        {
            RemoveFromList(rci, _rollCallParticipants);
            RemoveFromList(rci, _rollCallGood);
            RemoveFromList(rci, _rollCallWaiting);
        }

        public void HandleRadioSignal(RadioSignalingItem sigItem, EmergencyState emerState= EmergencyState.NonEmergency)
        {
            if (!_firegroundEnabled)
                return;
            if (sigItem == null)
                return;
            RadioInfo rInfo = RadioInfoLookupHelper.Instance.GetRadioInfoFromSignalingItem(sigItem);

            if (rInfo == null || rInfo.ExcludeFromRollCall)
                return;

            RollCallItem rci = GetItemFromLookupKey(rInfo.SignalingLookupKey);
            if (rci == null)
            {
                rci = new RollCallItem(rInfo, emerState);
            }
            else
            {
                rci.EmergencyState = emerState;
            }
            if (_rollCallActive)
            {
                AddToGoodList(rci);
            }
            else
            {
                AddToWaitingList(rci);
            }
        }
        public void HandleRadioCall(RadioSignalItemModel rsim)
        {
            if (!_firegroundEnabled)
                return;
            if (rsim == null || rsim.RawSignalItem == null)
                return;
            HandleRadioSignal(rsim.RawSignalItem, rsim.EmergencyState);
        }

        public void HandleRadioInfoUpdated(string lookupKey)
        {
            RollCallItem rci = GetItemFromLookupKey(lookupKey);
            if (rci != null)
            {
                rci.RaiseDisplayInfoChanged();
            }
        }

        public void DoRestartRollCall()
        {
            if(!_firegroundEnabled)
            {
                DoClearAll();
                return;
            }
            RollCallItem[] callList = GoodList.ToArray();
            foreach (RollCallItem rci in callList)
            {
                AddToWaitingList(rci);
            }
        }

        public void ClearRollCall()
        {
            if(MessageBoxHelper.Show("Are you sure you want to remove all radios from roll call?", "Clear Roll Call", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No)== System.Windows.MessageBoxResult.Yes)
            {
                DoClearAll();
            }
        }
    }
}

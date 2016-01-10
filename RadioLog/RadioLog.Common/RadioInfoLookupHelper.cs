using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace RadioLog.Common
{
    public class RadioInfoLookupHelper:BaseXmlLoader
    {
        private bool _changesMade = false;
        private bool _autoSaveContacts = false;
        private bool _firegroundMode = AppSettings.Instance.WorkstationMode == RadioLogMode.Fireground;
        private Dictionary<Guid, AgencyInfo> _agencies = new Dictionary<Guid, AgencyInfo>();
        private Dictionary<Guid, UnitInfo> _units = new Dictionary<Guid, UnitInfo>();
        private Dictionary<string, RadioInfo> _radios = new Dictionary<string, RadioInfo>();

        private static RadioInfoLookupHelper _instance = null;
        public static RadioInfoLookupHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RadioInfoLookupHelper();
                }
                return _instance;
            }
        }

        private RadioInfoLookupHelper()
        {
            LoadInfo();
        }

        public bool ChangesMade { get { return _changesMade; } }
        public void MarkChangesMade() { _changesMade = true; }
        public void ClearChangesMade() { _changesMade = false; }
        
        public void LoadInfo()
        {
            UpdateFromAppSettings();
            _agencies.Clear();
            _units.Clear();
            _radios.Clear();
            LoadFromXMLFile(AppSettings.Instance.RadioInfoSettingsFileName);
        }
        public void SaveInfo()
        {
            if (!_changesMade)
                return;
            SaveToXMLFile(AppSettings.Instance.RadioInfoSettingsFileName);
        }
        public void ReloadInfo()
        {
            if(ChangesMade)
            {
                SaveInfo();
            }
            LoadInfo();
        }

        public RadioInfo GetRadioInfoFromSignalingItem(RadioSignalingItem sigItem)
        {
            if (sigItem == null || string.IsNullOrWhiteSpace(sigItem.UnitId) || sigItem.SourceType == SignalingSourceType.StreamingTag)
                return null;
            string radioLookupKey = RadioInfo.GenerateLookupKey(sigItem.SignalingFormat, sigItem.UnitId);
            if (_radios.ContainsKey(radioLookupKey))
                return _radios[radioLookupKey];
            return null;
        }

        public void PerformLookupsOnRadioSignalItem(RadioSignalingItem sigItem)
        {
            if (sigItem == null || string.IsNullOrWhiteSpace(sigItem.UnitId) || sigItem.SourceType == SignalingSourceType.StreamingTag)
                return;
            RadioInfo rInfo = GetRadioInfoFromSignalingItem(sigItem);
            if (rInfo == null && _autoSaveContacts)
            {
                //should we save it?
                rInfo = new RadioInfo(Guid.Empty, Guid.Empty, sigItem.SignalingFormat, sigItem.UnitId, sigItem.UnitId, string.Empty, string.Empty, RadioTypeCode.Unknown, sigItem.SourceName, false);
                _radios[rInfo.SignalingLookupKey] = rInfo;
                MarkChangesMade();
            }
            if (rInfo != null)
            {
                sigItem.AssignedPersonnel = rInfo.PersonnelName;
                sigItem.AssignedRole = rInfo.RoleName;
                sigItem.RadioName = rInfo.RadioName;
                sigItem.RadioType = rInfo.RadioType;
                bool bAgencySet = false;
                if (rInfo.UnitKeyId != Guid.Empty && _units.ContainsKey(rInfo.UnitKeyId))
                {
                    UnitInfo uInfo = _units[rInfo.UnitKeyId];
                    sigItem.UnitName = uInfo.UnitName;
                    if (uInfo.AgencyKeyId != Guid.Empty && _agencies.ContainsKey(uInfo.AgencyKeyId))
                    {
                        sigItem.AgencyName = _agencies[uInfo.AgencyKeyId].AgencyName;
                        bAgencySet = true;
                    }
                }
                if (!bAgencySet && rInfo.AgencyKeyId != Guid.Empty)
                {
                    if (_agencies.ContainsKey(rInfo.AgencyKeyId))
                    {
                        sigItem.AgencyName = _agencies[rInfo.AgencyKeyId].AgencyName;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(sigItem.UnitName)) { sigItem.UnitName = sigItem.UnitId; }
            if (string.IsNullOrWhiteSpace(sigItem.RadioName)) { sigItem.RadioName = sigItem.UnitId; }
        }

        public override string RootElementName { get { return "RadioInfoConfig"; } }

        public void UpdateFromAppSettings()
        {
            _autoSaveContacts = AppSettings.Instance.ShouldAutoSaveContacts;
            _firegroundMode = AppSettings.Instance.WorkstationMode == RadioLogMode.Fireground;
        }

        internal bool LoadInfoFromNode(System.Xml.XmlNode xNode)
        {
            bool bChangesMade = false;
            switch (xNode.Name.ToLower())
            {
                case "agencies":
                    {
                        foreach (XmlNode xAgency in xNode.ChildNodes)
                        {
                            Guid agencyId = GetGuidAttribute(xAgency.Attributes["AgencyId"]);
                            if (agencyId == Guid.Empty)
                            {
                                agencyId = Guid.NewGuid();
                                bChangesMade = true;
                            }
                            string agencyName = GetAttributeText(xAgency.Attributes["AgencyName"]);
                            _agencies[agencyId] = new AgencyInfo(agencyId, agencyName);
                        }
                        break;
                    }
                case "units":
                    {
                        foreach (XmlNode xUnit in xNode.ChildNodes)
                        {
                            Guid unitKeyId = GetGuidAttribute(xUnit.Attributes["UnitKeyId"]);
                            if (unitKeyId == Guid.Empty)
                            {
                                unitKeyId = Guid.NewGuid();
                                bChangesMade = true;
                            }
                            Guid agencyId = GetGuidAttribute(xUnit.Attributes["AgencyId"]);
                            string unitName = GetAttributeText(xUnit.Attributes["UnitName"]);
                            _units[unitKeyId] = new UnitInfo(unitKeyId, agencyId, unitName);
                        }
                        break;
                    }
                case "radios":
                    {
                        foreach (XmlNode xRadio in xNode.ChildNodes)
                        {
                            Guid agencyId = GetGuidAttribute(xRadio.Attributes["AgencyId"]);
                            Guid unitKeyId = GetGuidAttribute(xRadio.Attributes["UnitKeyId"]);
                            string signalingFormat = GetAttributeText(xRadio.Attributes["SignalingFormat"]);
                            string signalingUnitId = GetAttributeText(xRadio.Attributes["SignalingUnitId"]);
                            string radioName = GetAttributeText(xRadio.Attributes["RadioName"]);
                            string roleName = GetAttributeText(xRadio.Attributes["RoleName"]);
                            string personnelName = GetAttributeText(xRadio.Attributes["PersonnelName"]);
                            RadioTypeCode radioType = GetEnumAttribute<RadioTypeCode>(xRadio.Attributes["RadioType"], RadioTypeCode.Unknown);
                            string firstHeardSource = GetAttributeText(xRadio.Attributes["FirstHeardSource"]);
                            bool excludeFromRollcall = GetBoolAttribute(xRadio.Attributes["ExcludeFromRollcall"]);
                            RadioInfo radioInfo = new RadioInfo(unitKeyId, agencyId, signalingFormat, signalingUnitId, radioName, roleName, personnelName, radioType, firstHeardSource, excludeFromRollcall);
                            _radios[radioInfo.SignalingLookupKey] = radioInfo;
                        }
                        break;
                    }
            }
            return bChangesMade;
        }

        public override bool LoadXml(System.Xml.XmlDocument xDoc)
        {
            bool bChangesMade = false;
            foreach(XmlNode xNode in xDoc.DocumentElement.ChildNodes)
            {
                bChangesMade |= LoadInfoFromNode(xNode);
            }

            _changesMade |= bChangesMade;
            return bChangesMade;
        }

        public override void SaveXml(System.Xml.XmlDocument xDoc)
        {
            XmlNode xAgencies = xDoc.CreateElement("Agencies");
            xDoc.DocumentElement.AppendChild(xAgencies);
            foreach (KeyValuePair<Guid, AgencyInfo> kvpA in _agencies)
            {
                XmlElement elemAgency = xDoc.CreateElement("Agency");
                SetGuidAttribute(elemAgency, "AgencyId", kvpA.Value.AgencyId);
                SetTextAttribute(elemAgency, "AgencyName", kvpA.Value.AgencyName);
                xAgencies.AppendChild(elemAgency);
            }

            XmlNode xUnits = xDoc.CreateElement("Units");
            xDoc.DocumentElement.AppendChild(xUnits);
            foreach (KeyValuePair<Guid, UnitInfo> kvpU in _units)
            {
                XmlElement elemUnit = xDoc.CreateElement("Unit");
                SetGuidAttribute(elemUnit, "UnitKeyId", kvpU.Value.UnitKeyId);
                SetNonEmptyGuidAttribute(elemUnit, "AgencyId", kvpU.Value.AgencyKeyId);
                SetTextAttribute(elemUnit, "UnitName", kvpU.Value.UnitName);
                xUnits.AppendChild(elemUnit);
            }

            XmlNode xRadios = xDoc.CreateElement("Radios");
            xDoc.DocumentElement.AppendChild(xRadios);
            foreach (KeyValuePair<string, RadioInfo> kvpR in _radios)
            {
                XmlElement elemRadio = xDoc.CreateElement("Radio");
                SetNonEmptyGuidAttribute(elemRadio, "AgencyId", kvpR.Value.AgencyKeyId);
                SetNonEmptyGuidAttribute(elemRadio, "UnitKeyId", kvpR.Value.UnitKeyId);
                SetTextAttribute(elemRadio, "SignalingFormat", kvpR.Value.SignalingFormat);
                SetTextAttribute(elemRadio, "SignalingUnitId", kvpR.Value.SignalingUnitId);
                SetTextAttribute(elemRadio, "RadioName", kvpR.Value.RadioName);
                SetTextAttribute(elemRadio, "RoleName", kvpR.Value.RoleName);
                SetTextAttribute(elemRadio, "PersonnelName", kvpR.Value.PersonnelName);
                SetEnumAttribute(elemRadio, "RadioType", kvpR.Value.RadioType);
                SetTextAttribute(elemRadio, "FirstHeardSource", kvpR.Value.FirstHeardSourceName);
                SetBoolAttribute(elemRadio, "ExcludeFromRollcall", kvpR.Value.ExcludeFromRollCall);
                xRadios.AppendChild(elemRadio);
            }

            ClearChangesMade();
        }

        public IEnumerable<AgencyInfo> AgencyList { get { return _agencies.Values; } }
        public IEnumerable<UnitInfo> UnitList { get { return _units.Values; } }
        public IEnumerable<RadioInfo> RadioList { get { return _radios.Values; } }

        public Guid GetAgencyIdForUnitId(Guid unitId)
        {
            if (unitId == Guid.Empty || !_units.ContainsKey(unitId))
                return Guid.Empty;
            return _units[unitId].AgencyKeyId;
        }
        public void AddOrUpdateAgency(Guid agencyId, string agencyName)
        {
            if (string.IsNullOrWhiteSpace(agencyName))
                return;
            if (agencyId == Guid.Empty)
                agencyId = Guid.NewGuid();
            if (_agencies.ContainsKey(agencyId))
            {
                _agencies[agencyId].AgencyName = agencyName;
            }
            else
            {
                _agencies[agencyId] = new AgencyInfo(agencyId, agencyName);
            }
            MarkChangesMade();
        }
        public void AddOrUpdateUnit(Guid unitId, Guid agencyId, string unitName)
        {
            if (string.IsNullOrWhiteSpace(unitName))
                return;
            if (unitId == Guid.Empty)
                unitId = Guid.NewGuid();
            if (_units.ContainsKey(unitId))
            {
                _units[unitId].AgencyKeyId = agencyId;
                _units[unitId].UnitName = unitName;
            }
            else
            {
                _units[unitId] = new UnitInfo(unitId, agencyId, unitName);
            }
            MarkChangesMade();
        }
        public void AddOrUpdateRadio(Guid unitKeyId, Guid agencyKeyId, string signalingFormat, string signalingUnitId, string radioName, string roleName, string personnelName, RadioTypeCode radioType, bool excludeFromRollCall)
        {
            if (string.IsNullOrWhiteSpace(signalingUnitId) || string.IsNullOrWhiteSpace(signalingFormat))
                return;
            string strKey = RadioInfo.GenerateLookupKey(signalingFormat, signalingUnitId);
            if (_radios.ContainsKey(strKey))
            {
                _radios[strKey].UnitKeyId = unitKeyId;
                _radios[strKey].AgencyKeyId = agencyKeyId;
                _radios[strKey].SignalingFormat = signalingFormat;
                _radios[strKey].SignalingUnitId = signalingUnitId;
                _radios[strKey].RadioName = radioName;
                _radios[strKey].RoleName = roleName;
                _radios[strKey].PersonnelName = personnelName;
                _radios[strKey].RadioType = radioType;
                _radios[strKey].ExcludeFromRollCall = excludeFromRollCall;
            }
            else
            {
                _radios[strKey] = new RadioInfo(unitKeyId, agencyKeyId, signalingFormat, signalingUnitId, radioName, roleName, personnelName, radioType, "MANUAL ENTRY", excludeFromRollCall);
            }
            MarkChangesMade();
        }
    }

    public class AgencyInfo
    {
        public Guid AgencyId { get; private set; }
        public string AgencyName { get; set; }

        public AgencyInfo(Guid agencyId, string agencyName)
        {
            this.AgencyId = agencyId;
            this.AgencyName = agencyName;
        }
    }

    public class UnitInfo
    {
        public Guid UnitKeyId { get; private set; }
        public Guid AgencyKeyId { get; set; }
        public string UnitName { get; set; }

        public UnitInfo(Guid unitKeyId, Guid agencyKeyId, string unitName)
        {
            this.UnitKeyId = unitKeyId;
            this.AgencyKeyId = agencyKeyId;
            this.UnitName = unitName;
        }
    }

    public class RadioInfo
    {
        public Guid UnitKeyId { get; set; }
        public Guid AgencyKeyId { get; set; }
        public string SignalingFormat { get; set; }
        public string SignalingUnitId { get; set; }
        public string RadioName { get; set; }
        public string RoleName { get; set; }
        public string PersonnelName { get; set; }
        public RadioTypeCode RadioType { get; set; }
        public string FirstHeardSourceName { get; set; }
        public bool ExcludeFromRollCall { get; set; }

        public static string GenerateLookupKey(string fmt, string id)
        {
            if (string.IsNullOrWhiteSpace(fmt) || string.IsNullOrWhiteSpace(id))
                return string.Empty;
            return string.Format("{0}|{1}", fmt, id).ToUpper();
        }

        public string SignalingLookupKey { get { return GenerateLookupKey(SignalingFormat, SignalingUnitId); } }

        public RadioInfo(Guid unitKeyId, Guid agencyKeyId, string signalingFormat, string signalingUnitId, string radioName, string roleName, string personnelName, RadioTypeCode radioType, string firstHeardSourceName, bool excludeFromRollcall)
        {
            this.UnitKeyId = unitKeyId;
            this.AgencyKeyId = agencyKeyId;
            this.SignalingFormat = signalingFormat;
            this.SignalingUnitId = signalingUnitId;
            this.RadioName = radioName;
            this.RoleName = roleName;
            this.PersonnelName = personnelName;
            this.RadioType = radioType;
            this.FirstHeardSourceName = firstHeardSourceName;
            this.ExcludeFromRollCall = excludeFromRollcall;
        }

        public string DisplayName
        {
            get
            {
                string rslt = DisplayFormatterUtils.MakeDisplayStringFromParts(" - ", PersonnelName, RoleName, RadioName);
                if (string.IsNullOrWhiteSpace(rslt))
                    return DisplayFormatterUtils.MakeDisplayStringFromParts(" - ", SignalingFormat, SignalingUnitId);
                else
                    return rslt;
            }
        }
    }
}

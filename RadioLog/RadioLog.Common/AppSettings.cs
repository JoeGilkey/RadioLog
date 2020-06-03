using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RadioLog.Common
{
    public class AppSettings : IDisposable, INotifyPropertyChanged
    {
        public const int MAX_MULTI_STREAM_CHANNELS = 8;

        public static bool HasSaveError { get; private set; }

        private static object _loadSaveLock = new object();

        #region Static Stuff
        private static AppSettings _settings = null;
        public static AppSettings Instance
        {
            get
            {
                if (_settings == null)
                    _settings = new AppSettings();
                return _settings;
            }
        }
        public static void ResetCacheAfterConfig() { _settings = null; }
        #endregion
        #region Internal Stuff
        const string DefaultSettingFileName = "RadioLog.settings";
        public const string DefaultLocalDataSubDir = "RadioLog";

        private string _defaultUpdateFileName = string.Empty;
        private Dictionary<string, Dictionary<string, string>> _values = new Dictionary<string, Dictionary<string, string>>();
        private List<SignalSource> _signalSources = new List<SignalSource>();
        private List<SignalGroup> _signalGroups = new List<SignalGroup>();
        private bool _isEditing = false;
        private string _appRootDir = string.Empty;
        private string _appDataDir = string.Empty;
        private string _appDocumentsDir = string.Empty;

        public string AppRootDir
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_appRootDir))
                {
                    _appRootDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(System.Reflection.Assembly.GetEntryAssembly().Location));
                }
                return _appRootDir;
            }
        }
        public string AppDataDir
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_appDataDir))
                {
                    string commonDataFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    _appDataDir = System.IO.Path.Combine(commonDataFolder, DefaultLocalDataSubDir);
                    DirectoryHelper.SetupDirectory(_appDataDir);
                }
                return _appDataDir;
            }
        }
        public string AppRecordingDir
        {
            get { return AppDocumentsDir; }
        }
        public string AppDocumentsDir
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_appDocumentsDir))
                {
                    string commonDocumentsFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                    _appDocumentsDir = System.IO.Path.Combine(commonDocumentsFolder, "RadioLogs");
                    DirectoryHelper.SetupDirectory(_appDocumentsDir);
                }
                return _appDocumentsDir;
            }
        }
        #endregion

        public void MarkModified() { this.IsEditing = true; }

        public bool IsEditing
        {
            get { return _isEditing; }
            private set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged("IsEditing");
                }
            }
        }

        ~AppSettings() { Dispose(); }
        private AppSettings()
        {
            HasSaveError = false;
            LoadSettingsFile();
        }
        public void Startup() { } //jg Dummy routine to start the loading process...
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        private void ValueEdited(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return;
            OnPropertyChanged(propertyName);
            IsEditing = true;
        }
        #endregion
        #region IDisposable Members
        private bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed)
            {
                SaveSettingsFile();
                foreach (KeyValuePair<string, Dictionary<string, string>> kvp in _values)
                    kvp.Value.Clear();
                _values.Clear();
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion
        #region Read Methods
        public string ReadString(string sectionName, string valueName, string defaultValue, bool setDefault = false)
        {
            if (string.IsNullOrWhiteSpace(sectionName) || string.IsNullOrWhiteSpace(valueName))
            {
                if (setDefault)
                    WriteString(sectionName, valueName, defaultValue, valueName);
                return defaultValue;
            }
            if (!_values.ContainsKey(sectionName) || !_values[sectionName].ContainsKey(valueName))
            {
                if (setDefault)
                    WriteString(sectionName, valueName, defaultValue, valueName);
                return defaultValue;
            }
            return
                _values[sectionName][valueName];
        }
        public string ReadString(string sectionName, string valueName) { return ReadString(sectionName, valueName, string.Empty); }
        public T ReadEnum<T>(string sectionName, string valueName, T defaultValue) where T : struct,IConvertible
        {
            string strValue = ReadString(sectionName, valueName);
            if (string.IsNullOrEmpty(strValue))
                return defaultValue;
            try
            {
                return (T)Enum.Parse(typeof(T), strValue);
            }
            catch { return defaultValue; }
        }
        public int? ReadNullableInt32(string sectionName, string valueName)
        {
            string s = ReadString(sectionName, valueName);
            if (string.IsNullOrEmpty(s))
                return null;
            else
            {
                int tmp;
                if (int.TryParse(s, out tmp))
                    return tmp;
                else
                    return null;
            }
        }
        public int ReadInt32(string sectionName, string valueName, int defaultValue, bool setDefault = false)
        {
            string s = ReadString(sectionName, valueName);
            if (string.IsNullOrEmpty(s))
            {
                if (setDefault)
                    WriteInt32(sectionName, valueName, defaultValue, valueName);
                return defaultValue;
            }
            else
            {
                int rslt = defaultValue;
                if (int.TryParse(s, out rslt))
                    return rslt;
                else
                {
                    if (setDefault)
                        WriteInt32(sectionName, valueName, defaultValue, valueName);
                    return defaultValue;
                }
            }
        }
        public int ReadInt32(string sectionName, string valueName) { return ReadInt32(sectionName, valueName, 0); }
        public long ReadInt64(string sectionName, string valueName, long defaultValue)
        {
            string s = ReadString(sectionName, valueName);
            if (string.IsNullOrEmpty(s))
                return defaultValue;
            else
            {
                long rslt = defaultValue;
                if (long.TryParse(s, out rslt))
                    return rslt;
                else
                    return defaultValue;
            }
        }
        public long ReadInt64(string sectionName, string valueName) { return ReadInt64(sectionName, valueName, 0); }
        public bool ReadBool(string sectionName, string valueName, bool defaultValue, bool setDefault = false)
        {
            string s = ReadString(sectionName, valueName);
            if (string.IsNullOrEmpty(s))
            {
                if (setDefault)
                    WriteBool(sectionName, valueName, defaultValue, valueName);
                return defaultValue;
            }
            else
            {
                bool rslt = defaultValue;
                if (bool.TryParse(s, out rslt))
                    return rslt;
                else
                {
                    if (setDefault)
                        WriteBool(sectionName, valueName, defaultValue, valueName);
                    return defaultValue;
                }
            }
        }
        public Guid ReadGuid(string sectionName, string valueName, Guid defaultValue)
        {
            string s = ReadString(sectionName, valueName);
            if (string.IsNullOrEmpty(s))
                return defaultValue;
            else
            {
                try { return new Guid(s); }
                catch { return defaultValue; }
            }
        }
        public Guid ReadGuid(string sectionName, string valueName) { return ReadGuid(sectionName, valueName, Guid.Empty); }
        public Guid? ReadNullableGuid(string sectionName, string valueName)
        {
            string s = ReadString(sectionName, valueName);
            if (string.IsNullOrEmpty(s))
                return null;
            else
            {
                Guid g;
                if (Guid.TryParse(s, out g))
                    return g;
                else
                    return null;
            }
        }
        public double? ReadNullableDouble(string sectionName, string valueName)
        {
            string s = ReadString(sectionName, valueName);
            if (string.IsNullOrEmpty(s))
                return null;
            else
            {
                double d;
                if (double.TryParse(s, out d))
                    return d;
                else
                    return null;
            }
        }
        public double ReadDouble(string sectionName, string valueName, double defaultValue)
        {
            string s = ReadString(sectionName, valueName);
            if (string.IsNullOrEmpty(s))
                return defaultValue;
            else
            {
                double d = defaultValue;
                if (double.TryParse(s, out d))
                    return d;
                else
                    return defaultValue;
            }
        }
        public double ReadDouble(string sectionName, string valueName) { return ReadDouble(sectionName, valueName, 0.0d); }
        public double ReadDoubleWithMinumum(string sectionName, string valueName, double defaultValue, double minValue)
        {
            double d = ReadDouble(sectionName, valueName, defaultValue);
            if (d < minValue)
                return defaultValue;
            else
                return d;
        }
        public DateTime? ReadNullableDateTime(string sectionName, string valueName)
        {
            string s = ReadString(sectionName, valueName);
            if (string.IsNullOrEmpty(s))
                return null;
            DateTime dt;
            if (DateTime.TryParse(s, out dt))
                return dt;
            else
                return null;
        }
        #endregion
        #region Write Methods
        public void WriteString(string sectionName, string valueName, string value, string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(sectionName) || string.IsNullOrWhiteSpace(valueName))
                return;
            if (!_values.ContainsKey(sectionName))
            {
                _values.Add(sectionName, new Dictionary<string, string>());
            }
            if (!_values[sectionName].ContainsKey(valueName))
                _values[sectionName].Add(valueName, value);
            else
                _values[sectionName][valueName] = value;
            ValueEdited(propertyName);
        }
        public void WriteEnum<T>(string sectionName, string valueName, T value, string propertyName = null) where T : struct,IConvertible
        {
            WriteString(sectionName, valueName, value.ToString(), propertyName);
        }

        public void WriteInt32(string sectionName, string valueName, int value, string propertyName = null) { WriteString(sectionName, valueName, value.ToString(), propertyName); }
        public void WriteNullableInt32(string sectionName, string valueName, int? value, string propertyName)
        {
            if (value.HasValue)
            {
                WriteString(sectionName, valueName, value.Value.ToString(), propertyName);
            }
            else
            {
                WriteString(sectionName, valueName, string.Empty, propertyName);
            }
        }
        public void WriteInt64(string sectionName, string valueName, long value, string propertyName) { WriteString(sectionName, valueName, value.ToString(), propertyName); }
        public void WriteBool(string sectionName, string valueName, bool value, string propertyName = null) { WriteString(sectionName, valueName, value.ToString(), propertyName); }
        public void WriteGuid(string sectionName, string valueName, Guid value, string propertyName) { WriteString(sectionName, valueName, value.ToString(), propertyName); }
        public void WriteDouble(string sectionName, string valueName, double value, string propertyName) { WriteString(sectionName, valueName, value.ToString(), propertyName); }
        public void WriteDoubleWithMinimum(string sectionName, string valueName, double value, double minValue, string propertyName) { WriteString(sectionName, valueName, Math.Max(value, minValue).ToString(), propertyName); }
        public void WriteNullableDateTime(string sectionName, string valueName, DateTime? value, string propertyName)
        {
            if (value.HasValue)
            {
                WriteString(sectionName, valueName, value.Value.ToString(), propertyName);
            }
            else
            {
                WriteString(sectionName, valueName, string.Empty, propertyName);
            }
        }
        public void WriteNullableGuid(string sectionName, string valueName, Guid? value, string propertyName)
        {
            if (value.HasValue)
                WriteGuid(sectionName, valueName, value.Value, propertyName);
            else
                WriteString(sectionName, valueName, string.Empty, propertyName);
        }
        public void WriteNullableDouble(string sectionName, string valueName, double? value, string propertyName)
        {
            if (value.HasValue)
                WriteDouble(sectionName, valueName, value.Value, propertyName);
            else
                WriteString(sectionName, valueName, string.Empty, propertyName);
        }
        #endregion
        #region File Methods
#if USE_ISOLATED_STORAGE
        private Stream GetLoadSettingsFileStream()
        {
            System.IO.IsolatedStorage.IsolatedStorageFile file = System.IO.IsolatedStorage.IsolatedStorageFile.GetMachineStoreForAssembly();
            return file.OpenFile(DefaultSettingFileName, FileMode.OpenOrCreate, FileAccess.Read);
        }
        private Stream GetSaveSettingsFileStream()
        {
            if (DoesSettingsFileExist())
                System.IO.IsolatedStorage.IsolatedStorageFile.GetMachineStoreForAssembly().DeleteFile(DefaultSettingFileName);
            return System.IO.IsolatedStorage.IsolatedStorageFile.GetMachineStoreForAssembly().CreateFile(DefaultSettingFileName);
        }
        private bool DoesSettingsFileExist()
        {
            return System.IO.IsolatedStorage.IsolatedStorageFile.GetMachineStoreForAssembly().FileExists(DefaultSettingFileName);
        }
#else
        private string SettingsFileName() { return System.IO.Path.Combine(AppDataDir, DefaultSettingFileName); }
        private Stream GetLoadSettingsFileStream()
        {
            return System.IO.File.OpenRead(this.SettingsFileName());
        }
        private Stream GetSaveSettingsFileStream()
        {
            string strFile = SettingsFileName();
            if (System.IO.File.Exists(strFile))
                System.IO.File.Delete(strFile);
            return System.IO.File.OpenWrite(strFile);
        }
        private bool DoesSettingsFileExist() { return System.IO.File.Exists(SettingsFileName()); }
#endif
        public void LoadSettingsFile()
        {
            IsEditing = false;
            if (DoesSettingsFileExist())
            {
                lock (_loadSaveLock)
                {
                    XmlDocument xDoc = new XmlDocument();
                    try
                    {
                        Stream fs = GetLoadSettingsFileStream();
                        xDoc.Load(fs);
                        fs.Close();
                        PrepareXML(xDoc);
                    }
                    catch
                    {
                        xDoc = new XmlDocument();
                        PrepareXML(xDoc);
                    }
                    IsEditing = LoadXML(xDoc);
                }
            }
            if (IsEditing)
            {
                SaveSettingsFile();
            }
        }
        private void PrepareXML(XmlDocument xDoc)
        {
            if (xDoc.DocumentElement == null)
            {
                XmlNode xn = xDoc.CreateElement("RadioLogConfig");
                xDoc.AppendChild(xn);
            }
        }

        private string GetAttributeText(XmlAttribute attrib)
        {
            if (attrib == null)
                return string.Empty;
            else
                return attrib.Value;
        }
        private Guid GetGuidAttributeText(XmlAttribute attrib)
        {
            string strGuid = GetAttributeText(attrib);
            if (string.IsNullOrEmpty(strGuid))
                return Guid.Empty;
            Guid g;
            if (Guid.TryParse(strGuid, out g))
                return g;
            else
                return Guid.Empty;
        }
        private int GetIntAttributeText(XmlAttribute attrib, int defaultVal = 0)
        {
            string strInt = GetAttributeText(attrib);
            if (string.IsNullOrEmpty(strInt))
                return defaultVal;
            int i;
            if (int.TryParse(strInt, out i))
                return i;
            else
                return defaultVal;
        }
        private bool GetBoolAttributeText(XmlAttribute attrib, bool defaultVal = false)
        {
            string strBool = GetAttributeText(attrib);
            if (string.IsNullOrEmpty(strBool))
                return defaultVal;
            bool b;
            if (bool.TryParse(strBool, out b))
                return b;
            else
                return defaultVal;
        }
        private T GetEnumAttribute<T>(XmlAttribute attrib, T defaultValue) where T : struct,IConvertible
        {
            string strValue = GetAttributeText(attrib);
            if (string.IsNullOrWhiteSpace(strValue))
                return defaultValue;
            try
            {
                T rslt = defaultValue;
                if (Enum.TryParse<T>(strValue, true, out rslt))
                    return rslt;
                else
                    return defaultValue;
            }
            catch { return defaultValue; }
        }
        private XmlAttribute CreateTextAttribute(XmlDocument xDoc, string attribName, string attribValue)
        {
            XmlAttribute x = xDoc.CreateAttribute(attribName);
            x.Value = attribValue;
            return x;
        }
        private XmlAttribute CreateEnumAttribute<T>(XmlDocument xDoc, string attribName, T attribValue) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum) return null;
            return CreateTextAttribute(xDoc, attribName, attribValue.ToString());
        }

        internal bool LoadSourcesFromXml(XmlNode xSection, bool bFromImport)
        {
            bool bChangesMade = false;
            foreach (XmlNode n in xSection.ChildNodes)
            {
                if (string.Equals(n.Name, "Source", StringComparison.InvariantCultureIgnoreCase))
                {
                    Guid id = GetGuidAttributeText(n.Attributes["SourceId"]);
                    if (id == Guid.Empty)
                    {
                        id = Guid.NewGuid();
                        bChangesMade = true;
                    }
                    string sourceName = GetAttributeText(n.Attributes["SourceName"]);
                    string sourceLoc = GetAttributeText(n.Attributes["SourceLocation"]);
                    string sourceColor = GetAttributeText(n.Attributes["SourceColor"]);
                    int sourceChan = GetIntAttributeText(n.Attributes["SourceChannel"], 0);
                    SignalingSourceType sourceType = GetEnumAttribute<SignalingSourceType>(n.Attributes["SourceType"], SignalingSourceType.Unknown);
                    if (sourceType == SignalingSourceType.Unknown)
                        continue;
                    if (bFromImport && _signalSources.FirstOrDefault(s => s.SourceId == id || s.SourceName == sourceName || (s.SourceType == sourceType && s.SourceLocation == sourceLoc)) != null)
                        continue;
                    bool isEnabled = GetBoolAttributeText(n.Attributes["IsEnabled"], true);
                    bool isPriority = GetBoolAttributeText(n.Attributes["IsPriority"], false);
                    bool record = GetBoolAttributeText(n.Attributes["RecordAudio"], false);
                    int recordKickTime = GetIntAttributeText(n.Attributes["RecordingKickTime"], 15);
                    int volume = GetIntAttributeText(n.Attributes["Volume"], 50);
                    int maxVolume = GetIntAttributeText(n.Attributes["MaxVolume"], 0);
                    if (maxVolume <= 0)
                    {
                        maxVolume = 100;
                        volume = volume * 10;
                    }
                    volume = Math.Max(0, Math.Min(maxVolume, volume));
                    NoiseFloor noiseFloor = GetEnumAttribute<NoiseFloor>(n.Attributes["NoiseFloor"], NoiseFloor.Normal);
                    int customNoiseFloor = GetIntAttributeText(n.Attributes["CustomNoiseFloor"], 10);
                    bool removeNoise = GetBoolAttributeText(n.Attributes["RemoveNoise"], false);

                    int displayOrder = GetIntAttributeText(n.Attributes["DisplayOrder"], 10000);
                    Guid groupId = GetGuidAttributeText(n.Attributes["GroupId"]);
                    bool dMDC1200 = GetBoolAttributeText(n.Attributes["DecodeMDC1200"]);
                    bool dGEStar = GetBoolAttributeText(n.Attributes["DecodeGEStar"]);
                    bool dFleetSync = GetBoolAttributeText(n.Attributes["DecodeFleetSync"]);
                    //bool dP25 = GetBoolAttributeText(n.Attributes["DecodeP25"]);
                    string waveOutDev = GetAttributeText(n.Attributes["WaveOutDeviceName"]);
                    int waveOutChan = GetIntAttributeText(n.Attributes["WaveOutChannel"]);
                    SignalRecordingType recType = GetEnumAttribute<SignalRecordingType>(n.Attributes["RecordingType"], SignalRecordingType.VOX);
                    SignalSource newSource = new SignalSource()
                    {
                        SourceId = id,
                        SourceName = sourceName,
                        SourceLocation = sourceLoc,
                        SourceColor = sourceColor,
                        SourceType = sourceType,
                        SourceChannel = sourceChan,
                        IsEnabled = isEnabled,
                        IsPriority = isPriority,
                        RecordAudio = record,
                        RecordingKickTime = recordKickTime,
                        RecordingType = recType,
                        Volume = volume,
                        MaxVolume = maxVolume,
                        NoiseFloor = noiseFloor,
                        CustomNoiseFloor = customNoiseFloor,
                        RemoveNoise = removeNoise,
                        ComPortInfo = null,
                        DisplayOrder = displayOrder,
                        GroupId = groupId,
                        DecodeMDC1200 = dMDC1200,
                        DecodeGEStar = dGEStar,
                        DecodeFleetSync = dFleetSync,
                        //DecodeP25 = dP25,
                        WaveOutDeviceName = waveOutDev,
                        WaveOutChannel = waveOutChan
                    };
                    _signalSources.Add(newSource);
                }
            }
            return bChangesMade;
        }
        private void LoadGroupsFromXml(XmlNode xSection, bool bFromImport)
        {
            foreach (XmlNode n in xSection.ChildNodes)
            {
                if (string.Equals(n.Name, "Group", StringComparison.InvariantCultureIgnoreCase))
                {
                    Guid id = GetGuidAttributeText(n.Attributes["GroupId"]);
                    string groupName = GetAttributeText(n.Attributes["GroupName"]);
                    int sortOrder = GetIntAttributeText(n.Attributes["DisplayOrder"], 10000);
                    string colorStr = GetAttributeText(n.Attributes["Color"]);
                    if (!bFromImport || _signalGroups.FirstOrDefault(g => g.GroupId == id) == null)
                    {
                        SignalGroup newGrp = new SignalGroup()
                        {
                            GroupId = id,
                            GroupName = groupName,
                            DisplayOrder = sortOrder,
                            GroupMuted = false,
                            GroupColorString = colorStr
                        };
                        _signalGroups.Add(newGrp);
                    }
                }
            }
        }
        internal void ImportXML(XmlDocument xDoc)
        {
            foreach (XmlNode xSection in xDoc.DocumentElement.ChildNodes)
            {
                if (xSection.Name.Equals("Sources", StringComparison.InvariantCultureIgnoreCase))
                {
                    LoadSourcesFromXml(xSection, true);
                }
                else if (xSection.Name.Equals("Groups", StringComparison.InvariantCultureIgnoreCase))
                {
                    LoadGroupsFromXml(xSection, true);
                }
            }
        }
        private bool LoadXML(XmlDocument xDoc)
        {
            bool bChangesMade = false;

            _signalSources.Clear();
            _signalGroups.Clear();

            foreach (XmlNode xSection in xDoc.DocumentElement.ChildNodes)
            {
                if (xSection.Name.Equals("StreamSources", StringComparison.InvariantCultureIgnoreCase)) //for upgrades only!!!
                {
                    foreach (XmlNode n in xSection.ChildNodes)
                    {
                        if (string.Equals(n.Name, "StreamSource", StringComparison.InvariantCultureIgnoreCase))
                        {
                            bChangesMade = true;
                            Guid id = GetGuidAttributeText(n.Attributes["srcId"]);
                            if (id == Guid.Empty)
                            {
                                id = Guid.NewGuid();
                            }
                            SignalingSourceType srcType = GetEnumAttribute<SignalingSourceType>(n.Attributes["srcType"], SignalingSourceType.Streaming);
                            string srcName = GetAttributeText(n.Attributes["srcName"]);
                            string srcURL = GetAttributeText(n.Attributes["srcURL"]);
                            string srcColor = GetAttributeText(n.Attributes["srcColor"]);
                            int srcChan = GetIntAttributeText(n.Attributes["srcChan"], 0);
                            bool bEnabled = GetBoolAttributeText(n.Attributes["enabled"]);
                            bool bPriority = GetBoolAttributeText(n.Attributes["priority"]);
                            int iVol = GetIntAttributeText(n.Attributes["volume"], 5);
                            int iMaxVol = GetIntAttributeText(n.Attributes["maxVolume"], 0);
                            if (iMaxVol <= 0)
                            {
                                iMaxVol = 100;
                                iVol = iVol * 10;
                            }
                            iVol = Math.Max(0, Math.Min(iMaxVol, iVol));
                            bool bRecord = GetBoolAttributeText(n.Attributes["record"]);
                            int recordKick = GetIntAttributeText(n.Attributes["recordKick"]);
                            if (recordKick <= 0)
                                recordKick = 15;
                            SignalSource newSource = new SignalSource()
                            {
                                SourceId = id,
                                SourceName = srcName,
                                SourceLocation = srcURL,
                                SourceColor = srcColor,
                                SourceType = srcType,
                                SourceChannel = srcChan,
                                IsEnabled = bEnabled,
                                IsPriority = bPriority,
                                RecordAudio = bRecord,
                                RecordingKickTime = recordKick,
                                RecordingType = SignalRecordingType.VOX,
                                Volume = iVol,
                                MaxVolume = iMaxVol,
                                NoiseFloor = Common.NoiseFloor.Normal,
                                CustomNoiseFloor = 10,
                                RemoveNoise = false,
                                DisplayOrder = 10000,
                                GroupId = Guid.Empty
                            };
                            _signalSources.Add(newSource);
                        }
                    }
                }
                else if (xSection.Name.Equals("Sources", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (LoadSourcesFromXml(xSection, false))
                        bChangesMade = true;
                }
                else if (xSection.Name.Equals("Groups", StringComparison.InvariantCultureIgnoreCase))
                {
                    LoadGroupsFromXml(xSection, false);
                }
                else
                {
                    foreach (XmlNode n in xSection.ChildNodes)
                    {
                        WriteString(xSection.Name, n.Name, n.InnerText, string.Empty);
                    }
                }
            }

            if (_signalGroups.Count <= 0)
            {
                SignalGroup grp = new SignalGroup()
                {
                    GroupId = Guid.Empty,
                    GroupName = "Signals",
                    DisplayOrder = 0,
                    GroupMuted = false,
                    GroupColorString = string.Empty
                };
                _signalGroups.Add(grp);
                bChangesMade = true;
            }

            return bChangesMade;
        }

        internal void SaveSourcesToXml(XmlDocument xDoc)
        {
            if (xDoc == null)
                return;
            XmlNode strmParent = xDoc.CreateElement("Sources");
            xDoc.DocumentElement.AppendChild(strmParent);
            foreach (SignalSource source in _signalSources)
            {
                XmlNode x = xDoc.CreateElement("Source");
                if (source.SourceId == Guid.Empty)
                    source.SourceId = Guid.NewGuid();
                string srcLoc = source.SourceLocation;
                if (source.ComPortInfo != null)
                {
                    srcLoc = source.ComPortInfo.ToString();
                }
                x.Attributes.Append(CreateTextAttribute(xDoc, "SourceId", source.SourceId.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "SourceName", source.SourceName));
                x.Attributes.Append(CreateTextAttribute(xDoc, "SourceLocation", srcLoc));
                x.Attributes.Append(CreateTextAttribute(xDoc, "SourceColor", source.SourceColor));
                x.Attributes.Append(CreateEnumAttribute<SignalingSourceType>(xDoc, "SourceType", source.SourceType));
                x.Attributes.Append(CreateTextAttribute(xDoc, "SourceChannel", source.SourceChannel.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "IsEnabled", source.IsEnabled.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "IsPriority", source.IsPriority.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "RecordAudio", source.RecordAudio.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "RecordingKickTime", source.RecordingKickTime.ToString()));
                x.Attributes.Append(CreateEnumAttribute<SignalRecordingType>(xDoc, "RecordingType", source.RecordingType));
                x.Attributes.Append(CreateTextAttribute(xDoc, "Volume", source.Volume.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "MaxVolume", source.MaxVolume.ToString()));
                x.Attributes.Append(CreateEnumAttribute<NoiseFloor>(xDoc, "NoiseFloor", source.NoiseFloor));
                x.Attributes.Append(CreateTextAttribute(xDoc, "CustomNoiseFloor", source.CustomNoiseFloor.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "RemoveNoise", source.RemoveNoise.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "DisplayOrder", source.DisplayOrder.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "GroupId", source.GroupId.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "DecodeMDC1200", source.DecodeMDC1200.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "DecodeGEStar", source.DecodeGEStar.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "DecodeFleetSync", source.DecodeFleetSync.ToString()));
                //x.Attributes.Append(CreateTextAttribute(xDoc, "DecodeP25", source.DecodeP25.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "WaveOutDeviceName", source.WaveOutDeviceName));
                x.Attributes.Append(CreateTextAttribute(xDoc, "WaveOutChannel", source.WaveOutChannel.ToString()));
                strmParent.AppendChild(x);
            }
        }
        internal void SaveGroupsToXml(XmlDocument xDoc)
        {
            if (xDoc == null)
                return;
            XmlNode grpParent = xDoc.CreateElement("Groups");
            xDoc.DocumentElement.AppendChild(grpParent);
            foreach (SignalGroup grp in _signalGroups)
            {
                XmlNode x = xDoc.CreateElement("Group");
                x.Attributes.Append(CreateTextAttribute(xDoc, "GroupId", grp.GroupId.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "GroupName", grp.GroupName));
                x.Attributes.Append(CreateTextAttribute(xDoc, "DisplayOrder", grp.DisplayOrder.ToString()));
                x.Attributes.Append(CreateTextAttribute(xDoc, "Color", grp.GroupColorString));
                grpParent.AppendChild(x);
            }
        }
        private void SaveXML(XmlDocument xDoc)
        {
            SaveSourcesToXml(xDoc);

            SaveGroupsToXml(xDoc);

            foreach (KeyValuePair<string, Dictionary<string, string>> kvp in _values)
            {
                XmlNode parent = xDoc.CreateElement(kvp.Key);
                xDoc.DocumentElement.AppendChild(parent);

                foreach (KeyValuePair<string, string> kvpChild in kvp.Value)
                {
                    XmlNode x = xDoc.CreateElement(kvpChild.Key);
                    x.InnerText = kvpChild.Value;
                    parent.AppendChild(x);
                }
            }
        }

        public void ExportSourcesAndGroups(string strFileName)
        {
            try
            {
                XmlDocument xDoc = new XmlDocument();
                PrepareXML(xDoc);
                SaveSourcesToXml(xDoc);
                SaveGroupsToXml(xDoc);
                if (System.IO.File.Exists(strFileName))
                    System.IO.File.Delete(strFileName);
                using (Stream fs = System.IO.File.OpenWrite(strFileName))
                {
                    xDoc.Save(fs);
                    fs.Close();
                }
            }
            catch { }
        }
        public void ImportSourcesAndGroups(string strFileName)
        {
            if (System.IO.File.Exists(strFileName))
            {
                XmlDocument xDoc = new XmlDocument();
                using (Stream fs = System.IO.File.OpenRead(strFileName))
                {
                    xDoc.Load(fs);
                    fs.Close();
                }
                PrepareXML(xDoc);
                ImportXML(xDoc);
            }
        }

        public void SaveSettingsFile()
        {
            try
            {
                lock (_loadSaveLock)
                {
                    HasSaveError = false;
                    XmlDocument xDoc = new XmlDocument();
                    PrepareXML(xDoc);
                    SaveXML(xDoc);
                    Stream fs = GetSaveSettingsFileStream();
                    xDoc.Save(fs);
                    fs.Close();
                    IsEditing = false;
                }
            }
            catch
            {
                HasSaveError = true;
            }
        }
        public void SaveSettingsToExternalFile(string fileName)
        {
            try
            {
                XmlDocument xDoc = new XmlDocument();
                PrepareXML(xDoc);
                SaveXML(xDoc);
                Stream fs = System.IO.File.Create(fileName);
                xDoc.Save(fs);
                fs.Close();
                IsEditing = false;
            }
            catch { }
        }
        public void LoadSettingsFromExternalFile(string fileName)
        {
            IsEditing = false;
            if (System.IO.File.Exists(fileName))
            {
                XmlDocument xDoc = new XmlDocument();
                Stream fs = System.IO.File.OpenRead(fileName);
                xDoc.Load(fs);
                fs.Close();
                PrepareXML(xDoc);
                IsEditing = LoadXML(xDoc);
            }
            if (IsEditing)
            {
                SaveSettingsToExternalFile(fileName);
            }
        }
        #endregion

        #region DoNotAskYesNo
        public DoNotAskYesNo GetShouldAsk(string settingName)
        {
            return ReadEnum<DoNotAskYesNo>("AskYesNo", settingName, DoNotAskYesNo.Ask);
        }
        public void SetShouldAsk(string settingName, DoNotAskYesNo val)
        {
            WriteEnum<DoNotAskYesNo>("AskYesNo", settingName, val);
        }
        #endregion

        #region License Settings
        public string LicenseActivationKey
        {
            get { return ReadString("License", "LicenseActivationKey", string.Empty); }
            set { WriteString("License", "LicenseActivationKey", value, "LicenseActivationKey"); }
        }
        public string LicenseKey
        {
            get { return ReadString("License", "LicenseKey", string.Empty); }
            set { WriteString("License", "LicenseKey", value, "LicenseKey"); }
        }
        public string LicenseHardwareId
        {
            get { return ReadString("License", "LicenseHardwareId", string.Empty); }
            set { WriteString("License", "LicenseHardwareId", value, "LicenseHardwareId"); }
        }
        #endregion

        #region Workstation Properties
        public Guid WorkstationId
        {
            get
            {
                Guid workstationId = ReadGuid("RadioLog", "WorkstationId");
                if (workstationId == Guid.Empty)
                {
                    workstationId = Guid.NewGuid();
                    WriteGuid("RadioLog", "WorkstationId", workstationId, "WorkstationId");
                    SaveSettingsFile();
                }
                return workstationId;
            }
            set { WriteGuid("RadioLog", "WorkstationId", value, "WorkstationId"); }
        }
        public bool GlobalLoggingEnabled
        {
            get { return ReadBool("RadioLog", "GlobalLoggingEnabled", false, true); }
            set { WriteBool("RadioLog", "GlobalLoggingEnabled", value, "GlobalLoggingEnabled"); }
        }
        public DisplayStyle MainDisplayStyle
        {
            get { return ReadEnum<DisplayStyle>("RadioLog", "MainDisplayStyle", DisplayStyle.TabbedInterface); }
            set { WriteEnum<DisplayStyle>("RadioLog", "MainDisplayStyle", value, "MainDisplayStyle"); }
        }
        public bool UseGroups
        {
            get { return ReadBool("RadioLog", "UseGroups", true); }
            set { WriteBool("RadioLog", "UseGroups", value, "UseGroups"); }
        }
        public ProcessorViewSize ViewSize
        {
            get { return ReadEnum<ProcessorViewSize>("RadioLog", "ProcessorViewSize", ProcessorViewSize.Normal); }
            set { WriteEnum<ProcessorViewSize>("RadioLog", "ProcessorViewSize", value, "ProcessorViewSize"); }
        }
        public int GridFontSize
        {
            get { return ReadInt32("RadioLog", "GridFontSize", 20); }
            set { WriteInt32("RadioLog", "GridFontSize", value, "GridFontSize"); }
        }
        public RadioLogMode WorkstationMode
        {
            get { return ReadEnum<RadioLogMode>("RadioLog", "WorkstationMode", RadioLogMode.Normal); }
            set { WriteEnum<RadioLogMode>("RadioLog", "WorkstationMode", value, "WorkstationMode"); }
        }
        public bool ShowDebugConsole
        {
            get { return ReadBool("RadioLog", "ShowDebugConsole", false); }
            set { WriteBool("RadioLog", "ShowDebugConsole", value, "ShowDebugConsole"); }
        }
        public bool DiagnosticMode
        {
            get { return ReadBool("RadioLog", "DiagnosticMode", false, true); }
            set { WriteBool("RadioLog", "DiagnosticMode", value, "DiagnosticMode"); }
        }
        public bool EnableErrorDumps
        {
            get { return ReadBool("RadioLog", "EnableErrorDumps", true, true); }
            set { WriteBool("RadioLog", "EnableErrorDumps", value, "EnableErrorDumps"); }
        }
        public bool EnableAutoMute
        {
            get { return ReadBool("RadioLog", "EnableAutoMute", false); }
            set { WriteBool("RadioLog", "EnableAutoMute", value); }
        }
        public int AutoMuteHangTime
        {
            get { return ReadInt32("RadioLog", "AutoMuteHangTime", 10); }
            set { WriteInt32("RadioLog", "AutoMuteHangTime", value); }
        }

        public bool InitialLayoutDone
        {
            get { return ReadBool("RadioLog", "InitialLayoutDone", false); }
            set { WriteBool("RadioLog", "InitialLayoutDone", value); }
        }

        public string CurrentTheme
        {
            get { return ReadString("RadioLog", "CurrentTheme"); }
            set { WriteString("RadioLog", "CurrentTheme", value, "CurrentTheme"); }
        }

        public string LogFileDirectory
        {
            get
            {
                string dir = ReadString("RadioLog", "LogFileDirectory");
                if (string.IsNullOrWhiteSpace(dir))
                {
                    dir = AppDocumentsDir;
                    DirectoryHelper.SetupDirectory(dir);
                }
                return dir;
            }
            set
            {
                WriteString("RadioLog", "LogFileDirectory", value, "LogFileDirectory");
            }
        }
        public string RecordFileDirectory
        {
            get
            {
                string dir = ReadString("RadioLog", "RecordFileDirectory");
                if (string.IsNullOrWhiteSpace(dir))
                {
                    dir = AppRecordingDir;
                    DirectoryHelper.SetupDirectory(dir);
                }
                return dir;
            }
            set
            {
                WriteString("RadioLog", "RecordFileDirectory", value, "RecordFileDirectory");
            }
        }

        public bool PlaySoundOnEmergencyAlarm
        {
            get { return ReadBool("RadioLog", "PlaySoundOnEmergencyAlarm", false); }
            set { WriteBool("RadioLog", "PlaySoundOnEmergencyAlarm", value); }
        }
        public string EmergencyAlarmSoundFile
        {
            get { return ReadString("RadioLog", "EmergencyAlarmSoundFile", string.Empty); }
            set { WriteString("RadioLog", "EmergencyAlarmSoundFile", value); }
        }

        public string RadioInfoSettingsFileName
        {
            get { return ReadString("RadioLog", "RadioInfoSettingsFileName", Path.Combine(AppDataDir, "RadioSettings.xml")); }
            set { WriteString("RadioLog", "RadioInfoSettingsFileName", value, "RadioInfoSettingsFileName"); }
        }

        public bool ShouldAutoSaveContacts
        {
            get { return ReadBool("RadioLog", "ShouldAutoSaveContacts", true); }
            set { WriteBool("RadioLog", "ShouldAutoSaveContacts", value, "ShouldAutoSaveContacts"); }
        }

        public bool EnableClipboardStreamURLIntegration
        {
            get { return ReadBool("RadioLog", "EnableClipboardStreamURLIntegration", false); }
            set { WriteBool("RadioLog", "EnableClipboardStreamURLIntegration", value, "EnableClipboardStreamURLIntegration"); }
        }

        public bool ShowSourceName
        {
            get { return ReadBool("RadioLog", "ShowSourceName", true); }
            set { WriteBool("RadioLog", "ShowSourceName", value, "ShowSourceName"); }
        }
        public bool ShowUnitId
        {
            get { return ReadBool("RadioLog", "ShowUnitId", true); }
            set { WriteBool("RadioLog", "ShowUnitId", value, "ShowUnitId"); }
        }
        public bool ShowAgencyName
        {
            get { return ReadBool("RadioLog", "ShowAgencyName", false); }
            set { WriteBool("RadioLog", "ShowAgencyName", value, "ShowAgencyName"); }
        }
        public bool ShowUnitName
        {
            get { return ReadBool("RadioLog", "ShowUnitName", false); }
            set { WriteBool("RadioLog", "ShowUnitName", value, "ShowUnitName"); }
        }
        public bool ShowRadioName
        {
            get { return ReadBool("RadioLog", "ShowRadioName", true); }
            set { WriteBool("RadioLog", "ShowRadioName", value, "ShowRadioName"); }
        }
        public bool ShowAssignedRole
        {
            get { return ReadBool("RadioLog", "ShowAssignedRole", false); }
            set { WriteBool("RadioLog", "ShowAssignedRole", value, "ShowAssignedRole"); }
        }
        public bool ShowAssignedPersonnel
        {
            get { return ReadBool("RadioLog", "ShowAssignedPersonnel", false); }
            set { WriteBool("RadioLog", "ShowAssignedPersonnel", value, "ShowAssignedPersonnel"); }
        }
        public bool ShowSourceType
        {
            get { return ReadBool("RadioLog", "ShowSourceType", false); }
            set { WriteBool("RadioLog", "ShowSourceType", value, "ShowSourceType"); }
        }
        public bool ShowDescription
        {
            get { return ReadBool("RadioLog", "ShowDescription", true); }
            set { WriteBool("RadioLog", "ShowDescription", value, "ShowDescription"); }
        }
        public int RecordingFileSampleRate
        {
            get { return ReadInt32("RadioLog", "RecordingFileSampleRate", -1, true); }
            set { WriteInt32("RadioLog", "RecordingFileSampleRate", value, "RecordingFileSampleRate"); }
        }
        public int RecordingFileBitsPerSample
        {
            get { return ReadInt32("RadioLog", "RecordingFileBitsPerSample", -1, true); }
            set { WriteInt32("RadioLog", "RecordingFileBitsPerSample", value, "RecordingFileBitsPerSample"); }
        }
        #endregion

        #region Radio Reference Properties
        public string RRUserName
        {
            get { return ReadString("RadioReference", "RRUserName", string.Empty); }
            set { WriteString("RadioReference", "RRUserName", value, "RRUserName"); }
        }
        public string RRPassword
        {
            get { return ReadString("RadioReference", "RRPassword", string.Empty); }
            set { WriteString("RadioReference", "RRPassword", value, "RRPassword"); }
        }
        public bool RRSavePassword
        {
            get { return ReadBool("RadioReference", "RRSavePassword", true); }
            set { WriteBool("RadioReference", "RRSavePassword", value, "RRSavePassword"); }
        }
        #endregion

        #region GPS Properties
        public bool EnableGPS
        {
            get { return ReadBool("GPS", "EnableGPS", false, true); }
            set { WriteBool("GPS", "EnableGPS", value, "EnableGPS"); }
        }
        public string GPSPort
        {
            get { return ReadString("GPS", "GPSPort", string.Empty, true); }
            set { WriteString("GPS", "GPSPort", value, "GPSPort"); }
        }
        #endregion

        #region Grid Column Widths
        public double ColTimeWidth { get { return ReadDoubleWithMinumum("GridCol", "ColTimeWidth", 180, 80); } set { WriteDoubleWithMinimum("GridCol", "ColTimeWidth", value, 80, "ColTimeWidth"); } }
        public double ColSourceNameWidth { get { return ReadDoubleWithMinumum("GridCol", "ColSourceNameWidth", 240, 80); } set { WriteDoubleWithMinimum("GridCol", "ColSourceNameWidth", value, 80, "ColSourceNameWidth"); } }
        public double ColUnitIdWidth { get { return ReadDoubleWithMinumum("GridCol", "ColUnitIdWidth", 180, 80); } set { WriteDoubleWithMinimum("GridCol", "ColUnitIdWidth", value, 80, "ColUnitIdWidth"); } }
        public double ColAgencyNameWidth { get { return ReadDoubleWithMinumum("GridCol", "ColAgencyNameWidth", 240, 80); } set { WriteDoubleWithMinimum("GridCol", "ColAgencyNameWidth", value, 80, "ColAgencyNameWidth"); } }
        public double ColUnitNameWidth { get { return ReadDoubleWithMinumum("GridCol", "ColUnitNameWidth", 240, 80); } set { WriteDoubleWithMinimum("GridCol", "ColUnitNameWidth", value, 80, "ColUnitNameWidth"); } }
        public double ColRadioNameWidth { get { return ReadDoubleWithMinumum("GridCol", "ColRadioNameWidth", 240, 80); } set { WriteDoubleWithMinimum("GridCol", "ColRadioNameWidth", value, 80, "ColRadioNameWidth"); } }
        public double ColAssignedRoleWidth { get { return ReadDoubleWithMinumum("GridCol", "ColAssignedRoleWidth", 240, 80); } set { WriteDoubleWithMinimum("GridCol", "ColAssignedRoleWidth", value, 80, "ColAssignedRoleWidth"); } }
        public double ColAssignedPersonnelWidth { get { return ReadDoubleWithMinumum("GridCol", "ColAssignedPersonnelWidth", 240, 80); } set { WriteDoubleWithMinimum("GridCol", "ColAssignedPersonnelWidth", value, 80, "ColAssignedPersonnelWidth"); } }
        public double ColSourceTypedWidth { get { return ReadDoubleWithMinumum("GridCol", "ColSourceTypedWidth", 180, 80); } set { WriteDoubleWithMinimum("GridCol", "ColSourceTypedWidth", value, 80, "ColSourceTypedWidth"); } }
        public double ColDescriptionWidth { get { return ReadDoubleWithMinumum("GridCol", "ColDescriptionWidth", 300, 80); } set { WriteDoubleWithMinimum("GridCol", "ColDescriptionWidth", value, 80, "ColDescriptionWidth"); } }

        public double ColAlarmTimeWidth { get { return ReadDoubleWithMinumum("GridCol", "ColAlarmTimeWidth", 180, 80); } set { WriteDoubleWithMinimum("GridCol", "ColAlarmTimeWidth", value, 80, "ColAlarmTimeWidth"); } }
        public double ColAlarmUnitNameWidth { get { return ReadDoubleWithMinumum("GridCol", "ColAlarmUnitNameWidth", 240, 80); } set { WriteDoubleWithMinimum("GridCol", "ColAlarmUnitNameWidth", value, 80, "ColAlarmUnitNameWidth"); } }
        public double ColAlarmRadioNameWidth { get { return ReadDoubleWithMinumum("GridCol", "ColAlarmRadioNameWidth", 240, 80); } set { WriteDoubleWithMinimum("GridCol", "ColAlarmRadioNameWidth", value, 80, "ColAlarmRadioNameWidth"); } }
        public double ColAlarmAssignedRoleWidth { get { return ReadDoubleWithMinumum("GridCol", "ColAlarmAssignedRoleWidth", 240, 80); } set { WriteDoubleWithMinimum("GridCol", "ColAlarmAssignedRoleWidth", value, 80, "ColAlarmAssignedRoleWidth"); } }
        public double ColAlarmAssignedPersonnelWidth { get { return ReadDoubleWithMinumum("GridCol", "ColAlarmAssignedPersonnelWidth", 240, 80); } set { WriteDoubleWithMinimum("GridCol", "ColAlarmAssignedPersonnelWidth", value, 80, "ColAlarmAssignedPersonnelWidth"); } }
        #endregion

        public List<SignalSource> SignalSources { get { return _signalSources; } }
        public List<SignalGroup> SignalGroups { get { return _signalGroups; } }
        public void AddNewSignalSource(SignalSource src)
        {
            if (src.SourceId == Guid.Empty)
                src.SourceId = Guid.NewGuid();
            this.SignalSources.Add(src);
            this.SaveSettingsFile();
        }
    }

    public class SignalSource
    {
        public Guid SourceId { get; set; }
        public SignalingSourceType SourceType { get; set; }
        public string SourceName { get; set; }
        public string SourceLocation { get; set; }
        public string SourceColor { get; set; }
        public bool IsEnabled { get; set; }
        public int SourceChannel { get; set; }
        public bool IsPriority { get; set; }
        public bool RecordAudio { get; set; }
        public int RecordingKickTime { get; set; }
        public SignalRecordingType RecordingType { get; set; }
        public int Volume { get; set; }
        public int MaxVolume { get; set; }
        public Common.NoiseFloor NoiseFloor { get; set; }
        public int CustomNoiseFloor { get; set; }
        public bool RemoveNoise { get; set; }
        public int DisplayOrder { get; set; }
        public Guid GroupId { get; set; }
        public bool DecodeMDC1200 { get; set; }
        public bool DecodeGEStar { get; set; }
        public bool DecodeFleetSync { get; set; }
        //public bool DecodeP25 { get; set; }
        public string WaveOutDeviceName { get; set; }
        public int WaveOutChannel { get; set; }
        public SerialPortSettings ComPortInfo { get; set; }

        public SignalSource()
        {
            this.ComPortInfo = null;
        }
    }
    public class SignalGroup
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public int DisplayOrder { get; set; }
        public bool GroupMuted { get; set; }
        public string GroupColorString { get; set; }

        public SignalGroup() { }

        public IEnumerable<SignalSource> SignalSourcesForGroup()
        {
            return AppSettings.Instance.SignalSources.Where(s => s.GroupId == this.GroupId).OrderBy(s => s.DisplayOrder);
        }
    }

    public class SignalSourceGroupChangeHolder
    {
        public Guid SignalSourceId { get; set; }
        public Guid NewGroupId { get; set; }
    }

    public class SerialPortSettings
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public System.IO.Ports.Parity Parity { get; set; }
        public int DataBits { get; set; }
        public System.IO.Ports.StopBits StopBits { get; set; }

        public bool HP1Mode { get; set; }
        public bool UnitIdDisplayEnabled { get; set; }
        public int UnitIdDisplayLine { get; set; }

        private T GetEnum<T>(string strValue, T defaultValue) where T : struct,IConvertible
        {
            if (string.IsNullOrEmpty(strValue))
                return defaultValue;
            try
            {
                T rslt = defaultValue;
                if (Enum.TryParse<T>(strValue, true, out rslt))
                    return rslt;
                else
                    return defaultValue;
            }
            catch { return defaultValue; }
        }
        private int GetInt(string strValue, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(strValue))
                return defaultValue;
            int i;
            if (int.TryParse(strValue, out i))
                return i;
            return defaultValue;
        }
        private bool GetBool(string strValue, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(strValue))
                return defaultValue;
            bool b;
            if (bool.TryParse(strValue, out b))
                return b;
            return defaultValue;
        }

        public static bool IsValidComPortInfo(string strInfo)
        {
            if (string.IsNullOrWhiteSpace(strInfo))
                return false;
            string[] strparts = strInfo.Split(new char[] { '|' }, StringSplitOptions.None);
            return strparts != null && strparts.Length >= 5;
        }

        public SerialPortSettings(string strSettings = null)
        {
            this.PortName = string.Empty;
            this.BaudRate = 9600;
            this.Parity = System.IO.Ports.Parity.None;
            this.DataBits = 8;
            this.StopBits = System.IO.Ports.StopBits.One;
            this.HP1Mode = false;
            this.UnitIdDisplayEnabled = false;
            this.UnitIdDisplayLine = 3;

            string[] strParts = null;
            if (!string.IsNullOrWhiteSpace(strSettings))
                strParts = strSettings.Split(new char[] { '|' }, StringSplitOptions.None);
            if (strParts != null && strParts.Length >= 5)
            {
                this.PortName = strParts[0];
                this.BaudRate = GetInt(strParts[1], 9600);
                this.Parity = GetEnum<System.IO.Ports.Parity>(strParts[2], System.IO.Ports.Parity.None);
                this.DataBits = GetInt(strParts[3], 8);
                this.StopBits = GetEnum<System.IO.Ports.StopBits>(strParts[4], System.IO.Ports.StopBits.One);
                if (strParts.Length >= 8)
                {
                    this.HP1Mode = GetBool(strParts[5], false);
                    this.UnitIdDisplayEnabled = GetBool(strParts[6], false);
                    this.UnitIdDisplayLine = GetInt(strParts[7], 3);
                }
            }
        }

        public override string ToString()
        {
            return string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}", PortName, BaudRate.ToString(), Parity.ToString(), DataBits.ToString(), StopBits.ToString(), HP1Mode.ToString(), UnitIdDisplayEnabled.ToString(), UnitIdDisplayLine.ToString());
        }
    }
}
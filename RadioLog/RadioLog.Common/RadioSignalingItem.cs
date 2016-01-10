using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Common
{
    public class RadioSignalingItem
    {
        public SignalingSourceType SourceType { get; private set; }
        public string SourceName { get; private set; }
        public string SignalingFormat { get; private set; }
        public SignalCode Code { get; private set; }
        public string UnitId { get; private set; }
        public string Description { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string RecordingFileName { get; private set; }

        public Guid? RadioId { get; set; }
        public string AgencyName { get; set; }
        public string UnitName { get; set; }
        public string AssignedPersonnel { get; set; }
        public string AssignedRole { get; set; }
        public RadioTypeCode RadioType { get; set; }
        public string RadioName { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public RadioSignalingItem(SignalingSourceType sourceType, string sourceName, string format, SignalCode code, string unitId, string desc, DateTime timestamp, string recordingFileName)
        {
            this.Latitude = null;
            this.Longitude = null;

            this.SourceType = sourceType;
            this.SourceName = sourceName;
            this.SignalingFormat = format;
            this.Code = code;
            this.UnitId = unitId;
            this.Description = desc;
            this.Timestamp = timestamp;
            this.RecordingFileName = recordingFileName;

            this.RadioId = null;
            this.AgencyName = string.Empty;
            this.UnitName = string.Empty;
            this.AssignedPersonnel = string.Empty;
            this.AssignedRole = string.Empty;
            this.RadioType = RadioTypeCode.Unknown;
            this.RadioName = string.Empty;
        }
    }
}

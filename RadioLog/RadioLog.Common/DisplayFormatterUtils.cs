using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Common
{
    public static class DisplayFormatterUtils
    {
        public static string TimestampToDisplayStr(DateTime? dt)
        {
            if (dt == null || !dt.HasValue)
                return string.Empty;
            if (dt.Value.Date == DateTime.Now.Date)
                return dt.Value.ToString("H:mm:ss.FFFF");
            else
                return dt.Value.ToString("yyyy/MM/dd H:mm:ss.FFFF");
        }

        public static string RadioTypeCodeToDisplayStr(RadioTypeCode? code)
        {
            if (code == null || !code.HasValue)
                return string.Empty;
            switch (code.Value)
            {
                case RadioTypeCode.Mobile: return "Mobile";
                case RadioTypeCode.Portable: return "Portable";
                case RadioTypeCode.BaseStation: return "Base Station";
                default: return string.Empty;
            }
        }
        public static string SignalingSourceTypeToDisplayStr(SignalingSourceType? src)
        {
            if (src == null || !src.HasValue)
                return string.Empty;
            switch (src.Value)
            {
                case SignalingSourceType.File: return "File";
                case SignalingSourceType.Streaming: return "Audio Stream";
                case SignalingSourceType.WaveInChannel: return "Line In";
                case SignalingSourceType.StreamingTag: return "Audio Stream Tag";
                default: return string.Empty;
            }
        }
        public static string SignalCodeToDisplayStr(SignalCode? code)
        {
            if (code == null || !code.HasValue)
                return string.Empty;
            switch (code.Value)
            {
                case SignalCode.Emergency: return "EMERGENCY";
                case SignalCode.EmergencyAck: return "EMERGENCY CLEAR";
                case SignalCode.PTT: return "PTT";
                case SignalCode.RadioCheck: return "RADIO CHECK";
                case SignalCode.RadioCheckAck: return "RADIO CHECK ACK";
                case SignalCode.RadioRevive: return "RADIO REVIVE";
                case SignalCode.RadioReviveAck: return "RADIO REVIVE ACK";
                case SignalCode.RadioStun: return "RADIO STUN";
                case SignalCode.RadioStunAck: return "RADIO STUN ACK";
                case SignalCode.Generic: return string.Empty;
                default: return string.Empty;
            }
        }

        public static string MakeDisplayStringFromParts(string joinPart, params string[] parts)
        {
            if (parts == null || parts.Length <= 0)
                return string.Empty;
            List<string> strParts = new List<string>();
            foreach (string s in parts)
            {
                if (!string.IsNullOrWhiteSpace(s))
                    strParts.Add(s);
            }
            return string.Join(joinPart, strParts.ToArray());
        }
    }
}

using System;

namespace RadioLog.Common
{
    public enum RadioLogMode
    {
        Normal,
        Fireground
    }

    public enum SignalCode
    {
        PTT,
        Emergency,
        EmergencyAck,
        RadioCheck,
        RadioCheckAck,
        RadioStun,
        RadioStunAck,
        RadioRevive,
        RadioReviveAck,
        Generic
    }

    public enum SignalingSourceType
    {
        Unknown,
        Streaming,
        File,
        StreamingTag,
        WaveInChannel
    }

    public enum VolumeType
    {
        Default,
        Software
    }

    public enum SignalRecordingType
    {
        VOX,
        Fixed
    }

    public enum RadioTypeCode
    {
        Unknown,
        Mobile,
        Portable,
        BaseStation
    }

    public enum SignalingSourceStatus
    {
        Disabled,
        Disconnected,
        FeedNotActive,
        Idle,
        AudioActive,
        Muted
    }

    public enum WaveInRecordingState
    {
        Stopped,
        Monitoring,
        Recording,
        RequestedStop
    }

    public enum EmergencyState
    {
        NonEmergency,
        EmergencyActive,
        EmergencyCleared
    }

    public enum EmergencyType
    {
        Manual,
        Radio,
        Timer
    }

    public enum AudioKickState
    {
        Off,
        Kick,
        On
    }

    public enum DebugMsgLevel
    {
        RawPackets = 0,
        DebugInfo = 1,
        Information = 2
    }
}

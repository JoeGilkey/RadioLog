using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Common
{
    public enum DisplayStyle
    {
        TabbedInterface,
        GridsOnlyInterface,
        AllInOne,
        GridsAutoOpenSources,
        Columns
    }

    public enum ProcessorViewSize
    {
        Normal,
        Small,
        Wide,
        ExtraWide
    }

    public enum NoiseFloor
    {
        Low,
        Normal,
        High,
        ExtraHigh,
        Custom
    }

    public enum DoNotAskYesNo
    {
        Ask,
        Yes,
        No
    }

    public enum PopupScreenType
    {
        ManualSize,
        Percent80,
        Percent60
    }

    public enum MuteState
    {
        Normal,
        Muted,
        AutoMuted
    }
}

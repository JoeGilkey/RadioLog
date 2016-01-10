using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Source
{
    public enum SourceType
    {
        NONE,
        MIXER,
        TUNER,
        RECORDING
    }

    public class SourceTypeToDesc
    {
        public static string Convert(SourceType srcType)
        {
            switch (srcType)
            {
                case SourceType.NONE: return "No Source";
                case SourceType.MIXER: return "Mixer/Sound Card";
                case SourceType.TUNER: return "Tuner";
                case SourceType.RECORDING: return "IQ Recording";
                default: return string.Empty;
            }
        }
    }
}

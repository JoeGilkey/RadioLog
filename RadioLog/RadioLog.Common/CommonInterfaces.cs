using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Common
{
    public interface ISourceStatus
    {
        string SourceColor { get; }
        RadioLog.Common.SignalingSourceStatus StreamStatus { get; }
    }

    public interface IMainWindowDisplayProvider
    {
        object DataContext { get; set; }
    }
}

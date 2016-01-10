using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cinch
{
    public class MockView : IWorkSpaceAware
    {
        public WorkspaceData WorkSpaceContextualData { get; set; }
    }
}

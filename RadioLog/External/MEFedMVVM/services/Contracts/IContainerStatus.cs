using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MEFedMVVM.Services.Contracts
{
    public interface IContainerStatus : IContextAware
    {
        event Action ContainerLoaded;
        event Action ContainerUnloaded;
    }
}

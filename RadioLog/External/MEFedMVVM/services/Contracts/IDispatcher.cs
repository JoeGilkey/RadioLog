using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MEFedMVVM.Services.Contracts
{
    /// <summary>
    /// Interface to encapsulator the Dispatcher
    /// </summary>
    public interface IDispatcherService : IContextAware
    {
        void BeginInvoke(Delegate method, params object[] parameters);
    }
}

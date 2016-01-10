using System;
using MEFedMVVM.Services.Contracts;
using System.Windows.Threading;
using System.Windows;
using MEFedMVVM.ViewModelLocator;
using System.ComponentModel.Composition;

namespace MEFedMVVM.Services.CommonServices
{
    [PartCreationPolicy(System.ComponentModel.Composition.CreationPolicy.NonShared)]
    [ExportService(ServiceType.Both, typeof(IDispatcherService))]
    public class DefaultDispatcherService : IDispatcherService
    {
        #region IDispatcherService Members

        public void BeginInvoke(Delegate method, params object[] parameters)
        {
            if (currentDispatcher != null)
                currentDispatcher.BeginInvoke(method, parameters);
        }

        #endregion

        #region IContextAware Members
        Dispatcher currentDispatcher;
        public void InjectContext(object context)
        {
            var dependencyObject = context as DependencyObject;
            if (dependencyObject != null)
                currentDispatcher = dependencyObject.Dispatcher;
        }

        #endregion
    }
}

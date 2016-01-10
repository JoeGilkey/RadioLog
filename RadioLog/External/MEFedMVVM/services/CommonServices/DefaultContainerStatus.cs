using System;
using MEFedMVVM.Services.Contracts;
using System.Windows;
using MEFedMVVM.ViewModelLocator;
using System.ComponentModel.Composition;

namespace MEFedMVVM.Services.CommonServices
{
    [PartCreationPolicy(System.ComponentModel.Composition.CreationPolicy.NonShared)]
    [ExportService(ServiceType.Both, typeof(IContainerStatus))]
    public class DefaultContainerStatus : IContainerStatus
    {
        #region IContainerStatus Members

        public event Action ContainerLoaded;

        public event Action ContainerUnloaded;

        #endregion

        #region IContextAware Members
        FrameworkElement _element = null;
        
        public void InjectContext(object context)
        {
            if (_element == context)
                return;

            if (_element != null)// unregister first
            {
                _element.Loaded -= ElementLoaded;
                _element.Unloaded -= ElementUnloaded;
            }

            _element = context as FrameworkElement;

            if (_element != null)
            {
                _element.Loaded += ElementLoaded;
                _element.Unloaded += ElementUnloaded;
            }
        }


        void ElementLoaded(object sender, RoutedEventArgs e)
        {
            if (ContainerLoaded != null)
                ContainerLoaded();
        }

        void ElementUnloaded(object sender, RoutedEventArgs e)
        {
            if (ContainerUnloaded != null)
                ContainerUnloaded();
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

using MEFedMVVM.Common;
using System.Diagnostics;
using MEFedMVVM.Services.Contracts;
using System.Windows;
using System.ComponentModel.Composition.Primitives;

namespace MEFedMVVM.ViewModelLocator
{
    /// <summary>
    /// Inializer for the ViewModels
    /// </summary>
    public class BasicViewModelInializer
    {
        MEFedMVVMResolver resolver;

        public BasicViewModelInializer(MEFedMVVMResolver resolver)
        {
            this.resolver = resolver;
        }
        /// <summary>
        /// Create the ViewModel and injects any services needed
        /// </summary>
        /// <param name="viewModelContext"></param>
        /// <param name="containerElement"></param>
        /// <param name="dataServicesFactories"></param>
        public virtual void CreateViewModel(Export viewModelContext, FrameworkElement containerElement)
        {
            var vm = viewModelContext.Value;
            if (vm == null)
                throw new InvalidOperationException(CannotFindViewModel);

            TryInjectingServicesToVM(viewModelContext, vm, containerElement);

            containerElement.DataContext = vm;

            if (Designer.IsInDesignMode)
            {
                //if the ViewModel is an IDataContextAware ViewModel then we should call the DesignTimeInitialization
                var dataContextAwareVM = containerElement.DataContext as IDesignTimeAware;
                if (dataContextAwareVM != null)
                    dataContextAwareVM.DesignTimeInitialization();
            }
        }

        protected virtual void TryInjectingServicesToVM(Export vmExport, object vm, object containerContext)
        {
            var services = (IEnumerable<Type>)vmExport.Metadata[ExportViewModel.ContextAwareServicesProperty];

             var serviceConsumerVM = vm as IServiceConsumer;

             if (serviceConsumerVM != null && services != null) // then we should feed the view model with these services
             {
                 foreach (var serviceType in services)
                 {
                     //Get services from the MEFComposition
                     Export serviceExport = resolver.GetServiceByContract(serviceType);
                     if (serviceExport != null)
                     {
                         serviceConsumerVM.ServiceLocator.RegisterService(serviceExport.Value, serviceType);
                         InjectContext(serviceExport, containerContext);
                     }
                     else
                     {
                         Debug.WriteLine("Cannot find export for service : " + serviceType.FullName);
                     }
                 }
                 serviceConsumerVM.OnServicesInjected();
             }
        }

        private static void InjectContext(Export serviceExport, object context)
        {
            var contextAware = serviceExport.Value as IContextAware;
            if (contextAware != null)
                contextAware.InjectContext(context);
        }

        #region Exception Strings
        protected const string CannotFindViewModel = "Cannot get ViewModel. Please check that you applied the ExportViewModel attribute and that the ViewModel inherits from BaseViewModel";
        #endregion
    }
}

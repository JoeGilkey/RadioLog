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
        protected MEFedMVVMResolver resolver;

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
            resolver.SetContextToExportProvider(containerElement);
            var vm = resolver.GetExportedValue(viewModelContext);
            resolver.SetContextToExportProvider(null);

            if (vm == null)
                throw new InvalidOperationException(CannotFindViewModel);

            containerElement.DataContext = vm;

            if (Designer.IsInDesignMode)
            {
                //if the ViewModel is an IDataContextAware ViewModel then we should call the DesignTimeInitialization
                var dataContextAwareVM = containerElement.DataContext as IDesignTimeAware;
                if (dataContextAwareVM != null)
                    dataContextAwareVM.DesignTimeInitialization();
            }
        }

        #region Exception Strings
        protected const string CannotFindViewModel = "Cannot get ViewModel. Please check that you applied the ExportViewModel attribute and that the ViewModel inherits from BaseViewModel";
        #endregion
    }
}

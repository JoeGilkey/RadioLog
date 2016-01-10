using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition;

namespace MEFedMVVM.ViewModelLocator
{
    /// <summary>
    /// Import resolver for the MEFedMVVM lib
    /// </summary>
    public class MEFedMVVMResolver
    {
        CompositionContainer _container;
        public MEFedMVVMResolver(CompositionContainer container)
        {
            this._container = container;
        }

        /// <summary>
        /// Gets teh ViewModel export 
        /// </summary>
        /// <param name="vmContractName">The contract for the view model to get</param>
        /// <returns></returns>
        public Export GetViewModelByContract(string vmContractName)
        {
            if (_container == null)
            {
                // try getting the container again
                _container = LocatorBootstrapper.EnsureLocatorBootstrapper();
            }

            var viewModelTypeIdentity = AttributedModelServices.GetTypeIdentity(typeof(object));
            var requiredMetadata = new Dictionary<string, Type>();
            requiredMetadata[ExportViewModel.NameProperty] = typeof(string);
            requiredMetadata[ExportViewModel.ContextAwareServicesProperty] = typeof(IEnumerable<Type>);
            requiredMetadata[ExportViewModel.IsDataContextAwareProperty] = typeof(bool);


            var definition = new ContractBasedImportDefinition(ExportViewModel.Contract, viewModelTypeIdentity,
                                                               requiredMetadata, ImportCardinality.ZeroOrMore, false,
                                                               false, CreationPolicy.NonShared);

            var vmExports = _container.GetExports(definition);
            var vmExport = vmExports.Single(e => e.Metadata[ExportViewModel.NameProperty].Equals(vmContractName));
            if (vmExport != null)
                return vmExport;
            return null;
        }


        public Export GetServiceByContract(Type serviceType)
        {
            if (_container == null)
            {
                // try getting the container again
                _container = LocatorBootstrapper.EnsureLocatorBootstrapper();
            }

            var serviceTypeIdentity = AttributedModelServices.GetTypeIdentity(serviceType);
            var requiredMetadata = new Dictionary<string, Type>();
            requiredMetadata[ExportService.IsDesignTimeServiceProperty] = typeof(ServiceType);
            requiredMetadata[ExportService.ServiceContractProperty] = typeof(Type);


            var definition = new ContractBasedImportDefinition(serviceTypeIdentity, serviceTypeIdentity,
                                                               requiredMetadata, ImportCardinality.ZeroOrMore, false,
                                                               false, CreationPolicy.NonShared);

            var vmExport = _container.GetExports(definition).FirstOrDefault();
            if (vmExport != null)
                return vmExport;
            return null;
        }
    }
}

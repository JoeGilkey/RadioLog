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
        private CompositionContainer _container;
        public MEFedMVVMResolver(CompositionContainer container)
        {
            this._container = container;
        }

        public CompositionContainer Container
        {
            get
            {
                return _container;
            }
        }

        public void SatisfyImports(object attributedPart, object contextToInject)
        {
            SetContextToExportProvider(contextToInject);
            Container.SatisfyImportsOnce(attributedPart);
            SetContextToExportProvider(null);
        }

        /// <summary>
        /// Gets teh ViewModel export 
        /// </summary>
        /// <param name="vmContractName">The contract for the view model to get</param>
        /// <returns></returns>
        public Export GetViewModelByContract(string vmContractName, object contextToInject)
        {
            if(Container == null)
                return null;

            var viewModelTypeIdentity = AttributedModelServices.GetTypeIdentity(typeof(object));
            var requiredMetadata = new Dictionary<string, Type>();
            requiredMetadata[ExportViewModel.NameProperty] = typeof(string);
            requiredMetadata[ExportViewModel.IsDataContextAwareProperty] = typeof(bool);


            var definition = new ContractBasedImportDefinition(vmContractName, viewModelTypeIdentity,
                                                               requiredMetadata, ImportCardinality.ZeroOrMore, false,
                                                               false, CreationPolicy.Any);

            SetContextToExportProvider(contextToInject);
            var vmExports = Container.GetExports(definition);
            SetContextToExportProvider(null);

            var vmExport = vmExports.FirstOrDefault(e => e.Metadata[ExportViewModel.NameProperty].Equals(vmContractName));
            if (vmExport != null)
                return vmExport;
            return null;
        }

        public object GetExportedValue(Export export)
        {
            return Container.GetExportedValue<object>(export.Definition.ContractName);
        }

        internal void SetContextToExportProvider(object contextToInject)
        {
            if (Container.Providers != null && Container.Providers.Count >= 1)
            {
                //try to find the MEFedMVVMExportProvider
                foreach (var item in Container.Providers)
                {
                    var mefedProvider = item as MEFedMVVMExportProvider;
                    if (mefedProvider != null)
                        mefedProvider.SetContextToInject(contextToInject);
                }
            }
        }
    }
}

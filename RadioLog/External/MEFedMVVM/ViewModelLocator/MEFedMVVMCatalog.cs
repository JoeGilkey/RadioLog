using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Primitives;
using MEFedMVVM.Common;

namespace MEFedMVVM.ViewModelLocator
{
    /// <summary>
    /// Custome MEF Catalog to return services that are marked as Runtime when you are at runtime and design time services when you are at design time
    /// </summary>
    public class MEFedMVVMCatalog : ComposablePartCatalog
    {
        private readonly ComposablePartCatalog _inner;
        private readonly IQueryable<ComposablePartDefinition> _partsQuery;

        public MEFedMVVMCatalog(ComposablePartCatalog inner, bool designTime)
        {
            _inner = inner;
            if (designTime)
                _partsQuery = inner.Parts.Where(p => p.ExportDefinitions.Any(ed => !ed.Metadata.ContainsKey("IsDesignTimeService") || ed.Metadata.ContainsKey("IsDesignTimeService") && (ed.Metadata["IsDesignTimeService"].Equals(ServiceType.DesignTime) || ed.Metadata["IsDesignTimeService"].Equals(ServiceType.Both))));
            else
                _partsQuery = inner.Parts.Where(p => p.ExportDefinitions.Any(ed => !ed.Metadata.ContainsKey("IsDesignTimeService") ||  ed.Metadata.ContainsKey("IsDesignTimeService") && (ed.Metadata["IsDesignTimeService"].Equals(ServiceType.Runtime) || ed.Metadata["IsDesignTimeService"].Equals(ServiceType.Both))));                
        }


        public override IQueryable<ComposablePartDefinition> Parts
        {
            get
            {
                return _partsQuery;
            }
        }

        public static MEFedMVVMCatalog CreateCatalog(ComposablePartCatalog inner)
        {
            return new MEFedMVVMCatalog(inner, Designer.IsInDesignMode);
        }
    }
}

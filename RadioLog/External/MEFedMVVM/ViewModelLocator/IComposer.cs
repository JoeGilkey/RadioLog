using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

namespace MEFedMVVM.ViewModelLocator
{
    /// <summary>
    /// Interface for the entity responsable to creates the Composition Container that MEFedMVVM will use to resolve the ViewModels and services
    /// </summary>
    public interface IComposer
    {
        ComposablePartCatalog InitializeContainer();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Windows;

using MEFedMVVM.Common;
using System.ComponentModel.Composition.Primitives;

namespace MEFedMVVM.ViewModelLocator
{
    /// <summary>
    /// This is the ViewModel initializer that ViewModel after it is set as datacontext
    /// </summary>
    public class DataContextAwareViewModelInitializer : BasicViewModelInializer
    {
        public DataContextAwareViewModelInitializer(MEFedMVVMResolver resolver)
            : base (resolver )
        { }

        public override void CreateViewModel(Export viewModelContext, 
            FrameworkElement containerElement)
        {
            if (!Designer.IsInDesignMode) // if at runtime
            {
#if SILVERLIGHT
                RoutedEventHandler handler = null;
                handler = delegate
                {
                    if (containerElement.DataContext != null) // it means we have the VM instance now we should inject the services
                    {
                        resolver.SatisfyImports(containerElement.DataContext, containerElement);
                    }
                    containerElement.Loaded -= handler;
                };
                if (containerElement.DataContext == null)
                    containerElement.Loaded += handler;
                else
                {
                    handler(null, default(RoutedEventArgs));
                }
#else
                DependencyPropertyChangedEventHandler handler = null;
                handler = delegate
                {
                    if (containerElement.DataContext != null) // it means we have the VM instance now we should inject the services
                    {
                        resolver.SatisfyImports(containerElement.DataContext, containerElement);
                    }
                    containerElement.DataContextChanged -= handler;
                };

                if (containerElement.DataContext == null)
                    containerElement.DataContextChanged += handler; // we need to wait until the context is set
                else // DataContext is already set 
                {
                    handler(null, default(DependencyPropertyChangedEventArgs));
                }
#endif
            }


            if(Designer.IsInDesignMode)
            {
                base.CreateViewModel(viewModelContext, containerElement ); // this will create the VM and set it as DataContext

                //if the ViewModel is an IDataContextAware ViewModel then we should call the DesignTimeInitialization
                var dataContextAwareVM = containerElement.DataContext as IDesignTimeAware;
                if (dataContextAwareVM != null)
                    dataContextAwareVM.DesignTimeInitialization();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using MEFedMVVM.Common;
using MEFedMVVM.ViewModelLocator;
using System.Diagnostics;
using System.Windows;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Hosting;

namespace MEFedMVVM.ViewModelLocator
{
    /// <summary>
    /// Repoitory for the ViewModels
    /// </summary>
    public class ViewModelRepository : IPartImportsSatisfiedNotification
    {
        #region Singleton stuff
        //stores the instance of the ViewModel
        static ViewModelRepository instance;

        /// <summary>
        /// Singleton for the ViewModelLocator
        /// </summary>
        public static ViewModelRepository Instance
        {
            get
            {
                EnsureContainer();

                return ViewModelRepository.instance;
            }
        }

        public MEFedMVVMResolver Resolver
        {
            get
            {
                return resolver;
            }
        }

        private static void EnsureContainer()
        {
            //CAS - We have to rebuild the ViewModelRepository everytime when in design mode to ensure we have the correct assemblies
            if (instance == null || Designer.IsInDesignMode)
            {
                instance = new ViewModelRepository();
            }
        }
        #endregion

        private BasicViewModelInializer basicVMInitializer;
        private MEFedMVVMResolver resolver;
        private DataContextAwareViewModelInitializer dataContextAwareVMInitializer;
        private List<KeyValuePair<string, WeakReference>> unsatisfiedContracts = new List<KeyValuePair<string, WeakReference>>();
        private static CompositionContainer _container; 
        //tries to satisfy the imports
        private void TrySatisyImports()
        {
            try
            {
                var tempContainer = LocatorBootstrapper.EnsureLocatorBootstrapper();
                if (tempContainer != null)
                {
                    _container = tempContainer;
                    Debug.WriteLine("MEFedMVVM Composition Container is changing.");
                }
                resolver = new MEFedMVVMResolver(_container);
                basicVMInitializer = new BasicViewModelInializer(Resolver);
                dataContextAwareVMInitializer = new DataContextAwareViewModelInitializer(Resolver);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MEFEdMVVM: ViewModelRepoistory. Unable to satisfy imports. " + ex);
            }
        }

        private ViewModelRepository()
        {
            TrySatisyImports();
        }

        public static void AttachViewModelToView( string vmContract, FrameworkElement view)
        {
            var vmExport = Instance.Resolver.GetViewModelByContract(vmContract, view);
            if (vmExport != null)
            {
                Debug.WriteLine("Attaching ViewModel " + vmExport.Metadata[ExportViewModel.NameProperty]);
                if ((bool)vmExport.Metadata[ExportViewModel.IsDataContextAwareProperty])
                    ViewModelRepository.Instance.dataContextAwareVMInitializer.CreateViewModel(vmExport, view);
                else
                    ViewModelRepository.Instance.basicVMInitializer.CreateViewModel(vmExport, view);
            }
            else
            {
                RegisterMissingViewModel(vmContract, view);
            }
        }

        public static void RegisterMissingViewModel(string vmContractName, FrameworkElement view)
        {
            Debug.WriteLine("MEFEdMVVM: ViewModelRepoistory. ViewModel not found. Will try to recompose the ViewModel when a Recomposition is done");
            lock(Instance.unsatisfiedContracts)
                Instance.unsatisfiedContracts.Add( new KeyValuePair<string, WeakReference>( vmContractName, new WeakReference(view) ));
        }

        #region IPartImportsSatisfiedNotification Members

        public void OnImportsSatisfied()
        {
            lock (unsatisfiedContracts)
            {
                for (int i = 0; i < unsatisfiedContracts.Count; i++)
                {
                    var vmContract = unsatisfiedContracts[i];
                    Debug.WriteLine("MEFEdMVVM: ViewModelRepoistory. Recomposition was made. Will try to recompose missing ViewModel " + vmContract);
                    var vm = Instance.Resolver.GetViewModelByContract(vmContract.Key, vmContract.Value.IsAlive ? vmContract.Value.Target : null);
                    if (vm != null)
                    {
                        if (vmContract.Value.IsAlive) // if the UI element is still alive
                        {
                            AttachViewModelToView(vmContract.Key, (FrameworkElement)vmContract.Value.Target);
                        }
                        //remove the item from the list of unsatisfied contracts
                        unsatisfiedContracts.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
        #endregion
    }
}

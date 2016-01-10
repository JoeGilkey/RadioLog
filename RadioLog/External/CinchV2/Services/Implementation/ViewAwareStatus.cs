using System;
using System.Windows;
using System.ComponentModel.Composition;
using System.Windows.Threading;
using MEFedMVVM.Services.Contracts;
using MEFedMVVM.ViewModelLocator;

namespace Cinch
{
    /// <summary>
    /// View aware service that provides the following
    /// 1. Events for ViewLoaded / ViewUnloaded (WPF and SL)
    /// 2. Events for ViewActivated / ViewDeactivated (WPF Only)
    /// 3. Views current Dispatcher
    /// 4. If the view implements <c>IViewCreationContextProvider</c>
    ///    the current Views Context will also be available to allow
    ///    the ViewModel to obtain some view specific contextual information
    /// </summary>
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportService(ServiceType.Both, typeof(IViewAwareStatus))]
    public class ViewAwareStatus : IViewAwareStatus
    {

        #region Data
        private FrameworkElement view = null;
        #endregion

        #region IViewAwareStatus Members

        public event Action ViewLoaded;
        public event Action ViewUnloaded;

#if !SILVERLIGHT
        public event Action ViewActivated;
        public event Action ViewDeactivated;
#endif

        public Dispatcher ViewsDispatcher { get; private set; }

        public Object View
        {
            get
            {
                return (Object)view;
            }
        }
        #endregion

        #region IContextAware Members

        public void InjectContext(object view)
        {
            if (this.view == view)
                return;

            // unregister before hooking new events
            if (this.view != null)
            {
                this.view.Loaded -= OnViewLoaded;
                this.view.Unloaded -= OnViewUnloaded;
#if !SILVERLIGHT
                Window w = this.view as Window;
                if (w != null)
                {
                    w.Activated -= OnViewActivated;
                    w.Deactivated -= OnViewDeactivated;
                }
#endif
            }

            this.view = view as FrameworkElement;

            if (this.view != null)
            {
                this.view.Loaded += OnViewLoaded;
                this.view.Unloaded += OnViewUnloaded;

#if !SILVERLIGHT
                Window w = this.view as Window;
                if (w != null)
                {
                    w.Activated += OnViewActivated;
                    w.Deactivated += OnViewDeactivated;
                }
#endif
                //get the Views Dispatcher
                this.ViewsDispatcher = this.view.Dispatcher;

            }
        }




        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (ViewLoaded != null)
                ViewLoaded();
        }

        private void OnViewUnloaded(object sender, RoutedEventArgs e)
        {
            if (ViewUnloaded != null)
                ViewUnloaded();
        }


#if !SILVERLIGHT

        private void OnViewActivated(object sender, EventArgs e)
        {
            if (ViewActivated != null)
                ViewActivated();
        }

        private void OnViewDeactivated(object sender, EventArgs e)
        {
            if (ViewDeactivated != null)
                ViewDeactivated();
        }


#endif

        #endregion
    }
}
using System;
using System.Windows;
using System.ComponentModel.Composition;
using System.Windows.Threading;

namespace Cinch
{
    /// <summary>
    /// A test IVisualStateManager based service that can be used in a Unit
    /// test for WPF/SL
    /// </summary>


    /// <summary>
    /// A test IVisualStateManager based service that can be used in a Unit
    /// test for WPF/SL
    /// 
    /// This test version provides simulation methods SimulateViewXXXXX 
    /// that can be used simulate the ViewLoading etc etc.
    /// 
    /// For the Dispatcher the current Dispatcher is used
    /// 
    /// And for the <c>IViewCreationContextProvider</c>.Context you can use the 
    /// ViewCreationContext property setter to set it to whatever Content the View
    /// would have had its <c>IViewCreationContextProvider</c>.Context set to
    /// </summary>
    public class TestViewAwareStatus : IViewAwareStatus
    {
        #region Data
        //This should more than likely be some IView type of object
        private object simulatedViewObject;

        #endregion

        #region Ctor
        public TestViewAwareStatus()
        {
#if SILVERLIGHT
            ViewsDispatcher = System.Windows.Deployment.Current.Dispatcher;
#else
            ViewsDispatcher = Dispatcher.CurrentDispatcher;
#endif
        }
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
                return simulatedViewObject;
            }
            set
            {
                simulatedViewObject = value;
            }
        }


        #endregion

        #region IViewAware Members

        public void InjectContext(object view)
        {
            //nothing to do here, we should not be creating a FrameworkElement
            //in a unit test anyway, so we should expect "view" to be null
            //from a unit test case
        }
        #endregion

        #region Helpers



        /// <summary>
        /// can be called from unit test to simulate view Loaded
        /// </summary>
        public void SimulateViewIsLoadedEvent()
        {
            if (ViewLoaded != null)
                ViewLoaded();
        }

        /// <summary>
        /// can be called from unit test to simulate view Unloaded
        /// </summary>
        public void SimulateViewIsUnloadedEvent()
        {
            if (ViewUnloaded != null)
                ViewUnloaded();
        }

#if !SILVERLIGHT

        /// <summary>
        /// Can be called from unit test to simulate view Activated
        /// </summary>
        public void SimulateViewIsActivatedEvent()
        {
            if (ViewActivated != null)
                ViewActivated();
        }

        /// <summary>
        /// Can be called from unit test to simulate view Deactivated
        /// </summary>
        public void SimulateViewIsDeactivatedEvent()
        {
            if (ViewDeactivated != null)
                ViewDeactivated();
        }

#endif

        #endregion
    }
}
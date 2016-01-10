using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Threading;

/// <summary>
/// This class was obtained from Daniel Vaughan (a fellow WPF Discples blog)
/// http://www.codeproject.com/KB/silverlight/Mtvt.aspx
/// </summary>
namespace Cinch
{
    /// <summary>
    /// Singleton class providing the default implementation 
    /// for the <see cref="ISynchronizationContext"/>, specifically for the UI thread.
    /// </summary>
    public partial class UISynchronizationContext : ISynchronizationContext
    {
        #region Data
        private DispatcherSynchronizationContext context;
        private Dispatcher dispatcher;
        private readonly object initializationLock = new object();
        #endregion

        #region Singleton implementation

        static readonly UISynchronizationContext instance = new UISynchronizationContext();

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        /// <value>The singleton instance.</value>
        public static ISynchronizationContext Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion

        #region Private Methods
        private void EnsureInitialized()
        {
            if (dispatcher != null && context != null)
            {
                return;
            }

            lock (initializationLock)
            {
                if (dispatcher != null && context != null)
                {
                    return;
                }

                try
                {
#if SILVERLIGHT
                dispatcher = System.Windows.Deployment.Current.Dispatcher;
#else
                    dispatcher = Dispatcher.CurrentDispatcher;
#endif
                    context = new DispatcherSynchronizationContext(dispatcher);
                }
                catch (InvalidOperationException)
                {
                    throw new Exception("Initialised called from non-UI thread.");
                }
            }
        }
        #endregion

        #region ISynchronizationContext Methods

        public void Initialize()
        {
            EnsureInitialized();
        }

        public void Initialize(Dispatcher dispatcher)
        {
            ArgumentValidator.AssertNotNull(dispatcher, "dispatcher");
            lock (initializationLock)
            {
                this.dispatcher = dispatcher;
                context = new DispatcherSynchronizationContext(dispatcher);
            }
        }

        public void InvokeWithoutBlocking(SendOrPostCallback callback, object state)
        {
            ArgumentValidator.AssertNotNull(callback, "callback");
            EnsureInitialized();

            context.Post(callback, state);
        }

        public void InvokeWithoutBlocking(Action action)
        {
            ArgumentValidator.AssertNotNull(action, "action");
            EnsureInitialized();

            context.Post(state => action(), null);
        }

        public void InvokeAndBlockUntilCompletion(SendOrPostCallback callback, object state)
        {
            ArgumentValidator.AssertNotNull(callback, "callback");
            EnsureInitialized();

            context.Send(callback, state);
        }

        public void InvokeAndBlockUntilCompletion(Action action)
        {
            ArgumentValidator.AssertNotNull(action, "action");
            EnsureInitialized();

            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                context.Send(delegate { action(); }, null);
            }
        }

        public bool InvokeRequired
        {
            get
            {
                EnsureInitialized();
                return !dispatcher.CheckAccess();
            }
        }
        #endregion
    }


}

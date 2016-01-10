
using System;
using System.Threading;
using System.Windows.Threading;

/// <summary>
/// This class was obtained from Daniel Vaughan (a fellow WPF Discples blog)
/// http://www.codeproject.com/KB/silverlight/Mtvt.aspx
/// </summary>
namespace Cinch
{
    /// <summary>
    /// SynchronizationContext interface that provides
    /// various thread marshalling calls to be done
    /// </summary>
    public interface ISynchronizationContext
    {
        bool InvokeRequired { get; }

        void Initialize();
        void Initialize(Dispatcher dispatcher);
        void InvokeAndBlockUntilCompletion(Action action);
        void InvokeAndBlockUntilCompletion(SendOrPostCallback callback, object state);
        void InvokeWithoutBlocking(Action action);
        void InvokeWithoutBlocking(SendOrPostCallback callback, object state);
    }
}


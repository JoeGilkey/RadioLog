using System;
using System.ComponentModel;
using System.Windows.Threading;

using Cinch;

namespace RadioLog.WPFCommon
{
    public abstract class ThreadSafeViewModelBase : ViewModelBase, System.ComponentModel.Composition.IPartImportsSatisfiedNotification
    {
        private bool _waitingForPropChangedDispatcher = false;
        private object _waitingLock = new object();
        private BatchQueue<PropertyChangedEventArgs> _propChangedQueue = new BatchQueue<PropertyChangedEventArgs>();
        private bool _disablePropertyChanges = false;

        protected void DisablePropertyChanges() { _disablePropertyChanges = true; }
        protected void EnablePropertyChanges() { _disablePropertyChanges = false; }

        private static Dispatcher _dispatcher = null;
        protected static Dispatcher ThreadSafeDispatcher
        {
            get
            {
                if (_dispatcher == null)
                {
                    if (System.Windows.Application.Current != null)
                    {
                        _dispatcher = System.Windows.Application.Current.Dispatcher;
                    }
                }
                return _dispatcher;
            }
        }

        delegate void HandleThreadSafeDelegate();
        protected virtual void HandlePropertyEvents()
        {
            lock (_waitingLock)
            {
                _waitingForPropChangedDispatcher = false;
            }
            PropertyChangedEventArgs[] args = _propChangedQueue.DequeueCurrentQueue();
            if (args != null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("HandlePropertyEvents for {0} changed events.", args.Length));
                foreach (PropertyChangedEventArgs arg in args)
                    base.NotifyPropertyChanged(arg);
            }
        }
        private bool ShouldFirePropEventsDispatcher()
        {
            if (_waitingForPropChangedDispatcher)
                return false;
            else
                lock (_waitingLock)
                {
                    _waitingForPropChangedDispatcher = true;
                    return true;
                }
        }
        protected virtual void HookPropertyChanged(string propertyName) { }
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (e == null || _disablePropertyChanges)
                return;
            HookPropertyChanged(e.PropertyName);
            _propChangedQueue.Enqueue(e);
            if (ThreadSafeDispatcher == null || ThreadSafeDispatcher.CheckAccess())
                HandlePropertyEvents();
            else if (ShouldFirePropEventsDispatcher())
                ThreadSafeDispatcher.BeginInvoke(new HandleThreadSafeDelegate(HandlePropertyEvents));
        }

        public void OnPropertyChanged(string propertyName) { OnPropertyChanged(new PropertyChangedEventArgs(propertyName)); }
        protected void OnPropertyChanged()
        {
            string propertyName = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name.Substring(4);
            OnPropertyChanged(propertyName);
        }

        public virtual void OnImportsSatisfied() { }
    }
}

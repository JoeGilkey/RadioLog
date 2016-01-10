using System;
using System.ComponentModel;
using System.Windows.Threading;

namespace RadioLog.WPFCommon
{
    public abstract class ThreadSafeNotificationObject : INotifyPropertyChanged, INotifyPropertyChanging, IDisposable
    {
        private bool _waitingForPropChangedDispatcher = false;
        private object _waitingLock = new object();
        private  BatchQueue<PropertyChangingEventArgs> _propChangingQueue = new  BatchQueue<PropertyChangingEventArgs>();
        private  BatchQueue<PropertyChangedEventArgs> _propChangedQueue = new  BatchQueue<PropertyChangedEventArgs>();
        private bool _disablePropertyChanges = false;

        public ThreadSafeNotificationObject()
        {
            //
        }

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
            PropertyChangingEventArgs[] cArgs = _propChangingQueue.DequeueCurrentQueue();
            if (cArgs != null && PropertyChanging != null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("HandlePropertyEvents for {0} changing events.", cArgs.Length));
                foreach (PropertyChangingEventArgs arg in cArgs)
                    PropertyChanging(this, arg);
            }
            PropertyChangedEventArgs[] args = _propChangedQueue.DequeueCurrentQueue();
            if (args != null && PropertyChanged != null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("HandlePropertyEvents for {0} changed events.", args.Length));
                foreach (PropertyChangedEventArgs arg in args)
                    PropertyChanged(this, arg);
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
            if (this.PropertyChanged != null)
            {
                _propChangedQueue.Enqueue(e);
                if (ThreadSafeDispatcher == null || ThreadSafeDispatcher.CheckAccess())
                    HandlePropertyEvents();
                else if (ShouldFirePropEventsDispatcher())
                    ThreadSafeDispatcher.BeginInvoke(new HandleThreadSafeDelegate(HandlePropertyEvents));
            }
        }
        protected virtual void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            if (e == null || _disablePropertyChanges)
                return;
            if (this.PropertyChanging != null && e != null)
            {
                _propChangingQueue.Enqueue(e);
                if (ThreadSafeDispatcher == null || ThreadSafeDispatcher.CheckAccess())
                    HandlePropertyEvents();
                else if (ShouldFirePropEventsDispatcher())
                    ThreadSafeDispatcher.BeginInvoke(new HandleThreadSafeDelegate(HandlePropertyEvents));
            }
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) { OnPropertyChanged(new PropertyChangedEventArgs(propertyName)); }
        protected void OnPropertyChanged()
        {
            string propertyName = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name.Substring(4);
            OnPropertyChanged(propertyName);
        }

        [field: NonSerialized]
        public event PropertyChangingEventHandler PropertyChanging;

        protected void OnPropertyChanging(string propertyName) { OnPropertyChanging(new PropertyChangingEventArgs(propertyName)); }
        protected void OnPropertyChanging()
        {
            string propertyName = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name.Substring(4);
            OnPropertyChanging(propertyName);
        }

        #region IDisposable Members
        private bool _isDisposed = false;
        public virtual void Dispose()
        {
            _isDisposed = true;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [System.ComponentModel.DataAnnotations.Display(AutoGenerateField = false, AutoGenerateFilter = false)]
        public bool IsDisposed { get { return _isDisposed; } }
        #endregion
    }
}

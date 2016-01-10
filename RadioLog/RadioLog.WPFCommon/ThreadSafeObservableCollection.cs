using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace RadioLog.WPFCommon
{
    [Serializable]
    public class ThreadSafeObservableCollection<T> : Collection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        [Serializable]
        private class SimpleMonitor : IDisposable
        {
            private int _busyCount = 0;

            public void Dispose() { this._busyCount--; }
            public void Enter() { this._busyCount++; }
            public bool Busy { get { return (this._busyCount > 0); } }
        }

        #region Fields
        private object _lockObj = new object();
        private SimpleMonitor _monitor;
        private const string CountString = "Count";
        private const string IndexerName = "Item[]";
        #endregion

        #region Events
        [field: NonSerialized]
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructors
        public ThreadSafeObservableCollection() { this._monitor = new ThreadSafeObservableCollection<T>.SimpleMonitor(); }
        public ThreadSafeObservableCollection(IEnumerable<T> collection)
        {
            this._monitor = new ThreadSafeObservableCollection<T>.SimpleMonitor();
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            this.CopyFrom(collection);
        }
        public ThreadSafeObservableCollection(List<T> list)
            : base((list != null) ? new List<T>(list.Count) : list)
        {
            this._monitor = new ThreadSafeObservableCollection<T>.SimpleMonitor();
            this.CopyFrom(list);
        }
        #endregion
        #region Protected Methods
        protected IDisposable BlockReentrancy()
        {
            this._monitor.Enter();
            return this._monitor;
        }
        protected void CheckReentrancy()
        {
            if ((this._monitor.Busy && (this.CollectionChanged != null)) && (this.CollectionChanged.GetInvocationList().Length > 1))
            {
                throw new InvalidOperationException("Reentrancy Not Allowed");
            }
        }
        void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnItemPropertyChanged(new ItemPropertyChangedEventArgs<T>((T)sender, e.PropertyName));
        }
        void Item_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            OnItemPropertyChanging(new ItemPropertyChangedEventArgs<T>((T)sender, e.PropertyName));
        }
        protected override void ClearItems()
        {
            lock (_lockObj)
            {
                this.CheckReentrancy();
                int iCnt = base.Items.Count;
                for (int i = 0; i < iCnt; i++)
                {
                    if (i >= base.Items.Count)
                        break;
                    T item = base.Items[i];
                    INotifyPropertyChanged npc = item as INotifyPropertyChanged;
                    if (npc != null)
                        npc.PropertyChanged -= new PropertyChangedEventHandler(Item_PropertyChanged);
                    INotifyPropertyChanging npc2 = item as INotifyPropertyChanging;
                    if (npc2 != null)
                        npc2.PropertyChanging -= new PropertyChangingEventHandler(Item_PropertyChanging);
                }
                base.ClearItems();
                this.OnPropertyChanged(CountString);
                this.OnPropertyChanged(IndexerName);
                this.OnCollectionReset();
                HookItemsCleared();
            }
        }
        protected override void InsertItem(int index, T item)
        {
            lock (_lockObj)
            {
                if (base.Contains(item))
                    return;
                INotifyPropertyChanged npc = item as INotifyPropertyChanged;
                if (npc != null)
                    npc.PropertyChanged += new PropertyChangedEventHandler(Item_PropertyChanged);
                INotifyPropertyChanging npc2 = item as INotifyPropertyChanging;
                if (npc2 != null)
                    npc2.PropertyChanging += new PropertyChangingEventHandler(Item_PropertyChanging);
                this.CheckReentrancy();
                base.InsertItem(index, item);
                this.OnPropertyChanged(CountString);
                this.OnPropertyChanged(IndexerName);
                //this.OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
                this.OnCollectionReset();
                this.ActiveItem = item;
                HookItemAdded(item);
            }
        }
        protected virtual void MoveItem(int oldIndex, int newIndex)
        {
            lock (_lockObj)
            {
                this.CheckReentrancy();
                T item = base[oldIndex];
                base.RemoveItem(oldIndex);
                base.InsertItem(newIndex, item);
                this.OnPropertyChanged(IndexerName);
                //this.OnCollectionChanged(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex);
                this.OnCollectionReset();
            }
        }
        protected override void RemoveItem(int index)
        {
            lock (_lockObj)
            {
                this.CheckReentrancy();
                if (index >= base.Count)
                    return;
                T item = base[index];
                INotifyPropertyChanged npc = item as INotifyPropertyChanged;
                if (npc != null)
                    npc.PropertyChanged -= new PropertyChangedEventHandler(Item_PropertyChanged);
                if (index >= base.Count)
                    return;
                base.RemoveItem(index);
                this.OnPropertyChanged(CountString);
                this.OnPropertyChanged(IndexerName);
                //this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
                this.OnCollectionReset();
                if (object.Equals(item, ActiveItem))
                    ActiveItem = default(T);
                HookItemRemoved(item);
            }
        }
        protected override void SetItem(int index, T item)
        {
            lock (_lockObj)
            {
                this.CheckReentrancy();
                T oldItem = base[index];
                INotifyPropertyChanged npcOld = oldItem as INotifyPropertyChanged;
                if (npcOld != null)
                    npcOld.PropertyChanged -= new PropertyChangedEventHandler(Item_PropertyChanged);
                INotifyPropertyChanged npc = item as INotifyPropertyChanged;
                if (npc != null)
                    npc.PropertyChanged += new PropertyChangedEventHandler(Item_PropertyChanged);
                base.SetItem(index, item);
                this.OnPropertyChanged(IndexerName);
                //this.OnCollectionChanged(NotifyCollectionChangedAction.Replace, oldItem, item, index);
                this.OnCollectionReset();
                HookItemReplaced(oldItem, item);
            }
        }
        #endregion
        #region Private Methods
        private void CopyFrom(IEnumerable<T> collection)
        {
            lock (_lockObj)
            {
                IList<T> items = base.Items;
                if ((collection != null) && (items != null))
                {
                    using (IEnumerator<T> enumerator = collection.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            items.Add(enumerator.Current);
                        }
                    }
                }
            }
        }
        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        }
        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
        }
        private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
        }
        private void OnCollectionReset()
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        public void RaiseCollectionChanged() { this.OnCollectionReset(); }
        private void OnPropertyChanged(string propertyName) { this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName)); }
        #endregion
        #region Public Methods
        public void Move(int oldIndex, int newIndex) { this.MoveItem(oldIndex, newIndex); }
        public void Sort()
        {
            bool bWasReset = false;
            try
            {
                List<T> list = base.Items as List<T>;
                if (list != null)
                {
                    bWasReset = true;
                    list.Sort();
                }
            }
            catch
            {
                //crap...
            }
            try
            {
                if (bWasReset)
                {
                    this.OnCollectionReset();
                }
            }
            catch { }
        }
        #endregion
        #region Hooks
        protected virtual void HookItemsCleared() { }
        protected virtual void HookItemAdded(T item) { }
        protected virtual void HookItemRemoved(T item) { }
        protected virtual void HookItemReplaced(T oldItem, T newItem) { }
        protected virtual void HookItemPropertyChanged(T item, string propertyName) { }
        protected virtual void HookItemPropertyChanging(T item, string propertyName) { }
        #endregion
        #region NotifyItemPropertyChanged
        delegate void OnItemPropertyChangedDelegate(T item, string propertyName);
        protected void OnItemPropertyChanged(T item, string propertyName) { OnItemPropertyChanged(new ItemPropertyChangedEventArgs<T>(item, propertyName)); }

        [field: NonSerialized]
        public event EventHandler<ItemPropertyChangedEventArgs<T>> ItemPropertyChanged;
        #endregion
        #region NotifyItemPropertyChanging
        delegate void OnItemPropertyChangingDelegate(T item, string propertyName);
        protected void OnItemPropertyChanging(T item, string propertyName) { OnItemPropertyChanging(new ItemPropertyChangedEventArgs<T>(item, propertyName)); }

        [field: NonSerialized]
        public event EventHandler<ItemPropertyChangedEventArgs<T>> ItemPropertyChanging;
        #endregion
        #region ThreadSafe stuff
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

        private bool _waitingForPropChangedDispatcher = false;
        private bool _waitingForCollChangedDispatcher = false;
        private bool _waitingForItemChangedDispatcher = false;
        private bool _waitingForItemChangingDispatcher = false;
        private object _waitingLock = new object();
        private BatchQueue<PropertyChangedEventArgs> _propChangedQueue = new BatchQueue<PropertyChangedEventArgs>();
        //private BatchQueue<NotifyCollectionChangedEventArgs> _collChangedQueue = new BatchQueue<NotifyCollectionChangedEventArgs>();
        private BatchQueue<ItemPropertyChangedEventArgs<T>> _itemChangedQueue = new BatchQueue<ItemPropertyChangedEventArgs<T>>();
        private BatchQueue<ItemPropertyChangedEventArgs<T>> _itemChangingQueue = new BatchQueue<ItemPropertyChangedEventArgs<T>>();

        delegate void HandleThreadSafeCollectionChanged(NotifyCollectionChangedEventArgs args);
        delegate void HandleThreadSafeDelegate();
        /*
        private void HandleFullCollectionChanged()
        {
            lock (_waitingLock)
            {
                _waitingForCollChangedDispatcher = false;
            }
            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        */
        /*
        private void HandleCollectionChanged()
        {
            lock (_waitingLock)
            {
                _waitingForCollChangedDispatcher = false;
            }
            NotifyCollectionChangedEventArgs[] args = _collChangedQueue.DequeueCurrentQueue();
            if (args != null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("HandleCollectionChanged for {0} events.", args.Length));
                foreach (NotifyCollectionChangedEventArgs arg in args)
                    CollectionChanged(this, arg);
            }
        }
         */

        private void HandlePropertyChanged()
        {
            lock (_waitingLock)
            {
                _waitingForPropChangedDispatcher = false;
            }
            PropertyChangedEventArgs[] args = _propChangedQueue.DequeueCurrentQueue();
            if (args != null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("HandlePropertyChanged for {0} events.", args.Length));
                foreach (PropertyChangedEventArgs arg in args)
                    PropertyChanged(this, arg);
            }
        }
        private void HandleItemChanged()
        {
            lock (_waitingLock)
            {
                _waitingForItemChangedDispatcher = false;
            }
            ItemPropertyChangedEventArgs<T>[] args = _itemChangedQueue.DequeueCurrentQueue();
            if (args != null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("HandleItemPropertyChanged for {0} events.", args.Length));
                foreach (ItemPropertyChangedEventArgs<T> arg in args)
                    ItemPropertyChanged(this, arg);
            }
        }
        private void HandleItemChanging()
        {
            lock (_waitingLock)
            {
                _waitingForItemChangingDispatcher = false;
            }
            ItemPropertyChangedEventArgs<T>[] args = _itemChangingQueue.DequeueCurrentQueue();
            if (args != null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("HandleItemPropertyChanging for {0} events.", args.Length));
                foreach (ItemPropertyChangedEventArgs<T> arg in args)
                    ItemPropertyChanging(this, arg);
            }
        }
        private bool ShouldFirePropChangedDispatcher()
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
        private bool ShouldFireCollChangedDispatcher()
        {
            if (_waitingForCollChangedDispatcher)
                return false;
            else
                lock (_waitingLock)
                {
                    _waitingForCollChangedDispatcher = true;
                    return true;
                }
        }
        private bool ShouldFireItemPropertyChangedDispatcher()
        {
            if (_waitingForItemChangedDispatcher)
                return false;
            else
                lock (_waitingLock)
                {
                    _waitingForItemChangedDispatcher = true;
                    return true;
                }
        }
        private bool ShouldFireItemPropertyChangingDispatcher()
        {
            if (_waitingForItemChangingDispatcher)
                return false;
            else
                lock (_waitingLock)
                {
                    _waitingForItemChangingDispatcher = true;
                    return true;
                }
        }
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                if (ThreadSafeDispatcher == null || ThreadSafeDispatcher.CheckAccess())
                {
                    this.CollectionChanged(this, e);
                }
                else
                {
                    ThreadSafeDispatcher.BeginInvoke(new HandleThreadSafeCollectionChanged(OnCollectionChanged), e);
                }
            }
            /*
            if (this.CollectionChanged != null)
            {
                using (this.BlockReentrancy())
                {
                    _collChangedQueue.Enqueue(e);
                    if (ThreadSafeDispatcher == null || ThreadSafeDispatcher.CheckAccess())
                        HandleCollectionChanged();
                    else if (ShouldFireCollChangedDispatcher())
                        ThreadSafeDispatcher.BeginInvoke(new HandleThreadSafeDelegate(HandleCollectionChanged));
                    /*
                    if (ThreadSafeDispatcher == null || ThreadSafeDispatcher.CheckAccess())
                        HandleFullCollectionChanged();
                    else if (ShouldFireCollChangedDispatcher())
                        ThreadSafeDispatcher.BeginInvoke(new HandleThreadSafeDelegate(HandleFullCollectionChanged));
                     //jgstophere
                }
            }
             */
        }
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (this.PropertyChanged != null)
            {
                _propChangedQueue.Enqueue(e);
                if (ThreadSafeDispatcher == null || ThreadSafeDispatcher.CheckAccess())
                    HandlePropertyChanged();
                else if (ShouldFirePropChangedDispatcher())
                    ThreadSafeDispatcher.BeginInvoke(new HandleThreadSafeDelegate(HandlePropertyChanged));
            }
        }
        protected virtual void OnItemPropertyChanged(ItemPropertyChangedEventArgs<T> e)
        {
            if (ItemPropertyChanged != null)
            {
                _itemChangedQueue.Enqueue(e);
                if (ThreadSafeDispatcher == null || ThreadSafeDispatcher.CheckAccess())
                    HandleItemChanged();
                else if (ShouldFireItemPropertyChangedDispatcher())
                    ThreadSafeDispatcher.BeginInvoke(new HandleThreadSafeDelegate(HandleItemChanged));
            }
            HookItemPropertyChanged(e.Item, e.PropertyName);
        }
        protected virtual void OnItemPropertyChanging(ItemPropertyChangedEventArgs<T> e)
        {
            if (ItemPropertyChanging != null)
            {
                _itemChangingQueue.Enqueue(e);
                if (ThreadSafeDispatcher == null || ThreadSafeDispatcher.CheckAccess())
                    HandleItemChanging();
                else if (ShouldFireItemPropertyChangingDispatcher())
                    ThreadSafeDispatcher.BeginInvoke(new HandleThreadSafeDelegate(HandleItemChanging));
            }
            HookItemPropertyChanging(e.Item, e.PropertyName);
        }
        #endregion
        #region ActiveItem
        private T _activeItem = default(T);
        public T ActiveItem
        {
            get { return _activeItem; }
            set
            {
                if (!object.Equals(_activeItem, value))
                {
                    _activeItem = value;
                    if (_activeItem != null)
                    {
                        try
                        {
                            lock (_lockObj)
                            {
                                if (!base.Contains(_activeItem))
                                    this.Add(_activeItem);
                            }
                        }
                        catch
                        {
                            this.Add(_activeItem);
                        }
                    }
                    OnPropertyChanged("ActiveItem");
                }
            }
        }
        #endregion
    }

    public class ItemPropertyChangedEventArgs<T> : EventArgs
    {
        public T Item { get; private set; }
        public string PropertyName { get; private set; }

        public ItemPropertyChangedEventArgs(T item, string propertyName)
            : base()
        {
            this.Item = item;
            this.PropertyName = propertyName;
        }
    }
}

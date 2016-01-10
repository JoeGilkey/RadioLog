using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.WPFCommon
{
    /// <summary>
    /// This is a class that implements a Flip-Flop queue.  Useful for adding items to the queue on
    /// one thread, then taking them off on another thread.  While items are being taken off, the other
    /// thread can continue to add items without having to lock the whole object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FlipFlopQueue<T>
    {
        private BatchQueue<T> _queueIn = new BatchQueue<T>();
        private BatchQueue<T> _queueOut = new BatchQueue<T>();
        private object _dequeueLockObj = new object();

        private void FlipQueues()
        {
            BatchQueue<T> tmp = _queueIn;
            _queueIn = _queueOut;
            _queueOut = tmp;
        }

        public void Enqueue(T item)
        {
            if (item == null)
                return;
            _queueIn.Enqueue(item);
        }
        public T Dequeue()
        {
            lock (_dequeueLockObj)
            {
                if (_queueOut.Count == 0)
                {
                    FlipQueues();
                }
                if (_queueOut.Count == 0)
                    return default(T);
                else
                    return _queueOut.Dequeue();
            }
        }
        public T[] DequeueCurrentQueue()
        {
            lock (_dequeueLockObj)
            {
                if (_queueOut.Count == 0)
                {
                    FlipQueues();
                }
                return _queueOut.DequeueCurrentQueue();
            }
        }
    }

    public class BatchQueue<T>
    {
        private Queue<T> _queue = new Queue<T>();

        public int Count { get { return _queue.Count; } }
        public void Enqueue(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
            }
        }

        public T Dequeue()
        {
            if (_queue.Count > 0)
            {
                lock (_queue)
                {
                    return _queue.Dequeue();
                }
            }
            else
                throw new InvalidOperationException("The queue is empty!");
        }
        public T[] DequeueCurrentQueue()
        {
            lock (_queue)
            {
                T[] rslt = _queue.ToArray();
                _queue.Clear();
                return rslt;
            }
        }
    }
}

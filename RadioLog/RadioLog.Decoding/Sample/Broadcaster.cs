using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Sample
{
    public class Broadcaster<T> : Listener<T>
    {
        private List<Listener<T>> mListeners = new List<Listener<T>>();

        public void receive(T t)
        {
            broadcast(t);
        }

        /**
         * Clear listeners to prepare for garbage collection
         */
        public void dispose()
        {
            mListeners.Clear();
        }

        public bool hasListeners()
        {
            return mListeners.Count > 0;
        }

        public int getListenerCount()
        {
            return mListeners.Count;
        }

        public void addListener(Listener<T> listener)
        {
            mListeners.Add(listener);
        }

        public void removeListener(Listener<T> listener)
        {
            mListeners.Remove(listener);
        }

        public void clear()
        {
            mListeners.Clear();
        }

        public void broadcast(T t)
        {
            foreach (Listener<T> listener in mListeners)
            {
                listener.receive(t);
            }
        }
    }
}

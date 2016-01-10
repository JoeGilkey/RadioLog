using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Instrument
{
    public enum TapType
    {
        EVENT_SYNC_DETECT,
        STREAM_BINARY,
        STREAM_COMPLEX,
        STREAM_FLOAT,
        STREAM_SYMBOL
    }

    public abstract class Tap<T>
    {
        protected TapType mType;
        protected String mName;
        protected int mDelay;

        protected List<TapListener<T>> mListeners = new List<TapListener<T>>();

        /**
         * Instrumentation tap.  Provides a tap into a data stream or event stream
         * within a process, so that registered listeners can be notified every
         * time a new data sample is availabe, or an event occurrs.
         * 
         * @param name - displayable name to use for the tap
         * 
         * @param delay - indicates the number of delay units from when the data
         * begins the process until this event occurs.  When this data or event is
         * plotted on a flow graph, the delay value will be used to adjust placement
         * on the display to correctly align events.
         */
        public Tap(TapType type, String name, int delay)
        {
            mType = type;
            mName = name;
            mDelay = delay;
        }

        public TapType getType()
        {
            return mType;
        }

        /**
         * Display name for this tap
         */
        public String getName()
        {
            return mName;
        }

        /**
         * Delay for this tap
         */
        public int getDelay()
        {
            return mDelay;
        }

        /**
         * Registers a listener for data or events produced by this tap
         */
        public void addListener(TapListener<T> listener)
        {
            if (!mListeners.Contains(listener))
            {
                mListeners.Add(listener);
            }
        }

        /**
         * Removes the listener from receiving data or events from this tap
         */
        public void removeListener(TapListener<T> listener)
        {
            mListeners.Remove(listener);
        }

        /**
         * Number of listeners currently registered on this tap
         */
        public int getListenerCount()
        {
            return mListeners.Count;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.DSP.Filter
{
    public abstract class ComplexFilter:IListener<ComplexSample>
    {
        private IListener<ComplexSample> mListener = null;

        public void SetListener(IListener<ComplexSample> listener)
        {
            mListener = listener;
        }

        public IListener<ComplexSample> GetListener()
        {
            return mListener;
        }

        public bool HasListener()
        {
            return mListener != null;
        }

        protected void Send(ComplexSample sample)
        {
            if (mListener != null)
            {
                mListener.Receive(sample);
            }
        }

        public abstract void Receive(ComplexSample sample);
    }
}

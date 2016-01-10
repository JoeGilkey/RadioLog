using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing
{
    public interface IListener<T>
    {
        void Receive(T value);
    }

    public interface IListenerProvider<T>
    {
        void AddListener(IListener<T> value);
        void RemoveListener(IListener<T> value);
        List<IListener<T>> GetListeners();
        int GetListenerCount();
    }

    public interface IListenerOutput<T>
    {
        void SetOutputListener(IListener<T> outListener);
        void RemoveOutputListener();
    }

    public interface ISyncDetectListener
    {
        void SyncDetected();
    }
}

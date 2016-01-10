using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders.Fleetsync2
{
    public class Fleetsync2MessageProcessor : IListener<RadioLog.Common.SafeBitArray>
    {
        private Broadcaster<Message> mBroadcaster = new Broadcaster<Message>();

        public void Receive(Common.SafeBitArray value)
        {
            FleetsyncMessage message = new FleetsyncMessage(value);
            mBroadcaster.Receive(message);
        }

        public void addMessageListener(IListener<Message> listener)
        {
            mBroadcaster.AddListener(listener);
        }
        public void removeMessageListener(IListener<Message> listener)
        {
            mBroadcaster.RemoveListener(listener);
        }
    }
}

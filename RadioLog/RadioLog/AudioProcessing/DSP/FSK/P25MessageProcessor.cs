using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.DSP.FSK
{
    public class P25MessageProcessor : IListener<Message>
    {
        private Broadcaster<Message> mBroadcaster = new Broadcaster<Message>();

        public P25MessageProcessor() { }

        public void Receive(Message value)
        {
            Console.WriteLine("P25 message received!");

            mBroadcaster.Receive(value);
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

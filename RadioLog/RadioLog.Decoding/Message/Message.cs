using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Message
{
    public abstract class Message : RadioLog.Decoding.Controller.Activity.MessageDetailsProvider
    {
        protected long mTimeReceived;
        protected MessageType mType;

        public Message() : this(MessageType.UN_KNWN) { }

        public Message(MessageType type)
        {
            mTimeReceived = DateTime.Now.Ticks;
            mType = type;
        }

        public abstract String toString();
        public abstract String getToID();
        public abstract String getFromID();
        public abstract String getEventType();
        public abstract String getProtocol();
        public abstract String getErrorStatus();
        public abstract String getMessage();
        public abstract String getBinaryMessage();
        public abstract bool isValid();

        public long getTimeReceived()
        {
            return mTimeReceived;
        }

        public DateTime getDateReceived()
        {
            return new DateTime(mTimeReceived);
        }

        public MessageType getType()
        {
            return mType;
        }
    }
}
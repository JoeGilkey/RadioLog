using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Bits
{
    public class MessageFramer : IListener<bool>
    {
        private bool[] mSyncPattern;
        private int mMessageLength;
        private ISyncDetectListener mSyncDetectListener;
        private Broadcaster<RadioLog.Common.SafeBitArray> mBroadcaster = new Broadcaster<RadioLog.Common.SafeBitArray>();
        private List<MessageAssembler> mMessageAssemblers = new List<MessageAssembler>();
        private List<MessageAssembler> mCompletedMessageAssemblers = new List<MessageAssembler>();
        private SyncPatternMatcher mMatcher;

        public MessageFramer(bool[] syncPattern, int messageLength)
        {
            mSyncPattern = syncPattern;
            mMatcher = new SyncPatternMatcher(syncPattern);
            mMessageLength = messageLength;
        }

        public void Receive(bool bit)
        {
            mMatcher.receive(bit);

            List<MessageAssembler> toRemove = new List<MessageAssembler>();
            foreach (MessageAssembler assembler in mMessageAssemblers)
            {
                if (assembler.complete())
                {
                    assembler.dispose();
                    toRemove.Add(assembler);
                }
                else
                {
                    assembler.Receive(bit);
                }
            }
            foreach (MessageAssembler assembler in toRemove)
            {
                mMessageAssemblers.Remove(assembler);
            }

            if (mMatcher.matches())
            {
                addMessageAssembler(new MessageAssembler(this, mMessageLength, mSyncPattern));
                if (mSyncDetectListener != null)
                {
                    mSyncDetectListener.SyncDetected();
                }
            }
        }

        public void flush()
        {
            foreach(MessageAssembler assembler in mMessageAssemblers)
            {
                assembler.flush();
            }
        }

        public void setSyncDetectListener(ISyncDetectListener listener)
        {
            mSyncDetectListener = listener;
        }

        public void addMessageListener(IListener<RadioLog.Common.SafeBitArray> listener)
        {
            mBroadcaster.AddListener(listener);
        }

        public void removeMessageListener(IListener<RadioLog.Common.SafeBitArray> listener)
        {
            mBroadcaster.RemoveListener(listener);
        }

        private void addMessageAssembler(MessageAssembler assembler)
        {
            mMessageAssemblers.Add(assembler);
        }

        private void removeMessageAssembler(MessageAssembler assembler)
        {
            mMessageAssemblers.Remove(assembler);
        }

        private class MessageAssembler : IListener<bool>
        {
            MessageFramer mFramer;
            RadioLog.Common.SafeBitArray mMessage;
            bool mComplete = false;

            public MessageAssembler(MessageFramer framer, int messageLength)
            {
                mFramer = framer;
                mMessage = new RadioLog.Common.SafeBitArray(messageLength);
            }

            public MessageAssembler(MessageFramer framer, int messageLength, bool[] initialFill)
                : this(framer, messageLength)
            {
                /* Pre-load the message with the sync pattern */
                for (int x = 0; x < initialFill.Length; x++)
                {
                    Receive(initialFill[x]);
                }
            }

            public void dispose()
            {
                mMessage = null;
            }

            public void Receive(Boolean bit)
            {
                mMessage.Add(bit);

                /* Once our message is complete (ie full), send it to all registered
                 * message listeners, and set complete flag so for auto-removal */
                if (mMessage.IsFull())
                {
                    mComplete = true;
                    flush();
                }
            }

            /**
             * Flushes/Sends the current message, or partial message, and sets 
             * complete flag to true, so that we can be auto-removed
             */
            public void flush()
            {
                mFramer.mBroadcaster.Receive(mMessage);
                mComplete = true;
            }

            /**
             * Flag to indicate when this assembler has received all of the bits it
             * is looking for (ie message length), and should then be removed from
             * receiving any more bits
             */
            public bool complete()
            {
                return mComplete;
            }
        }
    }
}

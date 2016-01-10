using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Bits
{
    /**
 * MessageFramer - processes bitsets looking for a sync pattern within
 * the bits, and then extracts the message, including the sync 
 * pattern, for a total bit length of messageLength.
 * 
 * Will extract multiple messages simultaneously, for each sync pattern that is
 * encountered within the bitset bit stream.
 */
    public class MessageFramer : Sample.Listener<bool>, DSP.SyncDetectProvider
    {
        private bool[] mSyncPattern;
        private int mMessageLength;
        private DSP.SyncDetectListener mSyncDetectListener;
        private Sample.Broadcaster<BitSetBuffer> mBroadcaster = new Sample.Broadcaster<BitSetBuffer>();
        private List<MessageAssembler> mMessageAssemblers = new List<MessageAssembler>();

        private List<MessageAssembler> mCompletedMessageAssemblers = new List<MessageAssembler>();

        private BitSetBuffer mPreviousBuffer = null;

        private SyncPatternMatcher mMatcher;

        public MessageFramer(bool[] syncPattern, int messageLength)
        {
            mSyncPattern = syncPattern;
            mMatcher = new SyncPatternMatcher(syncPattern);
            mMessageLength = messageLength;
        }

        public void dispose()
        {
            mBroadcaster.dispose();
            mCompletedMessageAssemblers.Clear();
            mMessageAssemblers.Clear();
        }

        public void receive(bool bit)
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
                    assembler.receive(bit);
                }
            }
            foreach (MessageAssembler assembler in toRemove)
            {
                mMessageAssemblers.Remove(assembler);
            }

            /* Check for sync match and add new message assembler */
            if (mMatcher.matches())
            {
                addMessageAssembler(new MessageAssembler(mMessageLength, mSyncPattern));

                /* Notify any sync detect listener(s) */
                if (mSyncDetectListener != null)
                {
                    mSyncDetectListener.syncDetected();
                }
            }
        }

        /**
         * Causes all messages currently under assembly to be forcibly
         * sent (ie flushed) to all registered message listeners, and
         * subsequently, all assemblers to be deleted
         */
        public void flush()
        {
            foreach (MessageAssembler assembler in mMessageAssemblers)
            {
                assembler.flush();
            }
        }

        public void setSyncDetectListener(DSP.SyncDetectListener listener)
        {
            mSyncDetectListener = listener;
        }

        /**
         * Allow a message listener to register with this framer to receive
         * all framed messages
         */
        public void addMessageListener(Sample.Listener<BitSetBuffer> listener)
        {
            mBroadcaster.addListener(listener);
        }

        public void removeMessageListener(Sample.Listener<BitSetBuffer> listener)
        {
            mBroadcaster.removeListener(listener);
        }

        /*
         * Adds a message assembler to receive bits from the bit stream
         */
        private void addMessageAssembler(MessageAssembler assembler)
        {
            mMessageAssemblers.Add(assembler);
        }

        private void removeMessageAssembler(MessageAssembler assembler)
        {
            mMessageAssemblers.Remove(assembler);
        }

        /**
         * Assembles a binary message, starting with the initial fill of the 
         * identified sync pattern, and every bit thereafter.  Once the accumulated
         * bits equal the message length, the message is sent and the assembler
         * flags itself as complete.
         * 
         * By design, multiple message assemblers can exist at the same time, each
         * assembling different, overlapping potential messages
         */
        private class MessageAssembler : Sample.Listener<Boolean>
        {
            BitSetBuffer mMessage;
            bool mComplete = false;
            MessageFramer _framer;

            MessageAssembler(MessageFramer framer, int messageLength)
            {
                _framer = framer;
                mMessage = new BitSetBuffer(messageLength);
            }

            MessageAssembler(MessageFramer framer, int messageLength, bool[] initialFill)
                : this(framer, messageLength)
            {
                /* Pre-load the message with the sync pattern */
                for (int x = 0; x < initialFill.Length; x++)
                {
                    receive(initialFill[x]);
                }
            }

            public void dispose()
            {
                mMessage = null;
            }

            public void receive(Boolean bit)
            {
                try
                {
                    mMessage.add(bit);
                }
                catch (BitSetFullException e)
                {
                    //e.printStackTrace();
                }

                /* Once our message is complete (ie full), send it to all registered
                 * message listeners, and set complete flag so for auto-removal */
                if (mMessage.isFull())
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
                if (_framer != null)
                {
                    _framer.mBroadcaster.receive(mMessage);
                }
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.DSP.FSK
{
    public class P25MessageFramer : IListener<C4FMSymbol>
    {
        private static readonly int[] STATUS_BITS = new int[] { 22, 92, 162, 232, 302, 372, 442, 512, 582, 652, 722, 792, 862, 932 };
        private List<P25MessageAssembler> mAssemblers = new List<P25MessageAssembler>();

        public static readonly int TSBK_BEGIN = 64;
        public static readonly int TSBK_END = 260;
        public static readonly int TSBK_DECODED_END = 162;

        private IListener<Message> mListener;
        private Bits.SyncPatternMatcher mMatcher;
        private bool mInverted = false;
        private Decoders.P25.TrellisHalfRate mHalfRate = new Decoders.P25.TrellisHalfRate();

        public P25MessageFramer(long sync, int messageLength, bool inverted)
        {
            mMatcher = new Bits.SyncPatternMatcher(sync);
            mInverted = inverted;
            mAssemblers.Add(new P25MessageAssembler(this));
            mAssemblers.Add(new P25MessageAssembler(this));
        }

        private void dispatch(Message message)
        {
            if (mListener != null)
            {
                mListener.Receive(message);
            }
        }

        public void Receive(C4FMSymbol symbol)
        {
            C4FMSymbol correctedSymbol = symbol;
            mMatcher.receive(correctedSymbol.getBit1());
            mMatcher.receive(correctedSymbol.getBit2());

            foreach (P25MessageAssembler assembler in mAssemblers)
            {
                if (assembler.isActive())
                {
                    assembler.receive(correctedSymbol);

                    if (assembler.complete())
                    {
                        assembler.reset();
                    }
                }
            }

            if (mMatcher.matches())
            {
                bool found = false;
                foreach (P25MessageAssembler assembler in mAssemblers)
                {
                    if (!assembler.isActive())
                    {
                        assembler.setActive(true);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    //no inactive C4FM message assemblers available...
                }
            }
        }

        public void setListener(IListener<Message> listener)
        {
            mListener = listener;
        }
        public void removeListener()
        {
            mListener = null;
        }

        private class P25MessageAssembler
        {
            private P25MessageFramer mFramer;
            private int mStatusIndicatorPointer = 0;
            private RadioLog.Common.SafeBitArray mMessage;
            private int mMessageLength;
            private bool mComplete = false;
            private bool mActive = false;
            private Decoders.P25.Reference.DataUnitID mDUID = Decoders.P25.Reference.DataUnitID.NID;

            public P25MessageAssembler(P25MessageFramer framer)
            {
                mFramer = framer;
                mMessageLength = mDUID.getMessageLength();
                mMessage = new RadioLog.Common.SafeBitArray(mMessageLength);
                reset();
            }

            public void receive(C4FMSymbol symbol)
            {
                if (mActive)
                {
                    /* Throw away status bits that are injected every 70 bits */
                    if (mMessage.pointer() == STATUS_BITS[mStatusIndicatorPointer])
                    {
                        mStatusIndicatorPointer++;
                    }
                    else
                    {
                            mMessage.Add(symbol.getBit1());
                            mMessage.Add(symbol.getBit2());
                    
                        /* Check the message for complete */
                        if (mMessage.IsFull())
                        {
                            checkComplete();
                        }
                    }
                }
            }

            public void reset()
            {
                mDUID = Decoders.P25.Reference.DataUnitID.NID;
                mMessage.SetSize(mDUID.getMessageLength());
                mMessage.ClearAll();
                mStatusIndicatorPointer = 0;
                mComplete = false;
                mActive = false;
            }

            public void setActive(bool active)
            {
                mActive = active;
            }

            private void setDUID(Decoders.P25.Reference.DataUnitID id)
            {
                mDUID = id;
                mMessageLength = id.getMessageLength();
                mMessage.SetSize(mMessageLength);
            }

            private void checkComplete()
            {
                if (mDUID.Equals(Decoders.P25.Reference.DataUnitID.NID))
                {
                    int value = mMessage.getInt(Decoders.P25.Message.P25Message.DUID);

                    Decoders.P25.Reference.DataUnitID duid = Decoders.P25.Reference.DataUnitID.fromValue(value);

                    if (duid != Decoders.P25.Reference.DataUnitID.UNKN)
                    {
                        setDUID(duid);
                    }
                    else
                    {
                        mComplete = true;
                    }
                }
                else if (mDUID.Equals(Decoders.P25.Reference.DataUnitID.HDU) ||
                    mDUID.Equals(Decoders.P25.Reference.DataUnitID.LDU1) ||
                    mDUID.Equals(Decoders.P25.Reference.DataUnitID.LDU2) ||
                    mDUID.Equals(Decoders.P25.Reference.DataUnitID.PDU2) ||
                    mDUID.Equals(Decoders.P25.Reference.DataUnitID.PDU3) ||
                    mDUID.Equals(Decoders.P25.Reference.DataUnitID.TDU) ||
                    mDUID.Equals(Decoders.P25.Reference.DataUnitID.TDULC) ||
                    mDUID.Equals(Decoders.P25.Reference.DataUnitID.UNKN))
                {
                    mComplete = true;
                    mFramer.dispatch(new Decoders.P25.Message.P25Message(mMessage.Clone(), mDUID));
                }
                else if (mDUID.Equals(Decoders.P25.Reference.DataUnitID.PDU1))
                {
                    int blocks = mMessage.getInt(Decoders.P25.Message.PDUMessage.PDU_HEADER_BLOCKS_TO_FOLLOW);
                    int padBlocks = mMessage.getInt(Decoders.P25.Message.PDUMessage.PDU_HEADER_PAD_BLOCKS);

                    int blockCount = blocks + padBlocks;

                    if (blockCount == 24 || blockCount == 32)
                    {
                        setDUID(Decoders.P25.Reference.DataUnitID.PDU2);
                    }
                    else if (blockCount == 36 || blockCount == 48)
                    {
                        setDUID(Decoders.P25.Reference.DataUnitID.PDU3);
                    }
                    else
                    {
                        mFramer.dispatch(new Decoders.P25.Message.PDUMessage(mMessage.Clone(), mDUID));
                        mComplete = true;
                    }
                }
                else if (mDUID.Equals(Decoders.P25.Reference.DataUnitID.TSBK1))
                {
                    /* Remove interleaving */
                    Decoders.P25.P25Interleave.deinterleave(mMessage, TSBK_BEGIN, TSBK_END);

                    /* Remove trellis encoding */
                    mFramer.mHalfRate.decode(mMessage, TSBK_BEGIN, TSBK_END);

                    /* Construct the message */
                    int tsbkSystem1 = mMessage.getInt(Decoders.P25.Message.P25Message.NAC);

                    RadioLog.Common.SafeBitArray tsbkBuffer1 = new RadioLog.Common.SafeBitArray(mMessage.CloneFromIndexToIndex(TSBK_BEGIN, TSBK_DECODED_END), 96);

                    Decoders.P25.Message.TSBKMessage tsbkMessage1 = Decoders.P25.Message.TSBKMessageFactory.getMessage(tsbkSystem1, tsbkBuffer1);

                    if (tsbkMessage1.isLastBlock())
                    {
                        mComplete = true;
                    }
                    else
                    {
                        setDUID(Decoders.P25.Reference.DataUnitID.TSBK2);
                        mMessage.SetPointer(TSBK_BEGIN);
                    }

                    mFramer.dispatch(tsbkMessage1);
                }
                else if (mDUID.Equals(Decoders.P25.Reference.DataUnitID.TSBK2))
                {
                    /* Remove interleaving */
                    Decoders.P25.P25Interleave.deinterleave(mMessage, TSBK_BEGIN, TSBK_END);

                    /* Remove trellis encoding */
                    mFramer.mHalfRate.decode(mMessage, TSBK_BEGIN, TSBK_END);

                    /* Construct the message */
                    int tsbkSystem2 = mMessage.getInt(Decoders.P25.Message.P25Message.NAC);

                    RadioLog.Common.SafeBitArray tsbkBuffer2 = new RadioLog.Common.SafeBitArray(mMessage.CloneFromIndexToIndex(TSBK_BEGIN, TSBK_DECODED_END), 98);

                    Decoders.P25.Message.TSBKMessage tsbkMessage2 = Decoders.P25.Message.TSBKMessageFactory.getMessage(tsbkSystem2, tsbkBuffer2);

                    if (tsbkMessage2.isLastBlock())
                    {
                        mComplete = true;
                    }
                    else
                    {
                        setDUID(Decoders.P25.Reference.DataUnitID.TSBK3);
                        mMessage.SetPointer(TSBK_BEGIN);
                    }

                    mFramer.dispatch(tsbkMessage2);
                }
                else if (mDUID.Equals(Decoders.P25.Reference.DataUnitID.TSBK3))
                {
                    /* Remove interleaving */
                    Decoders.P25.P25Interleave.deinterleave(mMessage, TSBK_BEGIN, TSBK_END);

                    /* Remove trellis encoding */
                    mFramer.mHalfRate.decode(mMessage, TSBK_BEGIN, TSBK_END);

                    /* Construct the message */
                    int tsbkSystem3 = mMessage.getInt(Decoders.P25.Message.P25Message.NAC);

                    RadioLog.Common.SafeBitArray tsbkBuffer3 = new RadioLog.Common.SafeBitArray(mMessage.CloneFromIndexToIndex(TSBK_BEGIN, TSBK_DECODED_END), 96);

                    Decoders.P25.Message.TSBKMessage tsbkMessage3 = Decoders.P25.Message.TSBKMessageFactory.getMessage(tsbkSystem3, tsbkBuffer3);

                    mComplete = true;

                    mFramer.dispatch(tsbkMessage3);
                }
                else
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

            public bool isActive()
            {
                return mActive;
            }
        }
    }
}

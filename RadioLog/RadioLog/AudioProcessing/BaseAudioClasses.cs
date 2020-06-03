using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing
{
    public class SymbolEvent
    {
        private Common.SafeBitArray mBitset;
        private int mSamplesPerSymbol;
        private bool mDecision;
        private Shift mShift;

        public SymbolEvent(Common.SafeBitArray bitset, int samplesPerSymbol, bool decision, Shift shift)
        {
            mBitset = bitset;
            mSamplesPerSymbol = samplesPerSymbol;
            mDecision = decision;
            mShift = shift;
        }

        public Common.SafeBitArray getBitSet()
        {
            return mBitset;
        }

        public int getSamplesPerSymbol()
        {
            return mSamplesPerSymbol;
        }

        public bool getDecision()
        {
            return mDecision;
        }

        public Shift getShift()
        {
            return mShift;
        }
    }

    public class ComplexSample
    {
        private float mLeft;
        private float mRight;

        public ComplexSample(float left, float right)
        {
            mLeft = left;
            mRight = right;
        }

        public float Left { get { return mLeft; } set { mLeft = value; } }
        public float Right { get { return mRight; } set { mRight = value; } }
        public float X { get { return mLeft; } }
        public float Y { get { return mRight; } }
        public float Real { get { return mLeft; } }
        public float Imaginery { get { return mRight; } }

        public ComplexSample Copy() { return new ComplexSample(mLeft, mRight); }
        public ComplexSample Conjugate() { return new ComplexSample(mLeft, -mRight); }

        public void Multiply(float scalar)
        {
            mLeft *= scalar;
            mRight *= scalar;
        }
        public void Multiply(ComplexSample multiplier)
        {
            float inphase = (InPhase() * multiplier.InPhase()) -
                            (Quadrature() * multiplier.Quadrature());

            float quadrature = (Quadrature() * multiplier.InPhase()) +
                               (InPhase() * multiplier.Quadrature());

            mLeft = inphase;
            mRight = quadrature;
        }

        public static ComplexSample Multiply(ComplexSample sample1, ComplexSample sample2)
        {
            float inphase = (sample1.InPhase() * sample2.InPhase()) -
            (sample1.Quadrature() * sample2.Quadrature());

            float quadrature = (sample1.Quadrature() * sample2.InPhase()) +
               (sample1.InPhase() * sample2.Quadrature());

            return new ComplexSample(inphase, quadrature);
        }

        public static ComplexSample Multiply(ComplexSample sample, float left, float right)
        {
            float l = (sample.Left * left) - (sample.Right * right);
            float r = (sample.Right * left) + (sample.Left * right);

            return new ComplexSample(l, r);
        }

        public void Add(ComplexSample adder)
        {
            mLeft += adder.Left;
            mRight += adder.Right;
        }

        public float Magnitude()
        {
            return (float)Math.Sqrt(MagnitudeSquared());
        }
        public float MagnitudeSquared()
        {
            return (float)((InPhase() * InPhase()) +
                          (Quadrature() * Quadrature()));
        }
        public void Normalize()
        {
            Multiply((float)(1.0f / Magnitude()));
        }
        public void FastNormalize()
        {
            Multiply((float)(1.95f - MagnitudeSquared()));
        }
        public float InPhase()
        {
            return mLeft;
        }
        public float Quadrature()
        {
            return mRight;
        }
        public float MaximumAbsolute()
        {
            if (Math.Abs(mLeft) > Math.Abs(mRight))
            {
                return Math.Abs(mLeft);
            }
            else
            {
                return Math.Abs(mRight);
            }
        }
        public static ComplexSample fromAngle(float angle)
        {
            return new ComplexSample((float)Math.Cos(angle),
                                      (float)Math.Sin(angle));
        }
    }

    public abstract class Message
    {
        protected long mTimeReceived;
        protected MessageType mType;

        public Message() : this(MessageType.UN_KNWN) { }

        public Message(MessageType type)
        {
            mTimeReceived = DateTime.Now.Ticks;
            mType = type;
        }

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

        public abstract string GetFormatName();
        public abstract string GetUnitId();
        public abstract string GetDescription();
        public abstract Common.SignalCode GetSignalCode();
    }

    public abstract class Instrumentable<T> : IListenerProvider<T>
    {
        protected List<IListener<T>> mListeners = new List<IListener<T>>();

        /**
         * Registers a listener for data or events produced by this tap
         */
        public void AddListener(IListener<T> listener)
        {
            if (!mListeners.Contains(listener))
            {
                mListeners.Add(listener);
            }
        }

        /**
         * Removes the listener from receiving data or events from this tap
         */
        public void RemoveListener(IListener<T> listener)
        {
            mListeners.Remove(listener);
        }

        /**
         * Number of listeners currently registered on this tap
         */
        public int GetListenerCount()
        {
            return mListeners.Count;
        }

        public virtual List<IListener<T>> GetListeners()
        {
            return mListeners;
        }
    }

    public class Broadcaster<T> : Instrumentable<T>, IListener<T>
    {
        public void Broadcast(T value)
        {
            foreach (IListener<T> bcast in this.GetListeners())
            {
                bcast.Receive(value);
            }
        }

        public void Receive(T value)
        {
            Broadcast(value);
        }
    }
    public class RealSampleBroadcaster : Broadcaster<float> { }

    public abstract class Tap<T> : Instrumentable<T>
    {
        protected TapType mType;
        protected string mName;
        protected int mDelay;

        public Tap(TapType tapType, string name, int delay)
        {
            mType = tapType;
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
    }

    public abstract class Decoder: IListener<Message>
    {
        private Broadcaster<Message> mMessageBroadcaster = new Broadcaster<Message>();
        private RealSampleBroadcaster mRealBroadcaster = new RealSampleBroadcaster();
        private Broadcaster<ComplexSample> mComplexBroadcaster = new Broadcaster<ComplexSample>();
        protected SampleType mSourceSampleType;
        protected List<Decoder> mAuxiliaryDecoders = new List<Decoder>();

        public Decoder(SampleType sampleType)
        {
            mSourceSampleType = sampleType;
        }

        public virtual IListener<float> getRealReceiver() { return mRealBroadcaster; }
        public Broadcaster<ComplexSample> getComplexReceiver() { return mComplexBroadcaster; }
        public abstract DecoderType getDecoderType();
        
        public void addAuxiliaryDecoder(Decoder decoder)
        {
            mAuxiliaryDecoders.Add(decoder);
            mRealBroadcaster.AddListener(decoder.getRealReceiver());
            decoder.addMessageListener(this);
        }

        public List<Decoder> getAuuxiliaryDecoders() { return mAuxiliaryDecoders; }

        public void Receive(Message value)
        {
            Send(value);
        }

        public void Send(Message message)
        {
            if (mMessageBroadcaster != null)
            {
                mMessageBroadcaster.Receive(message);
            }
        }

        public void addMessageListener(IListener<Message> listener)
        {
            mMessageBroadcaster.AddListener(listener);
        }
        public void removeMessageListener(IListener<Message> listener)
        {
            mMessageBroadcaster.RemoveListener(listener);
        }

        public virtual void addUnfilteredRealSampleListener(IListener<float> listener) { }

        public void addRealSampleListener(IListener<float> listener)
        {
            mRealBroadcaster.AddListener(listener);
        }
        public void removeRealSampleListener(IListener<float> listener)
        {
            mRealBroadcaster.RemoveListener(listener);
        }

        public void addComplexListener(IListener<ComplexSample> listener)
        {
            mComplexBroadcaster.AddListener(listener);
        }
        public void removeComplexListener(IListener<ComplexSample> listener)
        {
            mComplexBroadcaster.RemoveListener(listener);
        }

        public void ProcessAudioSamples(float[] samples, int numSamples, bool bHasSound)
        {
            IListener<float> rcvr = this.getRealReceiver();
            for (int i = 0; i < numSamples; i++)
                rcvr.Receive(samples[i]);
        }
    }
}
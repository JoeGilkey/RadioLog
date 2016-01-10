using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Decode
{
    public abstract class Decoder : Sample.Listener<Message.Message>
    {
        private Sample.Broadcaster<Message.Message> mMessageBroadcaster = new Sample.Broadcaster<Message.Message>();
        private Sample.Real.RealSampleBroadcaster mRealBroadcaster = new Sample.Real.RealSampleBroadcaster();
        private Sample.Broadcaster<Sample.Complex.ComplexSample> mComplexBroadcaster = new Sample.Broadcaster<Sample.Complex.ComplexSample>();
        protected Source.SampleType mSourceSampleType;
        protected List<Decoder> mAuxiliaryDecoders = new List<Decoder>();

        public Decoder(Source.SampleType sampleType)
        {
            mSourceSampleType = sampleType;
        }

        /**
	 * Returns a real (ie demodulated) sample listener interface for 
	 * connecting this decoder to a real sample stream provider
	 */
        public Sample.Real.RealSampleBroadcaster getRealReceiver()
        {
            return mRealBroadcaster;
        }

        /**
         * Returns a complex listener interface for connecting this decoder to a 
         * float stream provider
         */
        public Sample.Listener<Sample.Complex.ComplexSample> getComplexReceiver()
        {
            return (Sample.Listener<Sample.Complex.ComplexSample>)mComplexBroadcaster;
        }

        /**
         * Returns the primary decoder type for this decoder
         */
        public abstract DecoderType getType();

        /**
         * Cleanup method.  Invoke this method after stop and before delete.
         */
        public virtual void dispose()
        {
            foreach (Decoder auxiliaryDecoder in mAuxiliaryDecoders)
            {
                auxiliaryDecoder.dispose();
            }

            mAuxiliaryDecoders.Clear();

            mMessageBroadcaster.clear();
            mComplexBroadcaster.clear();
            mRealBroadcaster.clear();
        }

        /**
         * Adds the auxiliary decoder (piggyback) to this decoder.  
         * 
         * Note: we assume that the auxiliary decoder is designed to receive 
         * demodulated samples, thus we automatically register the aux decoder to 
         * receive the demodulated sample stream.
         * 
         * Registers this decoder to receive the message output stream from the 
         * auxiliary decoder, so that those messages can be echoed and included in 
         * the consolidated message stream to all message listeners registered on 
         * the primary decoder.
         */
        public void addAuxiliaryDecoder(Decoder decoder)
        {
            mAuxiliaryDecoders.Add(decoder);
            mRealBroadcaster.addListener(decoder.getRealReceiver());
            decoder.addMessageListener(this);
        }

        /**
         * Returns a list of all auxiliary decoders that have been added to this
         * processing chain
         */
        public List<Decoder> getAuxiliaryDecoders()
        {
            return mAuxiliaryDecoders;
        }

        /**
         * Main receiver method for all demodulators to send their decoded messages
         * so that they will be broadcast to all registered listeners
         */
        public void receive(Message.Message message)
        {
            send(message);
        }

        /**
         * Broadcasts the message to all registered listeners
         */
        public void send(Message.Message message)
        {
            if (mMessageBroadcaster != null)
            {
                mMessageBroadcaster.receive(message);
            }
        }

        /**
         * Adds a listener to receiving decoded messages from all attached decoders
         */
        public void addMessageListener(Sample.Listener<Message.Message> listener)
        {
            mMessageBroadcaster.addListener(listener);
        }

        /**
         * Removes the listener from receiving decoded messages from all attached
         * decoders
         */
        public void removeMessageListener(Sample.Listener<Message.Message> listener)
        {
            mMessageBroadcaster.removeListener(listener);
        }

        /**
         * Adds a real sample listener to receive unfiltered demodulated samples
         */
        public abstract void addUnfilteredRealSampleListener(Sample.Real.RealSampleBroadcaster listener);

        /**
         * Adds a real sample listener to receive demodulated samples
         */
        public void addRealSampleListener(Sample.Real.RealSampleBroadcaster listener)
        {
            mRealBroadcaster.addListener(listener);
        }

        /**
         * Remove the real sample listener from receiving demodulated samples
         */
        public void removeRealListener(Sample.Real.RealSampleBroadcaster listener)
        {
            mRealBroadcaster.removeListener(listener);
        }

        /**
         * Adds a complex (I/Q) sample listener to receive copy of the inbound
         * complex sample stream
         */
        public void addComplexListener(Sample.Listener<Sample.Complex.ComplexSample> listener)
        {
            mComplexBroadcaster.addListener(listener);
        }

        /**
         * Removes the complex (I/Q) sample listener from receiving samples
         */
        public void removeComplexListener(Sample.Listener<Sample.Complex.ComplexSample> listener)
        {
            mComplexBroadcaster.removeListener(listener);
        }
    }
}
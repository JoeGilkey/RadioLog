using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders
{
    public class RootDecoder : IListener<Message>
    {
        public delegate void RootDecoderDelegate(RadioLog.Common.SignalCode sigCode, string format, string unitId, string desc);

        private List<Decoder> _decoders = new List<Decoder>();
        private RootDecoderDelegate _decoderDelegate;
        private int _sampleRate;

        public RootDecoder(int sampleRate, bool decodeFleetSync2, bool decodeP25, RootDecoderDelegate decoderDelegate)
        {
            _sampleRate = sampleRate;
            _decoderDelegate = decoderDelegate;

            if (decodeFleetSync2)
            {
                _decoders.Add(new Fleetsync2Decoder(sampleRate));
            }
            //if (decodeP25)
            //{
            //    _decoders.Add(new P25Decoder(sampleRate, SampleType.REAL));
            //}

            foreach (Decoder decoder in _decoders)
            {
                decoder.addMessageListener(this);
            }
        }

        private bool isDecoderTypePresent(DecoderType dType)
        {
            foreach (Decoder decoder in _decoders)
            {
                if (decoder.getDecoderType() == dType)
                    return true;
            }
            return false;
        }
        private void RemoveDecoderOfType(DecoderType dType)
        {
            List<Decoder> toRemove = new List<Decoder>();
            foreach (Decoder decoder in _decoders)
            {
                if (decoder.getDecoderType() == dType)
                    toRemove.Add(decoder);
            }
            foreach (Decoder decoder in toRemove)
            {
                _decoders.Remove(decoder);
            }
        }

        public void UpdateDecodeFleetSync(bool decode)
        {
            if (decode)
            {
                if (!isDecoderTypePresent(DecoderType.FLEETSYNC2))
                    _decoders.Add(new Fleetsync2Decoder(_sampleRate));
            }
            else
            {
                RemoveDecoderOfType(DecoderType.FLEETSYNC2);
            }
        }
        public void UpdateDecodeP25(bool decode)
        {
            //if (decode)
            //{
            //    if (!isDecoderTypePresent(DecoderType.P25_PHASE1))
            //    {
            //        _decoders.Add(new P25Decoder(_sampleRate, SampleType.REAL));
            //    }
            //}
            //else
            //{
            //    RemoveDecoderOfType(DecoderType.P25_PHASE1);
            //}
        }

        public void ProcessSamples(float[] samples, int numSamples, bool bHasSound)
        {
            foreach (Decoder decoder in _decoders)
            {
                decoder.ProcessAudioSamples(samples, numSamples, bHasSound);
            }
        }

        public void Receive(Message value)
        {
            if (value == null)
                return;
            if (_decoderDelegate != null)
            {
                _decoderDelegate(value.GetSignalCode(), value.GetFormatName(), value.GetUnitId(), value.GetDescription());
            }
        }
    }
}

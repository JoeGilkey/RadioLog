//#define ZEROCROSSING
#define DIFFERENTIATOR
#define MDC_SAMPLE_FORMAT_FLOAT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//http://batboard.batlabs.com/viewtopic.php?t=36459

namespace RadioLog.AudioProcessing.Decoders
{
    public class MDC1200
    {
        const int MDC_ND = 5;
        const int MDC_GDTHRESH = 5;

        private string _sourceName = string.Empty;
        private bool _buffersAreClear = false;

        #region OpCodes
        /**
         * Single Packets
         */
        /// <summary>
        /// Emergency
        /// </summary>
        /// <remarks>
        /// Supports the following arguments:
        ///     PTT_PRE (0x80)
        ///     EMERG_UNKNW (0x81)
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Outbound
        /// </remarks>
        public const byte EMERGENCY = 0x00;
        /// <summary>
        /// Emergency Acknowledge
        /// </summary>
        /// <remarks>
        /// Has no arguments, should always have NO_ARG.
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Inbound
        /// </remarks>
        public const byte EMERGENCY_ACK = 0x20;

        /// <summary>
        /// PTT ID
        /// </summary>
        /// <remarks>
        /// Supports the following arguments:
        ///     NO_ARG (0x00) value indicates Post- ID
        ///     PTT_PRE (0x80) value indicated Pre- ID
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Inbound
        /// </remarks>
        public const byte PTT_ID = 0x01;

        /// <summary>
        /// Radio Check
        /// </summary>
        /// <remarks>
        /// Always takes the argument RADIO_CHECK (0x85).
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = Ack
        ///     6 = Inbound/Outbound Packet = Outbound
        /// </remarks>
        public const byte RADIO_CHECK = 0x63;
        /// <summary>
        /// Radio Check Acknowledge
        /// </summary>
        /// <remarks>
        /// Has no arguments, should always have NO_ARG.
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Inbound
        /// </remarks>
        public const byte RADIO_CHECK_ACK = 0x03;

        /// <summary>
        /// Message
        /// </summary>
        /// <remarks>
        /// Argument contains the ID of the given message. This opcode
        /// optionally supports setting of bit 7 for required acknowledgement
        /// of receipt.
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack (or) Ack
        ///     6 = Inbound/Outbound Packet = Inbound
        /// </remarks>
        public const byte MESSAGE = 0x07;
        /// <summary>
        /// Message Acknowledge
        /// </summary>
        /// <remarks>
        /// Always takes the argument NO_ARG.
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Outbound
        /// </remarks>
        public const byte MESSAGE_ACK = 0x23;

        /// <summary>
        /// Status Request
        /// </summary>
        /// <remarks>
        /// Always takes the argument STATUS_REQ (0x06).
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Outbound
        /// </remarks>
        public const byte STATUS_REQUEST = 0x22;

        /// <summary>
        /// Status Response
        /// </summary>
        /// <remarks>
        /// Argument contains the ID of the given status.
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Inbound
        /// </remarks>
        public const byte STATUS_RESPONSE = 0x06;

        /// <summary>
        /// Remote Monitor
        /// </summary>
        /// <remarks>
        /// Always takes the argument REMOTE_MONITOR (0x8A).
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Inbound
        /// </remarks>
        public const byte REMOTE_MONITOR = 0x11;

        /// <summary>
        /// Selective Radio Inhibit
        /// </summary>
        /// <remarks>
        /// Supports the following arguments:
        ///     NO_ARG (0x00) value indicates Unit ID to inhibit
        ///     CANCEL_INHIBIT (0x0C) value indicates Unit ID to uninhibit
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Outbound
        /// </remarks>
        public const byte RADIO_INHIBIT = 0x2B;

        /// <summary>
        /// Selective Radio Inhibit Acknowledge
        /// </summary>
        /// <remarks>
        /// Supports the following arguments:
        ///     NO_ARG (0x00) value indicates the Unit ID acknowledges the inhibit
        ///     CANCEL_INHIBIT (0x0C) value indicates the Unit ID acknowledges is uninhibited
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Inbound
        /// </remarks>
        public const byte RADIO_INHIBIT_ACK = 0x0B;

        /// <summary>
        /// Request to Talk
        /// </summary>
        /// <remarks>
        /// This operand is doubled to 0x41?
        /// 
        /// Always takes the argument RTT (0x01).
        /// 
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = Ack
        ///     6 = Inbound/Outbound Packet = Inbound
        /// </remarks>
        public const byte RTT_1 = 0x40;
        public const byte RTT_2 = 0x41;

        /// <summary>
        /// Request to Talk Acknowledge
        /// </summary>
        /// <remarks>
        /// Has no arguments, should always have NO_ARG.
        /// 
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Outbound
        /// </remarks>
        public const byte RTT_ACK = 0x23;

        /**
         * Double Packets
         */
        /// <summary>
        /// Double Packet Operation (0x35)
        /// </summary>
        /// <remarks>
        /// Supports the following arguments:
        ///     DOUBLE_PACKET_FROM (0x89) value indicates who transmitted the double packet
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Outbound
        /// </remarks>
        public const byte DOUBLE_PACKET_TYPE1 = 0x35;
        /// <summary>
        /// Double Packet Operation (0x55)
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Command
        ///     7 = Ack/No Ack Required     = Ack
        ///     6 = Inbound/Outbound Packet = Inbound
        /// </summary>
        public const byte DOUBLE_PACKET_TYPE2 = 0x55;

        /// <summary>
        /// Call Alert/Page
        /// </summary>
        /// <remarks>
        /// Supports the following arguments:
        ///     CALL_ALERT_TO (0x0D) value indicates the intended target of the call
        ///     
        /// The DOUBLE_PACKET_FROM (0x89) of the DOUBLE_PACKET_TYPE1 frame will contain the unit ID that transmitted
        /// the call alert. This opcode expects an ack, regardless of the bit 7 setting.
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Data
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Inbound
        /// </remarks>
        public const byte CALL_ALERT_ACK_EXPECTED = 0x83;
        /// <summary>
        /// Call Alert/Page
        /// </summary>
        /// <remarks>
        /// Supports the following arguments:
        ///     CALL_ALERT_TO (0x0D) value indicates the intended target of the call
        ///     
        /// The DOUBLE_PACKET_FROM (0x89) of the DOUBLE_PACKET_TYPE1 frame will contain the unit ID that transmitted
        /// the call alert. This opcode does not expect an ack, regardless of the bit 7 setting.
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Data
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Inbound
        /// </remarks>
        public const byte CALL_ALERT_NOACK_EXPECTED = 0x81;

        /// <summary>
        /// Call Alert/Page Acknowledge
        /// </summary>
        /// <remarks>
        /// Supports the following arguments:
        ///     NO_ARG (0x00) value indicates the unit ID which initiated the call
        ///     
        /// The DOUBLE_PACKET_FROM (0x89) of the DOUBLE_PACKET_TYPE1 frame will contain the unit ID that transmitted
        /// the acknowledge.
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Data
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Outbound
        /// </remarks>
        public const byte CALL_ALERT_ACK = 0xA0;

        /// <summary>
        /// Voice Selective Call
        /// </summary>
        /// <remarks>
        /// This opcode is doubled to operand 0x82 as well as 0x80?
        /// 
        /// Supports the following arguments:
        ///     SELECTIVE_CALL_TO (0x15) value indicates the intended target of the call
        ///
        /// The DOUBLE_PACKET_FROM (0x89) of the DOUBLE_PACKET_TYPE1 frame will contain the unit ID that transmitted
        /// the call alert.
        ///
        /// Opcode Bits:
        ///     8 = Command/Data Packet     = Data
        ///     7 = Ack/No Ack Required     = No Ack
        ///     6 = Inbound/Outbound Packet = Inbound
        /// </remarks>
        public const byte SELECTIVE_CALL_1 = 0x80;
        public const byte SELECTIVE_CALL_2 = 0x82;
        #endregion
        #region Args
        /// <summary>
        /// No Argument
        /// </summary>
        public const byte NO_ARG = 0x00;

        /**
         * Single Packets
         */
        /// <summary>
        /// Emergency Argument (unknown use)
        /// </summary>
        public const byte ARG_EMERG_UNKNW = 0x81;

        /// <summary>
        /// PTT ID Pre-
        /// </summary>
        public const byte ARG_PTT_PRE = 0x80;

        /// <summary>
        /// Radio Check
        /// </summary>
        public const byte ARG_RADIO_CHECK = 0x85;

        /// <summary>
        /// Status Request
        /// </summary>
        public const byte ARG_STATUS_REQ = 0x06;

        /// <summary>
        /// Remote Monitor
        /// </summary>
        public const byte ARG_REMOTE_MONITOR = 0x8A;

        /// <summary>
        /// Cancel Selective Radio Inhibit
        /// </summary>
        public const byte ARG_CANCEL_INHIBIT = 0x0C;

        /// <summary>
        /// Request to Talk
        /// </summary>
        public const byte ARG_RTT = 0x01;

        /**
         * Double Packets
         */
        /// <summary>
        /// Double To Argument
        /// </summary>
        /// <remarks>Unit ID represents what radio ID the call is targeting</remarks>
        public const byte ARG_DOUBLE_PACKET_TO = 0x89;

        /// <summary>
        /// Call Alert Argument
        /// </summary>
        /// <remarks>Unit ID represents what radio ID the call originated from</remarks>
        public const byte ARG_CALL_ALERT = 0x0D;
        #endregion

        public delegate void MDC1200ReceivedDelegate(MDC1200 decoder, RadioLog.Common.SignalCode sigCode, int frameCount, byte op, byte arg, ushort unitID, byte extra0, byte extra1, byte extra2, byte extra3, string opMsg);

#if ZEROCROSSING
        private double hyst;
        private int level;
        private double lastvalue;
#endif
        private double incr;
        private MDC1200SampleItem[] mdcSamples = new MDC1200SampleItem[MDC_ND];
        private int good;
        private int indouble;
        private byte op;
        private byte arg;
        private ushort unitID;
        private ushort crc;
        private byte extra0;
        private byte extra1;
        private byte extra2;
        private byte extra3;
        private MDC1200ReceivedDelegate _callback = null;

        private class MDC1200SampleItem
        {
            public double th;
            public int zc;
            public bool xorb;
            public bool invert;
            public int nlstep;
            public double[] nlevel = new double[10];
            public UInt32 synclow;
            public UInt32 synchigh;
            public int shstate;
            public int shcount;
            public bool[] bits = new bool[112];
        }

        public MDC1200(int samepleRate, MDC1200ReceivedDelegate callback, string sourceName)
        {
            _callback = callback;
            _sourceName = sourceName;
            incr = (1200.0 * DecoderHelpers.TWOPI) / samepleRate;
            good = 0;
            indouble = 0;
#if ZEROCROSSING
            hyst = 3.0 / 256.0;
            level = 0;
#endif
            for (int i = 0; i < MDC_ND; i++)
            {
                mdcSamples[i] = new MDC1200SampleItem();
                mdcSamples[i].th = 0.0 + (i * (DecoderHelpers.TWOPI / MDC_ND));
                mdcSamples[i].zc = 0;
                mdcSamples[i].xorb = false;
                mdcSamples[i].invert = false;
                mdcSamples[i].shstate = -1;
                mdcSamples[i].shcount = 0;
                mdcSamples[i].nlstep = i;
            }
        }

        private void ClearAllSamples()
        {
            if (_buffersAreClear)
                return;
            for (int i = 0; i < MDC_ND; i++)
            {
                ClearBits(i);
            }
            _buffersAreClear = true;
        }
        private void ClearBits(int x)
        {
            for (int i = 0; i < 112; i++)
            {
                mdcSamples[x].bits[i] = false;
            }
        }
        private void ProcBits(int x)
        {
            bool[] lbits = new bool[112];
            int lbc = 0;
            int i, j, k;
            byte[] data = new byte[14];
            ushort ccrc;
            ushort rcrc;

            for (i = 0; i < 16; i++)
            {
                for (j = 0; j < 7; j++)
                {
                    k = (j * 16) + i;
                    lbits[lbc] = mdcSamples[x].bits[k];
                    ++lbc;
                }
            }
            for (i = 0; i < 14; i++)
            {
                data[i] = 0;
                for (j = 0; j < 8; j++)
                {
                    k = (i * 8) + j;
                    if (lbits[k])
                    {
                        data[i] |= (byte)(1 << j);
                    }
                }
            }

            ccrc = DecoderHelpers.MDC1200_ComputeCRC(data, 4);
            rcrc = (ushort)(data[5] << 8 | data[4]);

            if (ccrc == rcrc)
            {
                if (mdcSamples[x].shstate == 2)
                {
                    extra0 = data[0];
                    extra1 = data[1];
                    extra2 = data[2];
                    extra3 = data[3];

                    for (k = 0; k < MDC_ND; k++)
                    {
                        mdcSamples[k].shstate = -1;
                    }
                    good = 2;
                    indouble = 0;
                }
                else
                {
                    if (indouble <= 0)
                    {
                        good = 1;
                        op = data[0];
                        arg = data[1];
                        unitID = (ushort)((data[2] << 8) | data[3]);
                        crc = (ushort)((data[4] << 8) | data[5]);
                        switch (data[0])
                        {
                            /* list of opcode that mean 'double packet' */
                            case 0x35:
                            case 0x55:
                                good = 0;
                                indouble = 1;
                                mdcSamples[x].shstate = 2;
                                mdcSamples[x].shcount = 0;
                                ClearBits(x);
                                break;
                            default:
                                // only in the single-packet case, double keeps rest going
                                for (k = 0; k < MDC_ND; k++)
                                {
                                    mdcSamples[k].shstate = -1;
                                }
                                break;
                        }
                    }
                    else
                    {
                        // any subsequent good decoder allowed to attempt second half
                        mdcSamples[x].shstate = 2;
                        mdcSamples[x].shcount = 0;
                        ClearBits(x);
                    }
                }
            }
            else
            {
                mdcSamples[x].shstate = -1;
            }

            if (good > 0)
            {
                ProcessGoodMDC(good, op, arg, unitID, extra0, extra1, extra2, extra3);
                good = 0;
            }
        }

        private void ProcessGoodMDC(int good, byte op, byte arg, ushort unitID, byte extra0, byte extra1, byte extra2, byte extra3)
        {
            string desc = string.Empty;
            RadioLog.Common.SignalCode sigCode = Common.SignalCode.Generic;
            switch (op)
            {
                case PTT_ID:
                    switch (arg)
                    {
                        case NO_ARG:
                            desc += string.Format("PTT ID: {0:X4} [Post-ID]", unitID);
                            break;

                        case ARG_PTT_PRE:
                            desc += string.Format("PTT ID: {0:X4} [Pre-ID]", unitID);
                            break;

                        default:
                            desc += string.Format("PTT ID: {0:X4} [Unknown-ID]", unitID);
                            break;
                    }
                    sigCode = Common.SignalCode.PTT;
                    break;
                case EMERGENCY:
                    desc += string.Format("EMERG: {0:X4}", unitID);
                    sigCode = Common.SignalCode.Emergency;
                    break;

                case EMERGENCY_ACK:
                    desc += string.Format("EMERG ACK: {0:X4}", unitID);
                    sigCode = Common.SignalCode.EmergencyAck;
                    break;

                case RADIO_CHECK:
                    desc += string.Format("RADIO CHECK: {0:X4}", unitID);
                    sigCode = Common.SignalCode.RadioCheck;
                    break;

                case RADIO_CHECK_ACK:
                    desc += string.Format("RADIO CHECK ACK: {0:X4}", unitID);
                    sigCode = Common.SignalCode.RadioCheckAck;
                    break;

                case REMOTE_MONITOR:
                    desc += string.Format("REMOTE MONITOR: {0:X4}", unitID);
                    //sigCode
                    break;

                case RTT_1:
                case RTT_2:
                    desc += string.Format("RTT: {0:X4}", unitID);
                    break;

                case RTT_ACK:
                    desc += string.Format("RTT ACK: {0:X4}", unitID);
                    break;

                case RADIO_INHIBIT:
                    {
                        // check argument
                        switch (arg)
                        {
                            case NO_ARG:
                                desc += string.Format("RADIO STUN/INHIBIT: {0:X4}", unitID);
                                sigCode = Common.SignalCode.RadioStun;
                                break;

                            case ARG_CANCEL_INHIBIT:
                                desc += string.Format("RADIO REVIVE/UNINHIBIT: {0:X4}", unitID);
                                sigCode = Common.SignalCode.RadioRevive;
                                break;

                            default:
                                desc += string.Format("UNK INHIBIT: {0:X4}", unitID);
                                break;
                        }
                    }
                    break;

                case RADIO_INHIBIT_ACK:
                    {
                        // check argument
                        switch (arg)
                        {
                            case NO_ARG:
                                desc += string.Format("RADIO STUN/INHIBIT ACK: {0:X4}", unitID);
                                sigCode = Common.SignalCode.RadioStunAck;
                                break;

                            case ARG_CANCEL_INHIBIT:
                                desc += string.Format("RADIO REVIVE/UNINHIBIT ACK: {0:X4}", unitID);
                                sigCode = Common.SignalCode.RadioReviveAck;
                                break;

                            default:
                                desc += string.Format("UNK INHIBIT ACK: {0:X4}", unitID);
                                break;
                        }
                    }
                    break;

                /**
                 * Double Packet Operations
                 */
                case DOUBLE_PACKET_TYPE1:
                case DOUBLE_PACKET_TYPE2:
                    desc += string.Format("PACKET TO: {0:X4}", unitID);
                    break;

                case CALL_ALERT_ACK_EXPECTED:
                case CALL_ALERT_NOACK_EXPECTED:
                    desc += string.Format("PAGE FROM: {0:X4}", unitID);
                    break;

                case CALL_ALERT_ACK:
                    desc += string.Format("CALL/PAGE ACK: {0:X4}", unitID);
                    break;

                case SELECTIVE_CALL_1:
                case SELECTIVE_CALL_2:
                    desc += string.Format("SEL-CALL FROM: {0:X4}", unitID);
                    break;

                default:
                    desc += string.Format("UNK Op {0:x2}", op);
                    break;
            }
            if (good >= 2)
            {
                Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Green, "Src:{0},MDC:{1},Good:{2},Op:{3:x2},Arg:{4:x2},UnitID:{5:X4},E0:{6:x2},E1:{7:x2},E2:{8:x2},E3:{9:x2}", _sourceName, desc, good, op, arg, unitID, extra0, extra1, extra2, extra3);
            }
            else
            {
                Common.ConsoleHelper.ColorWriteLine(ConsoleColor.Green, "Src:{0},MDC:{1},Good:{2},Op:{3:x2},Arg:{4:x2},UnitID:{5:X4}", _sourceName, desc, good, op, arg, unitID);
            }
            ClearAllSamples();
            if (_callback != null)
            {
                _callback(this, sigCode, good, op, arg, unitID, extra0, extra1, extra2, extra3, "MDC: " + desc);
            }
        }

        private int OneBits(UInt32 n)
        {
            int i = 0;
            while (n > 0)
            {
                ++i;
                n &= (n - 1);
            }
            return i;
        }
        private void ProcessShiftIn(int x, bool bit)
        {
            int gcount;
            mdcSamples[x].synchigh <<= 1;
            if ((mdcSamples[x].synclow & 0x80000000) > 0)
                mdcSamples[x].synchigh |= 1;
            mdcSamples[x].synclow <<= 1;
            if (bit)
                mdcSamples[x].synclow |= 1;
            gcount = OneBits(0x000000ff & (0x00000007 ^ mdcSamples[x].synchigh));
            gcount += OneBits(0x092a446f ^ mdcSamples[x].synclow);
            if (gcount <= MDC_GDTHRESH)
            {
                mdcSamples[x].shstate = 1;
                mdcSamples[x].shcount = 0;
                ClearBits(x);
            }
            else if (gcount >= (40 - MDC_GDTHRESH))
            {
                mdcSamples[x].shstate = 1;
                mdcSamples[x].shcount = 0;
                mdcSamples[x].xorb = !mdcSamples[x].xorb;
                mdcSamples[x].invert = !mdcSamples[x].invert;
                ClearBits(x);
            }
        }
        private void ShiftIn(int x)
        {
            bool bit = mdcSamples[x].xorb;
            switch (mdcSamples[x].shstate)
            {
                case -1:
                    mdcSamples[x].synchigh = 0;
                    mdcSamples[x].synclow = 0;
                    mdcSamples[x].shstate = 0;
                    ProcessShiftIn(x, bit);
                    break;
                case 0:
                    ProcessShiftIn(x, bit);
                    break;
                case 1:
                case 2:
                    mdcSamples[x].bits[mdcSamples[x].shcount] = bit;
                    mdcSamples[x].shcount++;
                    if (mdcSamples[x].shcount > 111)
                    {
                        ProcBits(x);
                    }
                    break;
                default:
                    break;
            }
        }
        private void ZCProc(int x)
        {
            switch (mdcSamples[x].zc)
            {
                case 2:
                case 4:
                    break;
                case 3:
                    mdcSamples[x].xorb = !mdcSamples[x].xorb;
                    break;
                default:
                    return;
            }
            ShiftIn(x);
        }
        private void NLProc(int x)
        {
            double vnow;
            double vpast;
            switch (mdcSamples[x].nlstep)
            {
                case 3:
                    vnow = ((-0.60 * mdcSamples[x].nlevel[3]) + (0.97 * mdcSamples[x].nlevel[1]));
                    vpast = ((-0.60 * mdcSamples[x].nlevel[7]) + (0.97 * mdcSamples[x].nlevel[9]));
                    break;
                case 8:
                    vnow = ((-0.60 * mdcSamples[x].nlevel[8]) + (0.97 * mdcSamples[x].nlevel[6]));
                    vpast = ((-0.60 * mdcSamples[x].nlevel[2]) + (0.97 * mdcSamples[x].nlevel[4]));
                    break;
                default:
                    return;
            }
            mdcSamples[x].xorb = (vnow > vpast);
            if (mdcSamples[x].invert)
                mdcSamples[x].xorb = !mdcSamples[x].xorb;
            ShiftIn(x);
        }

#if MDC_SAMPLE_FORMAT_U8
        public int ProcessSamples(byte[] samples, int numSamples, bool bHasSound)
#elif MDC_SAMPLE_FORMAT_U16
        public int ProcessSamples(UInt16[] samples, int numSamples, bool bHasSound)
#elif MDC_SAMPLE_FORMAT_S16
        public int ProcessSamples(Int16[] samples, int numSamples, bool bHasSound)
#elif MDC_SAMPLE_FORMAT_FLOAT
        public int ProcessSamples(float[] samples, int numSamples, bool bHasSound)
#endif
        {
            if(!bHasSound)
            {
                ClearAllSamples();
                return 0;
            }

            _buffersAreClear = false;

            int i, j;
            double value;
#if ZEROCROSSING
            int k;
            double delta;
#endif

            for (i = 0; i < numSamples; i++)
            {
#if MDC_SAMPLE_FORMAT_U8
                value = (samples[i] - 128.0)/256;
#elif MDC_SAMPLE_FORMAT_U16
                value = (samples[i] - 32768.0)/65536.0;
#elif MDC_SAMPLE_FORMAT_S16
                value = (samples[i] / 65536.0;
#elif MDC_SAMPLE_FORMAT_FLOAT
                value = samples[i];
#else
                value = samples[i];
#endif
#if ZEROCROSSING
#if DIFFERENTIATOR
                delta = value - lastvalue;
                lastvalue = value;
                if (level == 0)
                {
                    if (delta > hyst)
                    {
                        for (k = 0; k < MDC_ND; k++)
                        {
                            mdcSamples[k].zc++;
                        }
                        level = 1;
                    }
                }
                else
                {
                    if (delta < (-1 * hyst))
                    {
                for (k = 0; k < MDC_ND; k++){
                    mdcSamples[k].zc++;
                }
                    }
                    level = 0;
                }
#else
                if (level == 0)
                {
                    if (s > hyst)
                    {
                        for (k = 0; k < MDC_ND; k++)
                        {
                            zc[k]++;
                        }
                        level = 1;
                    }
                }
                else
                {
                    if (s < (-1.0 * hyst))
                    {
                        for (k = 0; k < MDC_ND; k++)
                        {
                            zc[k]++;
                        }
                        level = 0;
                    }
                }
#endif
                for (j = 0; j < MDC_ND; j++)
                {
                    mdcSamples[j].th += incr;
                    if (mdcSamples[j].th >= TWOPI)
                    {
                        ZCProc(j);
                        mdcSamples[j].th -= TWOPI;
                        mdcSamples[j].zc = 0;
                    }
                }
#else
                for (j = 0; j < MDC_ND; j++)
                {
                    mdcSamples[j].th += (5.0 * incr);
                    if (mdcSamples[j].th > DecoderHelpers.TWOPI)
                    {
                        mdcSamples[j].nlstep++;
                        if (mdcSamples[j].nlstep > 9)
                            mdcSamples[j].nlstep = 0;
                        mdcSamples[j].nlevel[mdcSamples[j].nlstep] = value;
                        NLProc(j);
                        mdcSamples[j].th -= DecoderHelpers.TWOPI;
                    }
                }
#endif
            }
            if (good > 0)
                return good;
            return 0;
        }
    }
}

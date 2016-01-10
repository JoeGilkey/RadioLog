using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders.P25.Message
{
    public class TSBKMessage : P25Message
    {
        public static readonly int LAST_BLOCK_FLAG = 0;
        public static readonly int ENCRYPTED_FLAG = 1;
        public static readonly int[] OPCODE = { 2, 3, 4, 5, 6, 7 };
        public static readonly int[] VENDOR_ID = { 8, 9, 10, 11, 12, 13, 14, 15 };

        public TSBKMessage(int system, RadioLog.Common.SafeBitArray message)
            : base(message, Reference.DataUnitID.TSBK1)
        {
            //
        }

        public bool isLastBlock()
        {
            return mMessage[LAST_BLOCK_FLAG];
        }
        public bool isEncrypted()
        {
            return mMessage[ENCRYPTED_FLAG];
        }
        public P25.Reference.Opcode getOpcode() { return (Reference.Opcode)mMessage.getInt(OPCODE); }
        public int getVendor() { return mMessage.getInt(VENDOR_ID); }

        public override string GetDescription()
        {
            string vend = TSBKMessageFactory.GetVendorNameFromId(getVendor());
            if (string.IsNullOrWhiteSpace(vend))
                return TSBKMessageFactory.GetDescFromOpcode(getOpcode());
            else
                return string.Format("{0} [{1}]", TSBKMessageFactory.GetDescFromOpcode(getOpcode()), vend);
        }
    }

    public static class TSBKMessageFactory
    {
        public static string GetVendorNameFromId(int vendor)
        {
            switch (vendor)
            {
                case 0: return "STANDARD";
                case 16: return "RELM/BK RADIO";
                case 32: return "CYCOMM";
                case 40: return "EFRATOM";
                case 48: return "ERICSSON";
                case 56: return "DATRON";
                case 64: return "ICOM";
                case 72: return "GARMIN";
                case 80: return "GTE";
                case 85: return "IFR SYSTEMS";
                case 96: return "GEC-MARCONI";
                case 104: return "KENWOOD";
                case 112: return "GLENAYRE ELECTRONICS";
                case 116: return "JAPAN RADIO CO";
                case 120: return "KOKUSAI";
                case 124: return "MAXON";
                case 128: return "MIDLAND";
                case 134: return "DANIELS ELECTRONICS";
                case 144: return "MOTOROLA";
                case 160: return "THALES";
                case 164: return "M/A-COM";
                case 176: return "RATHEON";
                case 192: return "SEA";
                case 200: return "SECURICOR";
                case 208: return "ADI";
                case 216: return "TAIT";
                case 224: return "TELETEC";
                case 240: return "TRANSCRYPT";
                default: return string.Empty;
            }
        }
        public static string GetDescFromOpcode(Reference.Opcode opCode)
        {
            switch (opCode)
            {
                case Reference.Opcode.GROUP_VOICE_CHANNEL_GRANT: return "Group Voice Channel Grant"; //0
                case Reference.Opcode.GROUP_VOICE_CHANNLE_GRANT_UPDATE: return "Group Voice Channel Grant Update"; //2
                case Reference.Opcode.GROUP_VOICE_CHANNEL_GRANT_UPDATE_EXPLICIT: return "Group Voice Channel Grant Update - Explicit"; //3
                case Reference.Opcode.UNIT_TO_UNIT_VOICE_CHANNEL_GRANT: return "Unit-to-Unit Voice Channel Grant"; //4
                case Reference.Opcode.UNIT_TO_UNIT_ANSWER_REQUEST: return "Unit-to-Unit Answer Request"; //5
                case Reference.Opcode.UNIT_TO_UNIT_VOICE_CHANNEL_GRANT_UPDATE: return "Unit-to-Unit Voice Channel Grant Update"; //6
                case Reference.Opcode.TELEPHONE_INTERCONNECT_VOICE_CHANNEL_GRANT: return "Telephone Interconnect Voice Channel Grant"; //8
                case Reference.Opcode.TELEPHONE_INTERCONNECT_ANSWER_REQUEST: return "Telephone Interconnect Answer Request"; //10
                case Reference.Opcode.INDIVIDUAL_DATA_CHANNEL_GRANT: return "Individual Data Channel Grant"; //16
                case Reference.Opcode.GROUP_DATA_CHANNEL_GRANT: return "Group Data Channel Grant"; //17
                case Reference.Opcode.GROUP_DATA_CHANNEL_ANNOUNCEMENT: return "Group Data Channel Announcement"; //18
                case Reference.Opcode.GROUP_DATA_CHANNEL_ANNOUNCEMENT_EXPLICIT: return "Group Data Channel Announcement-Explicit"; //19
                case Reference.Opcode.SNDCP_DATA_CHANNEL_GRANT: return "SNDCP Data Channel Grant"; //20
                case Reference.Opcode.SNDCP_DATA_PAGE_REQUEST: return "SNDCP Data Page Request"; //21
                case Reference.Opcode.SNDCP_DATA_CHANNEL_ANNOUNCEMENT_EXPLICIT: return "SNDCP Data Channel Announcement Explicit"; //22
                case Reference.Opcode.STATUS_UPDATE: return "Status Update"; //24
                case Reference.Opcode.STATUS_QUERY: return "Status Query"; //26
                case Reference.Opcode.MESSAGE_UPDATE: return "Message Update"; //28
                case Reference.Opcode.RADIO_UNIT_MONITOR_COMMAND: return "Radio Unit Monitor Command"; //29
                case Reference.Opcode.CALL_ALERT: return "Call Alert"; //31
                case Reference.Opcode.ACKNOWLEDGE_RESPONSE_FNE: return "Acknowledge Response - FNE"; //32
                case Reference.Opcode.QUEUED_RESPONSE: return "Queued Response"; //33
                case Reference.Opcode.EXTENDED_FUNCTION_COMMAND: return "Extended Function Command"; //36
                case Reference.Opcode.DENY_RESPONSE: return "Deny Response"; //39
                case Reference.Opcode.GROUP_AFFILIATION_RESPONSE: return "Group Affiliation Response"; //40
                case Reference.Opcode.SECONDARY_CONTROL_CHANNEL_BROADCAST_EXPLICIT: return "Secondary Control Channel Broadcast-Explicit"; //41
                case Reference.Opcode.GROUP_AFFILIATION_QUERY: return "Group Affiliation Query"; //42
                case Reference.Opcode.LOCATION_REGISTRATION_RESPONSE: return "Location Registration Response"; //43
                case Reference.Opcode.UNIT_REGISTRATION_RESPONSE: return "Unit Registration Response"; //44
                case Reference.Opcode.UNIT_REGISTRATION_COMMAND: return "Unit Registration Command"; //45
                case Reference.Opcode.AUTHENTICATION_COMMAND: return "Authentication Command"; //46
                case Reference.Opcode.UNIT_DEREGISTRATION_ACKNOWLEDGE: return "De-Registration Acknowledge"; //47
                case Reference.Opcode.IDENTIFIER_UPDATE_VHF_UHF_BANDS: return "Identifier Update for VHF/UHF Bands"; //52
                case Reference.Opcode.TIME_DATE_ANNOUNCEMENT: return "Time and Date Announcement"; //53
                case Reference.Opcode.ROAMING_ADDRESS_COMMAND: return "Roaming Address Command"; //54
                case Reference.Opcode.ROAMING_ADDRESS_UPDATE: return "Roaming Address Update"; //55
                case Reference.Opcode.SYSTEM_SERVICE_BROADCAST: return "System Service Broadcast"; //56
                case Reference.Opcode.SECONDARY_CONTROL_CHANNEL_BROADCAST: return "Secondary Control Channel Broadcast"; //57
                case Reference.Opcode.RFSS_STATUS_BROADCAST: return "RFSS Status Broadcast"; //58
                case Reference.Opcode.NETWORK_STATUS_BROADCAST: return "Network Status Broadcast"; //59
                case Reference.Opcode.ADJACENT_STATUS_BROADCAST: return "Adjacent Status Broadcast"; //60
                case Reference.Opcode.IDENTIFIER_UPDATE_NON_VHF_UHF: return "Identifier Update for non-VHF/UHF Bands"; //61
                case Reference.Opcode.PROTECTION_PARAMETER_BROADCAST: return "Protection Parameter Broadcast"; //62
                case Reference.Opcode.PROTECTION_PARAMETER_UPDATE: return "Protection Parameter Update"; //63
                case Reference.Opcode.UNKNOWN: return "Unknown"; //-1
                default:
                    {
                        return string.Format("RESERVED: {0}", opCode);
                    }
            }
            return string.Empty;
        }

        public static TSBKMessage getMessage(int system, RadioLog.Common.SafeBitArray buffer)
        {
            int opcode = buffer.getInt(TSBKMessage.OPCODE);
            int vendor = buffer.getInt(TSBKMessage.VENDOR_ID);
            switch (vendor)
            {
                case 0: //STANDARD
                    {
                        return new TSBKMessage(system, buffer);
                    }
                case 144: //MOTOROLA
                    {
                        return new TSBKMessage(system, buffer);
                    }
                default:
                    {
                        return new TSBKMessage(system, buffer);
                    }
            }
        }
    }
}

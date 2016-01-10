using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Message
{
    public enum MessageType
    {
        CA_STRT, //"Call Start" ;
        CA_ENDD, //"Call End" ;
        CA_TOUT, //"Call Timeout" ;
        CA_PAGE, //"Call Page" ;
        DA_STRT, //"Data Start" ;
        DA_ENDD, //"Data End" ;
        DA_TOUT, //"Data Timeout" ;
        FQ_CCHN, //"Control Channel Frequency" ;
        FQ_RXCV, //"Receive Frequency" ;
        FQ_RXLO, //"Receive Frequency Low" ;
        FQ_RXHI, //"Receive Frequency High" ;
        FQ_TXMT, //"Transmit Frequency" ;
        FQ_TXLO, //"Transmit Frequency Low" ;
        FQ_TXHI, //"Transmit Frequency High" ;
        ID_ANIX, //"ANI ID" ;
        ID_CHAN, //"Channel ID" ;
        ID_ESNX, //"ESN ID" ;
        ID_ESNH, //"ESN ID Low" ;
        ID_ESNL, //"ESN ID High" ;
        ID_FROM, //"From ID" ;
        ID_MIDN, //"Mobile ID" ;
        ID_NBOR, //"Neighbor ID" ;
        ID_RDIO, //"Radio ID" ;
        ID_SITE, //"Site ID" ;
        ID_SYST, //"System ID" ;
        ID_TGAS, //"Assign Talkgroup ID" ;
        ID_TOTO, //"To ID" ;
        ID_UNIQ, //"Unique ID" ;
        MA_CHAN, //"Channel Map" ;
        MA_CHNL, //"Channel Map Low" ;
        MA_CHNH, //"Channel Map High" ;
        MS_TEXT, //"Text Message" ;
        MS_MGPS, //"GPS Message";
        MS_STAT, //"Status Message" ;
        RA_KILL, //"Radio Kill" ;
        RA_REGI, //"Radio Register" ;
        RA_STUN, //"Radio Stun" ;
        RQ_ACCE, //"Request Access" ;
        SY_IDLE, //"System Idle" ;
        UN_KNWN, //"Unknown" );
    }

    public class MessageTypeToDesc
    {
        public static string Convert(MessageType msgType)
        {
            switch (msgType)
            {
                case MessageType.CA_STRT: return "Call Start";
                case MessageType.CA_ENDD: return "Call End";
                case MessageType.CA_TOUT: return "Call Timeout";
                case MessageType.CA_PAGE: return "Call Page";
                case MessageType.DA_STRT: return "Data Start";
                case MessageType.DA_ENDD: return "Data End";
                case MessageType.DA_TOUT: return "Data Timeout";
                case MessageType.FQ_CCHN: return "Control Channel Frequency";
                case MessageType.FQ_RXCV: return "Receive Frequency";
                case MessageType.FQ_RXLO: return "Receive Frequency Low";
                case MessageType.FQ_RXHI: return "Receive Frequency High";
                case MessageType.FQ_TXMT: return "Transmit Frequency";
                case MessageType.FQ_TXLO: return "Transmit Frequency Low";
                case MessageType.FQ_TXHI: return "Transmit Frequency High";
                case MessageType.ID_ANIX: return "ANI ID";
                case MessageType.ID_CHAN: return "Channel ID";
                case MessageType.ID_ESNX: return "ESN ID";
                case MessageType.ID_ESNH: return "ESN ID Low";
                case MessageType.ID_ESNL: return "ESN ID High";
                case MessageType.ID_FROM: return "From ID";
                case MessageType.ID_MIDN: return "Mobile ID";
                case MessageType.ID_NBOR: return "Neighbor ID";
                case MessageType.ID_RDIO: return "Radio ID";
                case MessageType.ID_SITE: return "Site ID";
                case MessageType.ID_SYST: return "System ID";
                case MessageType.ID_TGAS: return "Assign Talkgroup ID";
                case MessageType.ID_TOTO: return "To ID";
                case MessageType.ID_UNIQ: return "Unique ID";
                case MessageType.MA_CHAN: return "Channel Map";
                case MessageType.MA_CHNL: return "Channel Map Low";
                case MessageType.MA_CHNH: return "Channel Map High";
                case MessageType.MS_TEXT: return "Text Message";
                case MessageType.MS_MGPS: return "GPS Message";
                case MessageType.MS_STAT: return "Status Message";
                case MessageType.RA_KILL: return "Radio Kill";
                case MessageType.RA_REGI: return "Radio Register";
                case MessageType.RA_STUN: return "Radio Stun";
                case MessageType.RQ_ACCE: return "Request Access";
                case MessageType.SY_IDLE: return "System Idle";
                case MessageType.UN_KNWN: return "Unknown";
                default: return string.Empty;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing
{
    public enum Shift
    {
        LEFT,
        RIGHT,
        NONE
    }

    public enum SampleType
    {
        COMPLEX,
        REAL
    }

    public enum CRC
    {
        PASSED,
        PASSED_INV,
        FAILED_CRC,
        FAILED_PARITY,
        CORRECTED,
        UNKNOWN
    }

    public enum WindowType
    {
    	BLACKMAN,
        COSINE,
        HAMMING,
        HANNING,
        NONE
    }

    public enum Output
    {
        NORMAL,
        INVERTED
    }

    public enum TapType
    {
        EVENT_SYNC_DETECT,
        STREAM_BINARY,
        STREAM_COMPLEX,
        STREAM_FLOAT,
        STREAM_SYMBOL
    }

    public enum MessageType
    {
        CA_STRT,// ( "Call Start" ),
        CA_ENDD,// ( "Call End" ),
        CA_TOUT,// ( "Call Timeout" ),
        CA_PAGE,// ( "Call Page" ),
        DA_STRT,// ( "Data Start" ),
        DA_ENDD,// ( "Data End" ),
        DA_TOUT,// ( "Data Timeout" ),
        FQ_CCHN,// ( "Control Channel Frequency" ),
        FQ_RXCV,// ( "Receive Frequency" ),
        FQ_RXLO,// ( "Receive Frequency Low" ),
        FQ_RXHI,// ( "Receive Frequency High" ),
        FQ_TXMT,// ( "Transmit Frequency" ),
        FQ_TXLO,// ( "Transmit Frequency Low" ),
        FQ_TXHI,// ( "Transmit Frequency High" ),
        ID_ANIX,// ( "ANI ID" ),
        ID_CHAN,// ( "Channel ID" ),
        ID_ESNX,// ( "ESN ID" ),
        ID_ESNH,// ( "ESN ID Low" ),
        ID_ESNL,// ( "ESN ID High" ),
        ID_FROM,// ( "From ID" ),
        ID_MIDN,// ( "Mobile ID" ),
        ID_NBOR,// ( "Neighbor ID" ),
        ID_RDIO,// ( "Radio ID" ),
        ID_SITE,// ( "Site ID" ),
        ID_SYST,// ( "System ID" ),
        ID_TGAS,// ( "Assign Talkgroup ID" ),
        ID_TOTO,// ( "To ID" ),
        ID_UNIQ,// ( "Unique ID" ),
        MA_CHAN,// ( "Channel Map" ),
        MA_CHNL,// ( "Channel Map Low" ),
        MA_CHNH,// ( "Channel Map High" ),
        MS_TEXT,// ( "Text Message" ),
        MS_MGPS,// ( "GPS Message"),
        MS_STAT,// ( "Status Message" ),
        RA_KILL,// ( "Radio Kill" ),
        RA_REGI,// ( "Radio Register" ),
        RA_STUN,// ( "Radio Stun" ),
        RQ_ACCE,// ( "Request Access" ),
        SY_IDLE,// ( "System Idle" ),
        UN_KNWN// ( "Unknown" ),
    }

    public enum DecoderType
    {
        AM,
        FLEETSYNC2,
        LTR_STANDARD,
        LTR_NET,
        MDC1200,
        MPT1327,
        NBFM,
        PASSPORT,
        P25_PHASE1
    }
}

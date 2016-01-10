using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Controller.Activity
{
    public interface MessageDetailsProvider
    {
        /**
         * Indicates if the message is valid and has passed crc/integrity checks
         */
        bool isValid();

        /**
         * Status of the CRC check of the message
         */
        String getErrorStatus();

        /**
         * Parsed Message
         * @return
         */
        String getMessage();

        /**
         * Raw ( 0 & 1 ) message bits
         */
        String getBinaryMessage();

        /**
         * Decoded protocol
         */
        String getProtocol();

        /**
         * Event - call, data, idle, etc.
         */
        String getEventType();

        /**
         * Formatted from identifier
         */
        String getFromID();

        /**
         * Formatted to identifier
         */
        String getToID();
    }
}

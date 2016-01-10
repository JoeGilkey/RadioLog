using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Instrument
{
    /**
 * Interface for classes that want to expose internal data, streams or events 
 * to external listeners.
 */
    public interface Instrumentable<T>
    {
        /**
         * Returns the list of available tap points.  Use the addTap() and
         * removeTap() to register a tap on the instrumentable class and then add
         * your TapListener(s) to the registered tap.
         */
        List<Tap<T>> getTaps();

        /**
         * Registers a tap on the instrumentable object.
         * 
         * @param tap - one of the tap(s) obtained from getTaps() from the 
         * instrumentable source
         */
        void addTap(Tap<T> tap);

        /**
         * Unregisters a tap on the instrumentable object, if the tap is currently
         * registered.
         * 
         * @param tap - one of the tap(s) obtained from getTaps() from the 
         * instrumentable source
         */
        void removeTap(Tap<T> tap);
    }
}

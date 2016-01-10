using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cinch
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PopupNameToViewLookupKeyMetadataAttribute : Attribute
    {
        public string PopupName { get; private set; }
        public Type ViewLookupKey { get; private set; }


        public PopupNameToViewLookupKeyMetadataAttribute(string popupName, Type viewLookupKey)
        {
            PopupName = popupName;
            ViewLookupKey = viewLookupKey;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cinch
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewnameToViewLookupKeyMetadataAttribute : Attribute
    {
        public string ViewName { get; private set; }
        public Type ViewLookupKey { get; private set; }


        public ViewnameToViewLookupKeyMetadataAttribute(string viewName, Type viewLookupKey)
        {
            ViewName = viewName;
            ViewLookupKey = viewLookupKey;
        }

    }
}

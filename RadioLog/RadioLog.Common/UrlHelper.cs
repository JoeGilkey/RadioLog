using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Common
{
    public static class UrlHelper
    {
        public static string GetCorrectedStreamURL(string inUrl)
        {
            if (string.IsNullOrWhiteSpace(inUrl))
                return string.Empty;
            inUrl = inUrl.Trim();
            if (!inUrl.ToLower().StartsWith("http"))
                inUrl = Uri.UriSchemeHttp + Uri.SchemeDelimiter + inUrl;
            Uri test;
            if (Uri.TryCreate(inUrl, UriKind.Absolute, out test))
                return inUrl;
            string fixedStr = Uri.UriSchemeHttp + Uri.SchemeDelimiter + inUrl;
            if (Uri.TryCreate(fixedStr, UriKind.Absolute, out test))
                return fixedStr;
            else
                return inUrl;
        }

        public static bool GetCorrectedStreamURL(string inUrl, out string outUrl)
        {
            outUrl = GetCorrectedStreamURL(inUrl);
            return !string.IsNullOrWhiteSpace(outUrl);
        }
    }
}

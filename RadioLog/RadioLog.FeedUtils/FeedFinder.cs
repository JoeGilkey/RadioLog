using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Web;

namespace RadioLog.FeedUtils
{
    public class FeedFinder
    {
        public delegate void ProcessUrlCompleteDelegate(List<string> results);
        
        private ProcessUrlCompleteDelegate _callback = null;
        private string _sourceUrl = string.Empty;
        private string _sourcePrefix = string.Empty;
        private bool _isLiveATC = false;
        private bool _isBroadcastify = false;
        private string _broadcastifyFeedId = string.Empty;
        private string _broadcastifyPrefix = string.Empty;
        private string _broadcastifyListPrefix = string.Empty;
        private string _broadcastifyListSuffix = string.Empty;
        private List<string> _broadcastifyFeedIds = new List<string>();

        public FeedFinder(string sourceUrl, ProcessUrlCompleteDelegate callback)
        {
            this._sourceUrl = GetCorrectedStreamURL(sourceUrl);
            this._callback = callback;

            try
            {
                Uri tmpUri = new Uri(this._sourceUrl);
                _sourcePrefix = tmpUri.Scheme + Uri.SchemeDelimiter + tmpUri.Host;
            }
            catch
            {
                _sourcePrefix = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(sourceUrl))
            {
                if (sourceUrl.ToLower().Contains("liveatc.net"))
                {
                    _isLiveATC = true;
                }
                if (sourceUrl.ToLower().Contains("broadcastify.com/listen/feed"))
                {
                    _isBroadcastify = true;
                    try
                    {
                        Uri tmpUri = new Uri(sourceUrl);
                        string path = tmpUri.AbsolutePath;
                        if (path.EndsWith("/"))
                            path = path.Substring(0, path.Length - 1);
                        int indx = path.LastIndexOf('/');
                        if (indx >= 0)
                            path = path.Substring(indx + 1);
                        _broadcastifyFeedId = path;
                        _broadcastifyPrefix = tmpUri.Scheme + Uri.SchemeDelimiter + tmpUri.Host;
                    }
                    catch
                    {
                        _isBroadcastify = false;
                    }
                }
                else if (sourceUrl.ToLower().Contains("broadcastify.com/listen"))
                {
                    _isBroadcastify = true;
                    try
                    {
                        Uri tmpUri = new Uri(sourceUrl);
                        _broadcastifyPrefix = tmpUri.Scheme + Uri.SchemeDelimiter + tmpUri.Host;
                    }
                    catch
                    {
                        _isBroadcastify = false;
                    }
                }
            }
        }

        private string GetCorrectedStreamURL(string inUrl)
        {
            Uri test;
            if (Uri.TryCreate(inUrl, UriKind.Absolute, out test))
                return inUrl;
            string fixedStr = Uri.UriSchemeHttp + Uri.SchemeDelimiter + inUrl;
            if (Uri.TryCreate(fixedStr, UriKind.Absolute, out test))
                return fixedStr;
            else
                return inUrl;
        }

        public void ParsePage()
        {
            Task tParse = new Task(() =>
            {
                string htmlSource = RetrievePage(_sourceUrl);
                if (string.IsNullOrWhiteSpace(htmlSource))
                    return;
                HtmlAgilityPack.HtmlDocument htmlDoc = GetDocument(htmlSource);
                if (htmlDoc == null)
                    return;
                List<string> feeds = ProcessDocument(htmlDoc);
                if (_callback != null)
                {
                    _callback(feeds);
                }
            });
            tParse.Start();
        }

        private string GetFeedUrlFromValue(string val, bool bIsScriptPlaylist)
        {
            if (string.IsNullOrWhiteSpace(val))
                return string.Empty;
            if (bIsScriptPlaylist)
            {
                Uri test;
                if (Uri.TryCreate(val, UriKind.Absolute, out test))
                    return val;
                Uri rootUri = new Uri(_sourceUrl, UriKind.Absolute);
                if (!val.StartsWith("/"))
                    val = "/" + val;
                val = rootUri.Scheme + Uri.SchemeDelimiter + rootUri.Host + val;
                if (Uri.TryCreate(val, UriKind.Absolute, out test))
                    return val;
                return string.Empty;
            }
            if (val.StartsWith("/listen/feed/", StringComparison.InvariantCultureIgnoreCase))
                return val;
            int iLast = val.LastIndexOf('.');
            if (iLast < 0)
                return string.Empty;
            string ext = val.Substring(iLast + 1);
            if (string.IsNullOrWhiteSpace(ext))
                return string.Empty;
            if (ext.ToLower().Contains("m3u") || ext.ToLower().Contains("pls"))
            {
                Uri test;
                if (Uri.TryCreate(val, UriKind.Absolute, out test))
                    return val;
            }
            return string.Empty;
        }
        private void PostProcessBroadcastifyFeeds(List<string> subLinks)
        {
            if (subLinks == null || _broadcastifyFeedIds.Count <= 0 || string.IsNullOrWhiteSpace(_broadcastifyListPrefix) || string.IsNullOrWhiteSpace(_broadcastifyListSuffix))
                return;
            foreach (string feedId in _broadcastifyFeedIds)
            {
                string url = _broadcastifyListPrefix + feedId + _broadcastifyListSuffix;
                if (!url.StartsWith("/"))
                    url = _broadcastifyPrefix + "/" + url;
                else
                    url = _broadcastifyPrefix + url;
                if(!subLinks.Contains(url))
                {
                    subLinks.Add(url);
                }
            }
        }
        private string ProcessUrl(string val, bool bIsScriptPlaylist)
        {
            string url = GetFeedUrlFromValue(val, false);
            if (!string.IsNullOrWhiteSpace(url))
            {
                if (_isBroadcastify && url.ToLower().Contains("/listen/feed/"))
                {
                    if (url.EndsWith("/"))
                        url = url.Substring(0, url.Length - 1);
                    int indx = url.LastIndexOf('/');
                    if (indx >= 0)
                        url = url.Substring(indx + 1);
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        _broadcastifyFeedIds.Add(url);
                    }
                }
                else
                {
                    if (url.StartsWith("/"))
                    {
                        //fix it...
                        url = _sourcePrefix + url;
                    }
                    return url;
                }
            }
            return string.Empty;
        }
        private string[] ProcessNode(HtmlAgilityPack.HtmlNode htmlNode)
        {
            if (htmlNode == null || string.IsNullOrWhiteSpace(htmlNode.Name))
                return null;
            string nodeName = htmlNode.Name.ToLower().Trim();
            switch (nodeName)
            {
                case "a":
                    {
                        string href = htmlNode.GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrWhiteSpace(href))
                        {
                            string url = ProcessUrl(href, false);
                            if (!string.IsNullOrWhiteSpace(url))
                                return new string[] { url };
                        }
                        else if(_isLiveATC)
                        {
                            string onClick = htmlNode.GetAttributeValue("onClick", string.Empty);
                            if (!string.IsNullOrWhiteSpace(onClick) && onClick.ToLower().Contains("mydirectstream") && onClick.IndexOf('\'') > 0)
                            {
                                onClick = onClick.Substring(onClick.IndexOf('\'') + 1);
                                if (!string.IsNullOrWhiteSpace(onClick) && onClick.IndexOf('\'') > 0)
                                {
                                    onClick = onClick.Substring(0, onClick.IndexOf('\''));
                                    string url = ProcessUrl(string.Format("http://d.liveatc.net/{0}.m3u", onClick), false);
                                    if (!string.IsNullOrWhiteSpace(url))
                                        return new string[] { url };
                                }
                            }
                        }
                        break;
                    }
                case "meta":
                    {
                        string metaType = htmlNode.GetAttributeValue("http-equiv", string.Empty);
                        if (string.Equals(metaType, "refresh", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string metaContent = htmlNode.GetAttributeValue("content", string.Empty);
                            if (!string.IsNullOrWhiteSpace(metaContent))
                            {
                                int indUrl = metaContent.ToLower().IndexOf("url=");
                                if (indUrl >= 0)
                                {
                                    metaContent = metaContent.Substring(indUrl + 4);
                                }
                                string url = ProcessUrl(metaContent, false);
                                if (!string.IsNullOrWhiteSpace(url))
                                    return new string[] { url };
                            }
                        }
                        break;
                    }
                case "object":
                    {
                        string classid = htmlNode.GetAttributeValue("classid", string.Empty);
                        if (string.Equals(classid, "CLSID:22d6f312-b0f6-11d0-94ab-0080c74c7e95", StringComparison.InvariantCultureIgnoreCase))
                        {
                            //this is a media player object...
                            string mediaURL = string.Empty;
                            foreach (HtmlAgilityPack.HtmlNode subNode in htmlNode.ChildNodes)
                            {
                                string subNodeName = subNode.Name.ToLower().Trim();
                                if(string.Equals(subNodeName,"param", StringComparison.InvariantCultureIgnoreCase) && string.Equals(subNode.GetAttributeValue("name",string.Empty), "filename", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    string strFileName = subNode.GetAttributeValue("value", string.Empty);
                                    if(!string.IsNullOrWhiteSpace(strFileName))
                                    {
                                        return new string[] { strFileName };
                                    }
                                }
                            }
                        }
                        break;
                    }
                case "script":
                    {
                        if (string.IsNullOrWhiteSpace(htmlNode.GetAttributeValue("src", string.Empty)) && !string.IsNullOrWhiteSpace(htmlNode.InnerText))
                        {
                            //process script...
                            System.IO.StringReader rdr = new System.IO.StringReader(htmlNode.InnerText);
                            string inLine=string.Empty;
                            do
                            {
                                inLine = rdr.ReadLine();
                                if (!string.IsNullOrWhiteSpace(inLine))
                                {
                                    inLine = inLine.ToLower();
                                    int indx = inLine.IndexOf(".playlist");
                                    if (indx >= 0)
                                    {
                                        indx += 9;
                                        inLine = inLine.Substring(indx);
                                        indx = inLine.IndexOf('\'');
                                        if (indx >= 0)
                                        {
                                            inLine = inLine.Substring(indx + 1);
                                            indx = inLine.IndexOf('\'');
                                            if (indx >= 0)
                                            {
                                                inLine = GetFeedUrlFromValue(inLine.Substring(0, indx), true);
                                                if (!string.IsNullOrWhiteSpace(inLine))
                                                {
                                                    return new string[] { inLine };
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        indx = inLine.IndexOf("ipadurl");
                                        if (indx >= 0)
                                        {
                                            indx += 7;
                                            inLine = inLine.Substring(indx);
                                            indx = inLine.IndexOf("\"");
                                            if (indx < 0)
                                                indx = inLine.IndexOf("'");
                                            if (indx >= 0)
                                            {
                                                inLine = inLine.Substring(indx + 1);
                                                indx = inLine.LastIndexOf("\"");
                                                if (indx < 0)
                                                    indx = inLine.LastIndexOf("'");
                                                if (indx >= 0)
                                                {
                                                    inLine = GetFeedUrlFromValue(inLine.Substring(0, indx), true);
                                                    if (!string.IsNullOrWhiteSpace(inLine))
                                                    {
                                                        return new string[] { inLine };
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            indx = inLine.IndexOf(".m3u");
                                            if (indx >= 0)
                                            {
                                                int indxStart = inLine.IndexOf('"');
                                                int indxEnd = inLine.LastIndexOf('"');
                                                if (indxEnd > 0)
                                                    inLine = inLine.Substring(0, indxEnd);
                                                if (indxStart >= 0)
                                                    inLine = inLine.Substring(indxStart + 1);
                                                if (_isBroadcastify && inLine.IndexOf('"') >= 0)
                                                {
                                                    indxStart = inLine.IndexOf('"');
                                                    indxEnd = inLine.LastIndexOf('"');
                                                    string tmpStart = inLine.Substring(0, indxStart);
                                                    string tmpEnd = inLine.Substring(indxEnd + 1);
                                                    if (string.IsNullOrWhiteSpace(_broadcastifyFeedId))
                                                    {
                                                        _broadcastifyListPrefix = tmpStart;
                                                        _broadcastifyListSuffix = tmpEnd;
                                                    }
                                                    else
                                                    {
                                                        inLine = tmpStart + _broadcastifyFeedId + tmpEnd;
                                                        if (!inLine.StartsWith("/"))
                                                            inLine = _broadcastifyPrefix + "/" + inLine;
                                                        else
                                                            inLine = _broadcastifyPrefix + inLine;
                                                        if (!string.IsNullOrWhiteSpace(inLine))
                                                        {
                                                            return new string[] { inLine };
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            while (inLine != null);
                        }
                        break;
                    }
                default:
                    {
                        if (htmlNode.HasChildNodes)
                        {
                            List<string> subLinks = new List<string>();
                            foreach (HtmlAgilityPack.HtmlNode subNode in htmlNode.ChildNodes)
                            {
                                string[] links = ProcessNode(subNode);
                                if (links != null)
                                {
                                    foreach (string strLink in links)
                                    {
                                        if (!subLinks.Contains(strLink))
                                            subLinks.Add(strLink);
                                    }
                                }
                            }
                            PostProcessBroadcastifyFeeds(subLinks);
                            return subLinks.ToArray();
                        }
                        break;
                    }
            }
            return null;
        }
        private List<string> ProcessDocument(HtmlAgilityPack.HtmlDocument htmlDoc)
        {
            if (htmlDoc == null||htmlDoc.DocumentNode==null||htmlDoc.DocumentNode.ChildNodes==null)
                return null;
            List<string> streamLinks = new List<string>();

            foreach (HtmlAgilityPack.HtmlNode node in htmlDoc.DocumentNode.ChildNodes)
            {
                string[] nodeLinks = ProcessNode(node);
                if (nodeLinks != null && nodeLinks.Length > 0)
                {
                    foreach (string lnk in nodeLinks)
                    {
                        if (!streamLinks.Contains(lnk))
                            streamLinks.Add(lnk);
                    }
                }
            }

            return streamLinks;
        }

        private string RetrievePage(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;
            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            var webResponse=(HttpWebResponse)webRequest.GetResponse();
            if (webResponse.StatusCode != HttpStatusCode.OK)
                return string.Empty;
            StringBuilder sb = new StringBuilder();
            using (System.IO.StreamReader sr = new System.IO.StreamReader(webResponse.GetResponseStream()))
            {
                while (!sr.EndOfStream)
                {
                    string str = sr.ReadLine();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        sb.AppendLine(str);
                    }
                }
            }
            return sb.ToString();
        }
        private HtmlAgilityPack.HtmlDocument GetDocument(string htmlSource)
        {
            var document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(htmlSource);
            return document;
        }
    }
}

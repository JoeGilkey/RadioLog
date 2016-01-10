using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RadioLog.Broadcastify
{
    public class FeedAPI
    {
        private bool _userValid = false;
        private bool _userVerificationAttempted = false;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _lastUserValidationError = string.Empty;

        private const string BASE_URI = @"http://api.radioreference.com/audio/";
        private const string API_KEY = "78028295";
        private const string RESPONSE_TYPE = "json";
        private const string ACTION_FEEDS = "feeds";
        private const string ACTION_COUNTRIES = "countries";
        private const string ACTION_STATES = "states";
        private const string ACTION_COUNTIES = "counties";
        private const string ACTION_COUNTY = "county";
        private const string ACTION_FEED = "feed";
        private const string ACTION_ARCHIVES = "archives";
        private const string PARAM_KEY = "key";
        private const string PARAM_ACTION = "a";
        private const string PARAM_RESPONSE_TYPE = "type";
        private const string PARAM_COUNTY = "ctid";
        private const string PARAM_STATE = "stid";
        private const string PARAM_COUNTRY = "coid";
        private const string PARAM_TOP = "top";
        private const string PARAM_NEW = "new";
        private const string PARAM_SEARCH = "s";
        private const string PARAM_GENRE = "genre";
        private const string PARAM_FEED = "feedId";
        private const string GENRE_PUBLIC_SAFETY = "1";
        private const string GENRE_AIRCRAFT = "2";
        private const string GENRE_RAIL = "3";
        private const string GENRE_AMATEUR_RADIO = "4";
        private const string GENRE_MARINE = "5";
        private const string GENRE_OTHER = "6";
        private const string GENRE_SPECIAL_EVENT = "7";
        private const string GENRE_DISASTER_EVENT = "8";

        private string GetCacheFileName(string cacheType, string subCode = null)
        {
            if (string.IsNullOrWhiteSpace(cacheType))
            {
                return string.Empty;
            }
            string dirPath = Common.AppSettings.Instance.AppDataDir;
            string fileName;
            if (string.IsNullOrWhiteSpace(subCode))
                fileName = string.Format("RRCache_{0}.json", cacheType);
            else
                fileName = string.Format("RRCache_{0}_{1}.json", cacheType, subCode);
            return System.IO.Path.Combine(dirPath, fileName);
        }
        private JObject GetCachedData(string cacheType, string subCode = null)
        {
            string fileName = GetCacheFileName(cacheType, subCode);
            if (string.IsNullOrWhiteSpace(fileName) || !System.IO.File.Exists(fileName))
                return null;

            try
            {
                return Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(fileName));
            }
            catch
            {
                return null;
            }
        }
        private void SetCachedData(JObject jObj, string cacheType, string subCode = null)
        {
            if (jObj == null)
                return;
            string fileName = GetCacheFileName(cacheType, subCode);
            if (string.IsNullOrWhiteSpace(fileName))
                return;
            try
            {
                if (System.IO.File.Exists(fileName))
                    System.IO.File.Delete(fileName);
            }
            finally
            {
                try
                {
                    System.IO.File.WriteAllText(fileName, jObj.ToString());
                }
                catch (Exception ex)
                {
                    Common.DebugHelper.WriteExceptionToLog("FeedAPI:SetCachedData", ex, false, fileName);
                }
            }
        }

        public FeedAPI() { }

        public bool UserValid { get { return _userValid; } }
        public bool UserVerificationAttempted { get { return _userVerificationAttempted; } }

        public void UpdateUserInfo(string userName, string password)
        {
            if (!string.Equals(_username, userName, StringComparison.InvariantCultureIgnoreCase) || !string.Equals(_password, password, StringComparison.InvariantCultureIgnoreCase))
            {
                _username = userName;
                _password = password;
                _userValid = false;
                _userVerificationAttempted = false;
                _lastUserValidationError = string.Empty;
            }
        }

        private bool ValidateUser(out string userValidationError)
        {
            userValidationError = string.Empty;
            if (_userVerificationAttempted)
            {
                userValidationError = _lastUserValidationError;
                return _userValid;
            }
            _userValid = RRUserVerification.VerifyUser(_username, _password, out userValidationError);
            _lastUserValidationError = userValidationError;
            _userVerificationAttempted = true;
            return _userValid;
        }

        private Dictionary<string, string> GetBaseRequest()
        {
            Dictionary<string, string> reqParams = new Dictionary<string, string>();
            AddMandatoryValsToParams(reqParams);
            return reqParams;
        }
        private void AddMandatoryValsToParams(Dictionary<string, string> requestParms)
        {
            if (requestParms == null)
                return;
            requestParms[PARAM_KEY] = API_KEY;
            requestParms[PARAM_RESPONSE_TYPE] = RESPONSE_TYPE;
        }
        private string MakeRequestUrl(Dictionary<string, string> requestParams)
        {
            if (requestParams == null || requestParams.Count <= 0)
                return string.Empty;
            int iCnt = 0;
            string resp = BASE_URI + "?";
            foreach (KeyValuePair<string, string> reqParam in requestParams)
            {
                if (iCnt > 0)
                    resp += "&";
                resp += string.Format("{0}={1}", reqParam.Key, Uri.EscapeDataString(reqParam.Value));
                iCnt++;
            }
            return resp;
        }
        private string GetGenreValue(Genre genre)
        {
            switch (genre)
            {
                case Genre.PublicSafety: return GENRE_PUBLIC_SAFETY;
                case Genre.Aircraft: return GENRE_AIRCRAFT;
                case Genre.Rail: return GENRE_RAIL;
                case Genre.AmateurRadio: return GENRE_AMATEUR_RADIO;
                case Genre.Marine: return GENRE_MARINE;
                case Genre.Other: return GENRE_OTHER;
                case Genre.SpecialEvent: return GENRE_SPECIAL_EVENT;
                case Genre.DisasterEvent: return GENRE_DISASTER_EVENT;
                default: return string.Empty;
            }
        }
        private void AddGenreToRequest(Genre genre, Dictionary<string, string> requestParams)
        {
            string sGenre = GetGenreValue(genre);
            if (!string.IsNullOrWhiteSpace(sGenre))
                requestParams[PARAM_GENRE] = sGenre;
        }

        private JObject SendRequest(bool bNeedsUserValidation, Dictionary<string, string> requestParams, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                if (bNeedsUserValidation)
                {
                    if (!ValidateUser(out errorMessage))
                    {
                        return null;
                    }
                }

                string requestUrl = MakeRequestUrl(requestParams);
                if (string.IsNullOrWhiteSpace(requestUrl))
                {
                    errorMessage = "Unable to generate request URL.";
                    return null;
                }

                HttpWebRequest httpWebRequest = WebRequest.Create(requestUrl) as HttpWebRequest;
                if (httpWebRequest == null)
                {
                    errorMessage = "Unable to create web request.";
                    return null;
                }
                httpWebRequest.Accept = "application/json";
                httpWebRequest.Method = "GET";

                string output = "";

                HttpWebResponse httpResponse = httpWebRequest.GetResponse() as HttpWebResponse;
                if (httpResponse == null)
                {
                    errorMessage = "No response received from the server.";
                    return null;
                }

                using (StreamReader streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    output = streamReader.ReadToEnd();
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    errorMessage = "Response was empty.";
                    return null;
                }
                return JObject.Parse(output);
            }
            catch (Exception ex)
            {
                Common.DebugHelper.WriteExceptionToLog("Broadcastify.FeedAPI.SendRequest", ex, false);
                errorMessage = string.Format("Error sending request: {0}", ex.Message);
                return null;
            }
        }

        public List<ItemIdHolder> GetCachedCountries()
        {
            return ParseCountryList(GetCachedData("Countries"));
        }
        public List<ItemIdHolder> GetAllCountries(out string errorMessage)
        {
            errorMessage = string.Empty;
            JObject jObj = GetCachedData("Countries");
            if (jObj == null)
            {
                Dictionary<string, string> reqParams = GetBaseRequest();
                reqParams[PARAM_ACTION] = ACTION_COUNTRIES;
                jObj = SendRequest(false, reqParams, out errorMessage);
                if (jObj != null)
                {
                    SetCachedData(jObj, "Countries");
                }
            }
            return ParseCountryList(jObj);
        }
        public List<ItemIdHolder> GetAllStatesForCountry(string countryCode, out string errorMessage)
        {
            errorMessage = string.Empty;
            JObject jObj = GetCachedData("States", countryCode);
            if (jObj == null)
            {
                Dictionary<string, string> reqParqams = GetBaseRequest();
                reqParqams[PARAM_ACTION] = ACTION_STATES;
                reqParqams[PARAM_COUNTRY] = countryCode;
                jObj = SendRequest(false, reqParqams, out errorMessage);
                if (jObj != null)
                {
                    SetCachedData(jObj, "States", countryCode);
                }
            }
            return ParseStateList(jObj);
        }
        public List<ItemIdHolder> GetAllCountiesForState(string stateCode, out string errorMessage)
        {
            errorMessage = string.Empty;
            JObject jObj = GetCachedData("Counties", stateCode);
            if(jObj==null)
            {
                Dictionary<string, string> reqParqams = GetBaseRequest();
                reqParqams[PARAM_ACTION] = ACTION_COUNTIES;
                reqParqams[PARAM_STATE] = stateCode;
                jObj = SendRequest(false, reqParqams, out errorMessage);
                if (jObj != null)
                {
                    SetCachedData(jObj, "Counties", stateCode);
                }
            }
            return ParseCountyList(jObj);
        }
        public List<FeedItemHolder> GetAllFeedsForState(string stateCode, Genre genre, out string errorMessage)
        {
            errorMessage = string.Empty;
            Dictionary<string, string> reqParqams = GetBaseRequest();
            reqParqams[PARAM_ACTION] = ACTION_FEEDS;
            reqParqams[PARAM_STATE] = stateCode;
            AddGenreToRequest(genre, reqParqams);
            JObject jObj = SendRequest(true, reqParqams, out errorMessage);
            if (jObj == null)
                return null;
            else
                return ParseFeedList(jObj);
        }
        public List<FeedItemHolder> SearchForFeedsInCounty(string countyCode, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(countyCode))
            {
                errorMessage = "County ID Not Provided!";
                return null;
            }

            errorMessage = string.Empty;
            Dictionary<string, string> reqParams = GetBaseRequest();
            reqParams[PARAM_ACTION] = ACTION_COUNTY;
            reqParams[PARAM_COUNTY] = countyCode;
            JObject jObj = SendRequest(true, reqParams, out errorMessage);
            if (jObj == null)
                return null;
            else
                return ParseCountyFeedList(jObj);
        }
        public List<FeedItemHolder> SearchForFeeds(string query, string countryCode, string stateCode, string topCount, string newCount, Genre genre, out string errorMessage)
        {
            errorMessage = string.Empty;
            Dictionary<string, string> reqParams = GetBaseRequest();
            reqParams[PARAM_ACTION] = ACTION_FEEDS;
            if (!string.IsNullOrWhiteSpace(stateCode))
                reqParams[PARAM_STATE] = stateCode;
            else if (!string.IsNullOrWhiteSpace(countryCode))
                reqParams[PARAM_COUNTRY] = countryCode;      
            if (!string.IsNullOrWhiteSpace(topCount))
                reqParams[PARAM_TOP] = topCount;
            if (!string.IsNullOrWhiteSpace(newCount))
                reqParams[PARAM_NEW] = newCount;
            if (string.IsNullOrWhiteSpace(newCount) && string.IsNullOrWhiteSpace(topCount))
                reqParams[PARAM_TOP] = "50";
            if (!string.IsNullOrWhiteSpace(query))
                reqParams[PARAM_SEARCH] = query;
            AddGenreToRequest(genre, reqParams);
            JObject jObj = SendRequest(true, reqParams, out errorMessage);
            if (jObj == null)
                return null;
            else
                return ParseFeedList(jObj);
        }

        private string GetStringValueFromProperty(JObject rootObj, string propName)
        {
            if (rootObj == null || string.IsNullOrWhiteSpace(propName))
                return string.Empty;
            JProperty prop = rootObj.Property(propName);
            if (prop == null || prop.Value == null)
                return string.Empty;
            return prop.Value.ToString();
        }
        private string GetCorrectedFeedURL(string host, string port, string mount)
        {
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(mount))
                return string.Empty;
            string rslt = Uri.UriSchemeHttp + Uri.SchemeDelimiter + host;
            if (!string.IsNullOrWhiteSpace(port) && port.Trim() != "80")
            {
                rslt += ":" + port;
            }
            if (mount.StartsWith("/"))
                rslt += mount;
            else
                rslt += "/" + mount;
            Uri u;
            if (Uri.TryCreate(rslt, UriKind.Absolute, out u))
                return rslt;
            else
                return string.Empty;
        }
        
        private List<ItemIdHolder> ParseCountryList(JObject jObj)
        {
            if (jObj == null)
                return null;

            JProperty propContries = jObj.Property("Countries");
            if (propContries == null)
                return null;

            JArray arrayCountries = propContries.Value as JArray;
            if (arrayCountries == null)
                return null;

            List<ItemIdHolder> rslt = new List<ItemIdHolder>();

            foreach (JToken countryToken in arrayCountries)
            {
                JObject countryObj = countryToken as JObject;
                if (countryObj != null)
                {
                    string strId = GetStringValueFromProperty(countryObj, "id");
                    string strName = GetStringValueFromProperty(countryObj, "name");
                    string strCode = GetStringValueFromProperty(countryObj, "code");
                    if (!string.IsNullOrWhiteSpace(strId) && !string.IsNullOrWhiteSpace(strName))
                    {
                        rslt.Add(new ItemIdHolder(strId, strName, strCode));
                    }
                }
            }
            return rslt;
        }
        private List<ItemIdHolder> ParseStateList(JObject jObj)
        {
            if (jObj == null)
                return null;

            JProperty propStates = jObj.Property("States");
            if (propStates == null)
                return null;

            JArray arrayStates = propStates.Value as JArray;
            if (arrayStates == null)
                return null;

            List<ItemIdHolder> rslt = new List<ItemIdHolder>();

            foreach (JToken stateToken in arrayStates)
            {
                JObject stateObj = stateToken as JObject;
                if (stateObj != null)
                {
                    string strId = GetStringValueFromProperty(stateObj, "id");
                    string strName = GetStringValueFromProperty(stateObj, "name");
                    string strCode = GetStringValueFromProperty(stateObj, "code");
                    if (!string.IsNullOrWhiteSpace(strId) && !string.IsNullOrWhiteSpace(strName))
                    {
                        rslt.Add(new ItemIdHolder(strId, strName, strCode));
                    }
                }
            }
            return rslt;
        }
        private List<ItemIdHolder> ParseCountyList(JObject jObj)
        {
            if (jObj == null)
                return null;
            JProperty propCounties = jObj.Property("Counties");
            if (propCounties == null)
                return null;

            JArray arrayCounties = propCounties.Value as JArray;
            if (arrayCounties == null)
                return null;

            List<ItemIdHolder> rslt = new List<ItemIdHolder>();
            foreach (JToken countyToken in arrayCounties)
            {
                JObject countyObj = countyToken as JObject;
                if (countyObj != null)
                {
                    string strId = GetStringValueFromProperty(countyObj, "id");
                    string strName = GetStringValueFromProperty(countyObj, "name");
                    string strCode = GetStringValueFromProperty(countyObj, "code");
                    if (!string.IsNullOrWhiteSpace(strId) && !string.IsNullOrWhiteSpace(strName))
                    {
                        rslt.Add(new ItemIdHolder(strId, strName, strCode));
                    }
                }
            }
            return rslt;
        }
        private List<FeedItemHolder> ParseCountyFeedList(JObject jObj)
        {
            if (jObj == null)
                return null;
            JProperty propCounty = jObj.Property("County");
            if (propCounty == null)
                return null;
            JArray arrayCounty = propCounty.Value as JArray;
            if (arrayCounty == null)
                return null;

            List<FeedItemHolder> rslt = new List<FeedItemHolder>();
            foreach (JToken countyToken in arrayCounty)
            {
                JObject countyObj = countyToken as JObject;
                if (countyObj != null)
                {
                    List<FeedItemHolder> tmp = ParseFeedList(countyObj);
                    if (tmp != null && tmp.Count > 0)
                    {
                        foreach (FeedItemHolder tmpItem in tmp)
                        {
                            rslt.Add(tmpItem);
                        }
                    }
                }
            }
            return rslt;
        }
        private List<FeedItemHolder> ParseFeedList(JObject jObj)
        {
            if (jObj == null)
                return null;

            JProperty propFeeds = jObj.Property("Feeds");
            if (propFeeds == null)
                return null;

            JArray arrayFeeds = propFeeds.Value as JArray;
            if (arrayFeeds == null)
                return null;

            List<FeedItemHolder> rslt = new List<FeedItemHolder>();

            foreach (JToken feedToken in arrayFeeds)
            {
                JObject feedObj = feedToken as JObject;
                if (feedObj != null)
                {
                    string strId = GetStringValueFromProperty(feedObj, "id");
                    string strName = GetStringValueFromProperty(feedObj, "descr");
                    string strMount = GetStringValueFromProperty(feedObj, "mount");
                    string strGenre = GetStringValueFromProperty(feedObj, "genre");
                    if (!(string.IsNullOrWhiteSpace(strId) || string.IsNullOrWhiteSpace(strName) || string.IsNullOrWhiteSpace(strMount) || string.IsNullOrWhiteSpace(strGenre)))
                    {
                        JProperty propRelays = feedObj.Property("Relays");
                        if (propRelays != null)
                        {
                            JArray arrayRelays = propRelays.Value as JArray;
                            if (arrayRelays != null)
                            {
                                foreach (JToken relayToken in arrayRelays)
                                {
                                    JObject relayObj = relayToken as JObject;
                                    if (relayObj != null)
                                    {
                                        string strHost = GetStringValueFromProperty(relayObj, "host");
                                        string strPort = GetStringValueFromProperty(relayObj, "port");
                                        if (!string.IsNullOrWhiteSpace(strHost))
                                        {
                                            string url = GetCorrectedFeedURL(strHost, strPort, strMount);
                                            if (!string.IsNullOrWhiteSpace(url))
                                            {
                                                rslt.Add(new FeedItemHolder(strId, strName, strGenre, url));
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            List<FeedItemHolder> sortedRslt = new List<FeedItemHolder>(rslt.OrderBy(r => r.FeedName));
            return sortedRslt;
        }

        public List<GenreListHolder> GetGenreList()
        {
            List<GenreListHolder> rslt = new List<GenreListHolder>();
            rslt.Add(new GenreListHolder(Genre.All, "All"));
            rslt.Add(new GenreListHolder(Genre.PublicSafety, "Public Safety"));
            rslt.Add(new GenreListHolder(Genre.Aircraft, "Aircraft"));
            rslt.Add(new GenreListHolder(Genre.Rail, "Rail"));
            rslt.Add(new GenreListHolder(Genre.AmateurRadio, "Amateur Radio"));
            rslt.Add(new GenreListHolder(Genre.Marine, "Marine"));
            rslt.Add(new GenreListHolder(Genre.Other, "Other"));
            rslt.Add(new GenreListHolder(Genre.SpecialEvent, "Special Event"));
            rslt.Add(new GenreListHolder(Genre.DisasterEvent, "Disaster Event"));
            return rslt;
        }

        public Genre LastGenre
        {
            get { return Common.AppSettings.Instance.ReadEnum<Genre>("RadioReference", "LastGenre", Genre.All); }
            set { Common.AppSettings.Instance.WriteEnum<Genre>("RadioReference", "LastGenre", value); }
        }
        public string LastCountry
        {
            get { return Common.AppSettings.Instance.ReadString("RadioReference", "LastCountry", string.Empty); }
            set { Common.AppSettings.Instance.WriteString("RadioReference", "LastCountry", value); }
        }
        public string LastState
        {
            get { return Common.AppSettings.Instance.ReadString("RadioReference", "LastState", string.Empty); }
            set { Common.AppSettings.Instance.WriteString("RadioReference", "LastState", value); }
        }
    }

    public enum Genre
    {
        All,
        PublicSafety,
        Aircraft,
        Rail,
        AmateurRadio,
        Marine,
        Other,
        SpecialEvent,
        DisasterEvent
    };

    public class ItemIdHolder
    {
        public string ItemId { get; private set; }
        public string ItemName { get; private set; }
        public string ItemCode { get; private set; }

        public ItemIdHolder(string id, string name, string code = null)
        {
            this.ItemId = id;
            this.ItemName = name;
            this.ItemCode = code;
        }
    }
    public class GenreListHolder
    {
        public Genre Value { get; private set; }
        public string Name { get; private set; }
        public GenreListHolder(Genre value, string name)
        {
            this.Value = value;
            this.Name = name;
        }
    }
    public class FeedItemHolder
    {
        public string FeedId { get; private set; }
        public string FeedName { get; private set; }
        public string Genre { get; private set; }
        public string FeedURL { get; private set; }

        public FeedItemHolder(string id, string name, string genre, string url)
        {
            this.FeedId = id;
            this.FeedName = name;
            this.Genre = genre;
            this.FeedURL = url;
        }
    }
}

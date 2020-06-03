using System;
using System.Diagnostics;
using System.Globalization;

namespace RadioLog.Broadcastify
{
    public class RRUserVerification
    {
        private const string API_KEY = "78028295";
        private static DateTime LastGoodExpDate = new DateTime(1990, 1, 1);

        public static bool VerifyUser(string userName, string password, out string errorMsg)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                {
                    errorMsg = "User Name or Password is blank.";
                    return false;
                }
                RadioLog.Broadcastify.RRService.authInfo auth = new RRService.authInfo();
                auth.appKey = API_KEY;
                auth.username = userName;
                auth.password = password;

                RRService.RRWsdl client = new RRService.RRWsdl();
                RRService.UserInfo userInfo = client.getUserData(auth);

                if (userInfo == null)
                {
                    errorMsg = "No response received from user validation!";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(userInfo.subExpireDate))
                {
                    errorMsg = "No account expiration date!";
                    return true;
                }

                CultureInfo enUS = new CultureInfo("en-US");
                DateTime dateVal = DateTime.MinValue;

                if (DateTime.TryParseExact(userInfo.subExpireDate, "MM-dd-yyyy", enUS, DateTimeStyles.AssumeLocal, out dateVal))
                {
                    bool bGood = (dateVal.Date >= DateTime.Now.Date || dateVal.Date < LastGoodExpDate);
                    if (bGood)
                        errorMsg = string.Empty;
                    else
                        errorMsg = string.Format("Account Expired: {0}", dateVal.Date.ToShortDateString());
                    return bGood;
                }

                errorMsg = string.Format("Unable to parse expiration date: {0}", userInfo.subExpireDate);
                return false;
            }
            catch (Exception ex)
            {
                Debug.Fail($"RRUserVerification.VerifyUser: {ex}");
                errorMsg = string.Format("Unable to verify user: {0}", ex.Message);
                return false;
            }
        }
    }
}

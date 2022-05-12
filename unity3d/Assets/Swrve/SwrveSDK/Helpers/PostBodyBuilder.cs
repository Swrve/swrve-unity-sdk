using System;
using System.Text;
using System.Text.RegularExpressions;

namespace SwrveUnity.Helpers
{
    /// <summary>
    /// Used internally to build POST requests for submission to %Swrve.
    /// </summary>
    public class PostBodyBuilder
    {
        private const int ApiVersion = 3;
        private static readonly string FormatEventBody = Regex.Replace(@"
    {{
    ""user"":""{0}"",
    ""version"":{1},
    ""app_version"":""{2}"",
    ""session_token"":""{3}"",
    ""unique_device_id"":""{4}"",
    ""data"":[{5}]
    }}", @"\s", "");

        private static readonly string FormatUserIdentifyBody = Regex.Replace(@"
    {{
    ""api_key"":""{0}"",
    ""swrve_id"":""{1}"",
    ""external_user_id"":""{2}"",
    ""unique_device_id"":""{3}""
    }}", @"\s", "");

        private static readonly string FormatQaEventsBody = Regex.Replace(@"
    {{
    ""user"":""{0}"",
    ""version"":{1},
    ""app_version"":""{2}"",
    ""session_token"":""{3}"",
    ""unique_device_id"":""{4}"",
    ""data"":{5}
    }}", @"\s", "");

        protected static string CreateSessionToken(string apiKey, int appId, string userId, long time)
        {
            var md5Hash = SwrveHelper.ApplyMD5(String.Format("{0}{1}{2}", userId, time, apiKey));
            return String.Format("{0}={1}={2}={3}", appId, userId, time, md5Hash);
        }

        public static byte[] BuildEvent(string apiKey, int appId, string userId, string deviceId, string appVersion, long time, string events)
        {
            var encodedData = Encoding.UTF8.GetBytes(PostBodyBuilder.EventPostString(apiKey, appId, userId, deviceId, appVersion, time, events));
            return encodedData;
        }

        protected static string EventPostString(string apiKey, int appId, string userId, string deviceId, string appVersion, long time, string events)
        {
            var sessionToken = CreateSessionToken(apiKey, appId, userId, time);
            return String.Format(FormatEventBody, userId, ApiVersion, appVersion, sessionToken, deviceId, events);
        }

        public static byte[] BuildIdentify(string apiKey, string userId, string externalUserId, string deviceId)
        {
            var encodedData = Encoding.UTF8.GetBytes(PostBodyBuilder.IdentifyPostString(apiKey, userId, externalUserId, deviceId));
            return encodedData;
        }

        protected static string IdentifyPostString(string apiKey, string userId, string externalUserId, string deviceId)
        {
            return String.Format(FormatUserIdentifyBody, apiKey, userId, externalUserId, deviceId);
        }

        public static byte[] BuildQaEvent(string apiKey, int appId, string userId, string deviceId, string appVersion, long time, string events)
        {
            var encodedData = Encoding.UTF8.GetBytes(PostBodyBuilder.QaEventPostString(apiKey, appId, userId, deviceId, appVersion, time, events));
            return encodedData;
        }

        protected static string QaEventPostString(string apiKey, int appId, string userId, string deviceId, string appVersion, long time, string events)
        {
            var sessionToken = CreateSessionToken(apiKey, appId, userId, time);
            return String.Format(FormatQaEventsBody, userId, ApiVersion, appVersion, sessionToken, deviceId, events);
        }

    }
}

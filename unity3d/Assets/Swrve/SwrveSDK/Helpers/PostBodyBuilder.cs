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
    private const int ApiVersion = 2;
    private static readonly string Format = Regex.Replace (@"
{{
 ""user"":""{0}"",
 ""version"":{1},
 ""app_version"":""{2}"",
 ""session_token"":""{3}"",
 ""device_id"":""{4}"",
 ""data"":[{5}]
}}", @"\s", "");

    public static byte[] Build (string apiKey, int appId, string userId, string deviceId, string appVersion, long time, string events)
    {
        var sessionToken = CreateSessionToken (apiKey, appId, userId, time);
        var postString = String.Format (Format, userId, ApiVersion, appVersion, sessionToken, deviceId, events);
        var encodedData = Encoding.UTF8.GetBytes (postString);
        return encodedData;
    }

    private static string CreateSessionToken (string apiKey, int appId, string userId, long time)
    {
        var md5Hash = SwrveHelper.ApplyMD5 (String.Format ("{0}{1}{2}", userId, time, apiKey));
        return String.Format ("{0}={1}={2}={3}", appId, userId, time, md5Hash);
    }
}
}

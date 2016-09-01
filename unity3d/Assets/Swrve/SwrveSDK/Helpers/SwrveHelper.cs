using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace SwrveUnity.Helpers
{
/// <summary
/// Used internally as a helper for the Swrve SDK.
/// </summary>
public static class SwrveHelper
{
    // Testing
    public static DateTime? Now = null;
    public static DateTime? UtcNow = null;

    // Reference to avoid this class from getting stripped
    private static System.Security.Cryptography.MD5CryptoServiceProvider fakeReference = new System.Security.Cryptography.MD5CryptoServiceProvider ();

    private static Regex rgxNonAlphanumeric = new Regex("[^a-zA-Z0-9]");

    public static DateTime GetNow ()
    {
        if (Now != null && Now.HasValue) {
            return Now.Value;
        }

        return DateTime.Now;
    }

    public static DateTime GetUtcNow ()
    {
        if (UtcNow != null && UtcNow.HasValue) {
            return UtcNow.Value;
        }

        return DateTime.UtcNow;
    }

    public static void Shuffle<T> (this IList<T> list)
    {
        int n = list.Count;
        Random rnd = new Random ();
        while (n > 1) {
            int k = (rnd.Next (0, n) % n);
            n--;
            T value = list [k];
            list [k] = list [n];
            list [n] = value;
        }
    }

    public static byte[] MD5 (String str)
    {
        byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes (str);
        return SwrveMD5Core.GetHash (inputBytes);
    }

    public static string ApplyMD5 (String str)
    {
        byte[] hash = MD5 (str);
        StringBuilder sBuilder = new StringBuilder ();
        for (int i = 0; i < hash.Length; i++) {
            sBuilder.Append (hash [i].ToString ("x2"));
        }

        return sBuilder.ToString ();
    }

    public static bool CheckBase64 (string str)
    {
        string s = str.Trim ();
        return (s.Length % 4 == 0) && Regex.IsMatch (s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
    }

    public static string CreateHMACMD5 (string data, string key)
    {
        if (fakeReference != null) {
            byte[] bData = System.Text.Encoding.UTF8.GetBytes (data);
            byte[] bKey = System.Text.Encoding.UTF8.GetBytes (key);
            using (HMACMD5 hmac = new HMACMD5(bKey)) {
                byte[] signature = hmac.ComputeHash (bData);
                return System.Convert.ToBase64String (signature);
            }
        }
        return null;
    }

    public static readonly DateTime UnixEpoch = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static long GetSeconds ()
    {
        return (long)(((TimeSpan)(DateTime.UtcNow - UnixEpoch)).TotalSeconds);
    }

    public static long GetMilliseconds ()
    {
        return (long)(((TimeSpan)(DateTime.UtcNow - UnixEpoch)).TotalMilliseconds);
    }

    public static string GetEventName (Dictionary<string, object> eventParameters)
    {
        string eventName = string.Empty;
        string eventType = (string)eventParameters ["type"];

        switch (eventType) {
        case "session_start":
            eventName = "Swrve.session.start";
            break;
        case "session_end":
            eventName = "Swrve.session.end";
            break;
        case "buy_in":
            eventName = "Swrve.buy_in";
            break;
        case "iap":
            eventName = "Swrve.iap";
            break;
        case "event":
            eventName = (string)eventParameters ["name"];
            break;
        case "purchase":
            eventName = "Swrve.user_purchase";
            break;
        case "currency_given":
            eventName = "Swrve.currency_given";
            break;
        case "user":
            eventName = "Swrve.user_properties_changed";
            break;
        }

        return eventName;
    }

    public static string EpochToFormat (long epochTime, string format)
    {
        DateTime epoch = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return epoch.AddMilliseconds (epochTime).ToString (format);
    }

    public static string FilterNonAlphanumeric(string str)
    {
        return rgxNonAlphanumeric.Replace(str, string.Empty);
    }

    public static bool IsNotOnDevice()
    {
        return !IsOnDevice();
    }

    public static bool IsOnDevice()
    {
    #if UNITY_IOS
        return IsAvailableOn(UnityEngine.RuntimePlatform.IPhonePlayer);

    #elif UNITY_ANDROID
        return IsAvailableOn(UnityEngine.RuntimePlatform.Android);

    #else
        return false;

    #endif
    }

    public static bool IsAvailableOn(UnityEngine.RuntimePlatform platform)
    {
        bool available = false;

        available = UnityEngine.Application.platform == platform;
        
        return available;
    }
}
}

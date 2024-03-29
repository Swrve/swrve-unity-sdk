using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace SwrveUnity.Helpers
{
    /// <summary
    /// Used internally as a helper for the Swrve SDK.
    /// </summary>
    public static partial class SwrveHelper
    {
        // Testing
        public static DateTime? Now = null;
        public static DateTime? UtcNow = null;

#if !UNITY_WSA_10_0 || UNITY_EDITOR
        // Reference to avoid this class from getting stripped
        private static System.Security.Cryptography.MD5CryptoServiceProvider fakeReference = new System.Security.Cryptography.MD5CryptoServiceProvider();
#endif

        private static Regex rgxNonAlphanumeric = new Regex("[^a-zA-Z0-9]");

        public static DateTime GetNow()
        {
            if (Now != null && Now.HasValue)
            {
                return Now.Value;
            }

            return DateTime.Now;
        }

        public static DateTime GetUtcNow()
        {
            if (UtcNow != null && UtcNow.HasValue)
            {
                return UtcNow.Value;
            }

            return DateTime.UtcNow;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            System.Random rnd = new System.Random();
            while (n > 1)
            {
                int k = (rnd.Next(0, n) % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static byte[] MD5(String str)
        {
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(str);
            return SwrveMD5Core.GetHash(inputBytes);
        }

        public static string ApplyMD5(String str)
        {
            byte[] hash = MD5(str);
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sBuilder.Append(hash[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        public static bool CheckBase64(string str)
        {
            string s = str.Trim();
            return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
        }

        public static string CreateHMACMD5(string data, string key)
        {
            string hmacmd5 = null;
#if !UNITY_WSA_10_0 || UNITY_EDITOR
            if (fakeReference != null)
            {
                byte[] bData = System.Text.Encoding.UTF8.GetBytes(data);
                byte[] bKey = System.Text.Encoding.UTF8.GetBytes(key);
                using (HMACMD5 hmac = new HMACMD5(bKey))
                {
                    byte[] signature = hmac.ComputeHash(bData);
                    hmacmd5 = System.Convert.ToBase64String(signature);
                }
            }
#elif NETFX_CORE
        var alg = Windows.Security.Cryptography.Core.MacAlgorithmProvider.OpenAlgorithm(Windows.Security.Cryptography.Core.MacAlgorithmNames.HmacMd5);
        var buffMsg = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(data, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
        var keyBuff = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(key, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
        var hmacKey = alg.CreateKey(keyBuff);
        var buffHMAC = Windows.Security.Cryptography.Core.CryptographicEngine.Sign(hmacKey, buffMsg);
        hmacmd5 = Windows.Security.Cryptography.CryptographicBuffer.EncodeToHexString(buffHMAC);
#endif
            return hmacmd5;
        }

        private static SHA1Managed sha1Managed = new SHA1Managed();

        public static string sha1(byte[] bytes)
        {
            byte[] hash = sha1Managed.ComputeHash(bytes);
            string hashString = "";
            for (int i = 0; i < hash.Length; i++)
            {
                byte b = hash[i];
                hashString += Convert.ToInt32(b).ToString("x2");
            }
            return hashString;
        }

        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetSeconds()
        {
            return (long)(((TimeSpan)(DateTime.UtcNow - UnixEpoch)).TotalSeconds);
        }

        public static long GetMilliseconds()
        {
            return (long)(((TimeSpan)(DateTime.UtcNow - UnixEpoch)).TotalMilliseconds);
        }

        public static string GetRandomUUID()
        {
#if UNITY_IOS
            string randomUUID = getNativeRandomUUID();
            if (!string.IsNullOrEmpty(randomUUID))
            {
                return randomUUID;
            }
#endif
            try
            {
                Type type = System.Type.GetType("System.Guid");
                if (type != null)
                {
                    MethodInfo methodInfo = type.GetMethod("NewGuid");
                    if (methodInfo != null)
                    {
                        object result = methodInfo.Invoke(null, null);
                        if (result != null)
                        {
                            string stringResult = result.ToString();
                            if (!string.IsNullOrEmpty(stringResult))
                            {
                                return stringResult;
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                SwrveLog.LogWarning("Couldn't get random UUID: " + exp.ToString());
            }

            // Generate random string if all fails
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string randomString = string.Empty;
            for (int i = 0; i < 128; i++)
            {
                System.Random rnd = new System.Random();
                int rndInt = rnd.Next(chars.Length);
                randomString += chars[rndInt];
            }
            return randomString;
        }

        public static string GetEventName(Dictionary<string, object> eventParameters)
        {
            string eventName = string.Empty;
            string eventType = (string)eventParameters["type"];

            switch (eventType)
            {
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
                    eventName = (string)eventParameters["name"];
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
                case "device_update":
                    eventName = "Swrve.device_properties_changed";
                    break;
            }

            return eventName;
        }

        public static string EpochToFormat(long epochTime, string format)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(epochTime).ToString(format);
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

        public static Dictionary<string, string> GetUriQueryParameters(string query)
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            string formattedQuery = query.Replace("?", "");
            string[] listOfQueries = formattedQuery.Split('&');
            for (int queryIndex = 0; queryIndex < listOfQueries.Length; queryIndex++)
            {
                string[] valueKeyCombo = listOfQueries[queryIndex].Split('=');
                string paramKey = valueKeyCombo[0];
                string paramValue = (valueKeyCombo.Length == 2) ? valueKeyCombo[1] : null;

                if (!String.IsNullOrEmpty(paramValue))
                {
                    queryParams.Add(paramKey, paramValue);
                }
            }

            return queryParams;
        }

        public static string EscapeURL(string url)
        {
#if UNITY_2017_3_OR_NEWER
            return UnityWebRequest.EscapeURL(url);
#else
        return WWW.EscapeURL (url);
#endif
        }

        public static Dictionary<string, string> CombineTwoStringDictionaries(Dictionary<string, string> baseDictionary, Dictionary<string, string> overridingDictionary)
        {
            Dictionary<string, string> combinedDictionary = null;
            if (baseDictionary != null)
            {
                combinedDictionary = baseDictionary;
                if (overridingDictionary != null)
                {
                    foreach (string key in overridingDictionary.Keys)
                    {
                        string value = overridingDictionary[key];
                        if (combinedDictionary.ContainsKey(key))
                        {
                            combinedDictionary[key] = value;
                        }
                        else
                        {
                            combinedDictionary.Add(key, value);
                        }
                    }
                }
            }
            else
            {
                combinedDictionary = overridingDictionary;
            }

            return combinedDictionary;
        }

        public static int CalibrateRelativeFontSizeToPlatform(Messaging.SwrveMessageFormatCalibration calibration, float fontSize)
        {
            GUIStyle sampleStyle = new GUIStyle();
            sampleStyle.alignment = TextAnchor.MiddleCenter;
            int relativeFontSize = GetTextSizeToFitImage(sampleStyle, calibration.Text, calibration.Width, calibration.Height);
            return (int)((fontSize / calibration.BaseFontSize) * relativeFontSize);
        }

        public static int GetTextSizeToFitImage(GUIStyle style, string text, int width, int height)
        {
            int testTextSize = 200;
            style.fontSize = testTextSize;
            GUIContent testContent = new GUIContent(text);
            Vector2 size = style.CalcSize(testContent);
            float scalex = testTextSize / size.x;
            float scaley = testTextSize / size.y;
            return (int)Math.Min(width * scalex, height * scaley);
        }
    }
}
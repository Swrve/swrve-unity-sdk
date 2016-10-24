using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SwrveUnity
{
/// <summary>
/// Used internally to support all platform.
/// </summary>
public static class CrossPlatformUtils
{
    public static WWW MakeWWW (string url, byte[] encodedData, Dictionary<string, string> headers)
    {
#if (UNITY_METRO || UNITY_WP8) || (UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5)
        return new WWW(url, encodedData, headers);
#else
        return new WWW (url, encodedData, new Hashtable (headers));
#endif
    }
}
}

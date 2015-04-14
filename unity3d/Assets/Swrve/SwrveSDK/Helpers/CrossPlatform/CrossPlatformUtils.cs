using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Swrve
{
/// <summary>
/// Used internally to support all platform.
/// </summary>
public static class CrossPlatformUtils
{
    public static WWW MakeWWW (string url, byte[] encodedData, Dictionary<string, string> headers)
    {
#if (UNITY_METRO || UNITY_WP8) || !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6)
        return new WWW(url, encodedData, headers);
#else
        return new WWW (url, encodedData, new Hashtable (headers));
#endif
    }
}
}

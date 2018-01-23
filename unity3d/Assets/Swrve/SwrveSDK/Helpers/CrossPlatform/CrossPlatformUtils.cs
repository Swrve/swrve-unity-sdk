using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace SwrveUnity
{
/// <summary>
/// Used internally to support all platform.
/// </summary>
public static class CrossPlatformUtils
{
#if UNITY_2017_1_OR_NEWER
    public static UnityWebRequest MakeRequest(string url, string requestMethod, byte[] encodedData, Dictionary<string, string> headers)
    {
            UnityWebRequest request = new UnityWebRequest (url);
            UploadHandlerRaw uH = new UploadHandlerRaw (encodedData);
            DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
            request.uploadHandler = uH;
            request.downloadHandler = dH;
            request.method = requestMethod;

            // Set Headers
            List<string> headerKeyList = new List<string>(headers.Keys);

            for (int i = 0; i < headerKeyList.Count; i++) {
                var currentKey = headerKeyList[i];
                var currentValue = "";

                if (headers.TryGetValue (currentKey, out currentValue)) {
                    request.SetRequestHeader(currentKey, currentValue);   
                }           
            }
              
            return request;
    }
#else
    public static WWW MakeWWW (string url, byte[] encodedData, Dictionary<string, string> headers)
    {
#if (UNITY_METRO || UNITY_WP8) || (UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5 || UNITY_2017_1_OR_NEWER)
        return new WWW(url, encodedData, headers);
#else
        return new WWW(url, encodedData, new Hashtable (headers));
#endif
    }
#endif
}
        
}

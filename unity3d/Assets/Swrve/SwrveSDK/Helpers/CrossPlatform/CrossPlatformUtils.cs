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
    public static UnityWebRequest MakeRequest(string url, string requestMethod, byte[] encodedData, Dictionary<string, string> headers)
    {
        UnityWebRequest request = new UnityWebRequest (url);
        UploadHandlerRaw uH = (encodedData == null)? null : new UploadHandlerRaw (encodedData);
        DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
        request.uploadHandler = uH;
        request.downloadHandler = dH;
        request.method = requestMethod;

        // Set headers
        if (headers != null) {
            var itHeaders = headers.GetEnumerator();
            while (itHeaders.MoveNext()) {
                request.SetRequestHeader(itHeaders.Current.Key, itHeaders.Current.Value);
            }
        }

        return request;
    }
}
}

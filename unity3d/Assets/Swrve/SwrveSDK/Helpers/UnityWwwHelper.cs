using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using SwrveUnityMiniJSON;

namespace SwrveUnity.Helpers
{
public enum WwwDeducedError {
    NoError,
    NetworkError,
    ApplicationErrorHeader,
    ApplicationErrorBody
}

/// <summary>
/// Used internally as a helper for the WWW class.
/// </summary>
public class UnityWwwHelper
{
    /// <summary>
    /// Attempts to deduce the WWW error
    /// </summary>
    /// <returns>
    /// The deduced error.
    /// </returns>
    /// <param name='request'>
    /// The request to check.
    /// </param>
    /// <param name='expectedResponse'>
    /// The expected response.
    /// </param>
    public static WwwDeducedError DeduceWwwError (UnityWebRequest request)
    {

        // Use UnityWebRequests error detection first
#if UNITY_2020_1_OR_NEWER
        if (request.result == UnityWebRequest.Result.ConnectionError) {
#else
        if (request.isNetworkError) {
#endif
            SwrveLog.LogError ("Request network error: " + request.error + " in " + request.url);
            return WwwDeducedError.NetworkError;
        }

        // Check response headers for X-Swrve-Error
        if (request.GetResponseHeaders() != null) {
            string errorKey = null;

            Dictionary<string, string>.Enumerator enumerator = request.GetResponseHeaders().GetEnumerator();
            while(enumerator.MoveNext()) {
                string headerKey = enumerator.Current.Key;
                if (string.Equals (headerKey, "X-Swrve-Error", StringComparison.OrdinalIgnoreCase)) {
                    request.GetResponseHeaders().TryGetValue (headerKey, out errorKey);
                    break;
                }
            }

            if (errorKey != null) {
                SwrveLog.LogError (@"Request response headers [""X-Swrve-Error""]: " + errorKey + " at " + request.url);
                try {
                    if (!string.IsNullOrEmpty (request.downloadHandler.text)) {
                        SwrveLog.LogError (@"Request response headers [""X-Swrve-Error""]: " +
                                           ((IDictionary<string, object>)Json.Deserialize(request.downloadHandler.text))["message"]);
                    }
                } catch(Exception e) {
                    SwrveLog.LogError(e.Message);
                }
                return WwwDeducedError.ApplicationErrorHeader;
            }
        }

        if (!string.IsNullOrEmpty (request.error)) {
            SwrveLog.LogError ("Request network error: " + request.error + " in " + request.url);
            return WwwDeducedError.NetworkError;
        }

        return WwwDeducedError.NoError;
    }

    public static int parseResponseCode(string statusLine)
    {
        int ret = 0;

        string[] components = statusLine.Split(' ');
        if (components.Length < 3) {
            Debug.LogError("invalid response status: " + statusLine);
        } else {
            if (!int.TryParse(components[1], out ret)) {
                Debug.LogError("invalid response code: " + components[1]);
            }
        }

        return ret;
    }


}
}

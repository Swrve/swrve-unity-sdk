using UnityEngine;
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
    public static WwwDeducedError DeduceWwwError (WWW request)
    {
        // Check response headers for X-Swrve-Error
        if (request.responseHeaders.Count > 0) {
            string errorKey = null;

            Dictionary<string, string>.Enumerator enumerator = request.responseHeaders.GetEnumerator();
            while(enumerator.MoveNext()) {
                string headerKey = enumerator.Current.Key;
                if (string.Equals (headerKey, "X-Swrve-Error", StringComparison.OrdinalIgnoreCase)) {
                    request.responseHeaders.TryGetValue (headerKey, out errorKey);
                    break;
                }
            }

            if (errorKey != null) {
                SwrveLog.LogError (@"Request response headers [""X-Swrve-Error""]: " + errorKey + " at " + request.url);
                try {
                    if (!string.IsNullOrEmpty (request.text)) {
                        SwrveLog.LogError (@"Request response headers [""X-Swrve-Error""]: " +
                            ((IDictionary<string, object>)Json.Deserialize(request.text))["message"]);
                    }
                }
                catch(Exception e) {
                    
                }
                return WwwDeducedError.ApplicationErrorHeader;
            }
        }

        // Check WWW error- accessing www.bytes can barf if this is set.
        // Unity 3.4 webplayer has error/nohdr/nobody for non-200 http response
        // - actually an application error, but treated here as a network error
        //   hence 'NetworkOrApplicationError'
        if (!string.IsNullOrEmpty (request.error)) {
            SwrveLog.LogError ("Request error: " + request.error + " in " + request.url);
            return WwwDeducedError.NetworkError;
        }

        return WwwDeducedError.NoError;
    }
}
}

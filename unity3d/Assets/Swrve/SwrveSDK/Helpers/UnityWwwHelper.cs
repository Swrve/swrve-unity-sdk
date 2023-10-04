using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using SwrveUnityMiniJSON;

namespace SwrveUnity.Helpers
{

    public enum WwwDeducedError
    {
        NoError,
        NetworkError,
        ApplicationErrorHeader,
        ApplicationErrorBody,
        UserError,
        ServerError,
        ConnectionError
    }

    /// <summary>
    /// Used internally as a helper for the WWW class.
    /// </summary>
    public class UnityWwwHelper
    {
        public static int HTTP_TOO_MANY_REQUESTS = 429;

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
        public static WwwDeducedError DeduceWwwError(UnityWebRequest request)
        {

            // Use UnityWebRequests error detection first
#if UNITY_2020_1_OR_NEWER
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
#else
            if (request.isNetworkError)
            {
#endif
                SwrveLog.LogError("Request connection error: " + request.error + " in " + request.url);
                return WwwDeducedError.ConnectionError;
            }

            // Check response headers for X-Swrve-Error
            if (request.GetResponseHeaders() != null)
            {
                string errorKey = null;

                Dictionary<string, string>.Enumerator enumerator = request.GetResponseHeaders().GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string headerKey = enumerator.Current.Key;
                    if (string.Equals(headerKey, "X-Swrve-Error", StringComparison.OrdinalIgnoreCase))
                    {
                        request.GetResponseHeaders().TryGetValue(headerKey, out errorKey);
                        break;
                    }
                }

                if (errorKey != null)
                {
                    SwrveLog.LogError(@"Request response headers [""X-Swrve-Error""]: " + errorKey + " at " + request.url);
                    try
                    {
                        if (!string.IsNullOrEmpty(request.downloadHandler.text))
                        {
                            SwrveLog.LogError(@"Request response headers [""X-Swrve-Error""]: " +
                                               ((IDictionary<string, object>)Json.Deserialize(request.downloadHandler.text))["message"]);
                        }
                    }
                    catch (Exception e)
                    {
                        SwrveLog.LogError(e.Message);
                    }
                    return WwwDeducedError.ApplicationErrorHeader;
                }
            }

            //Get range Http response code is in.
            if (isSuccessResponseCode(request.responseCode))
            {
                return WwwDeducedError.NoError;
            }
            else if (isUserErrorResponseCode(request.responseCode))
            {
                SwrveLog.LogWarning("Request user error: " + request.error + " in " + request.url);
                return WwwDeducedError.UserError;
            }
            else if (isServerErrorResponseCode(request.responseCode))
            {
                SwrveLog.LogWarning("Request server error: " + request.error + " in " + request.url);
                return WwwDeducedError.ServerError;
            }

            //Fallback to legacy, generic error if no responseCode availble (or in other range)
            if (!string.IsNullOrEmpty(request.error))
            {
                SwrveLog.LogError("Request network error: " + request.error + " in " + request.url);
                return WwwDeducedError.NetworkError;
            }

            return WwwDeducedError.NoError;
        }

        protected static bool isUserErrorResponseCode(long responseCode)
        {
            return (responseCode >= 400 && responseCode < 500);
        }

        protected static bool isSuccessResponseCode(long responseCode)
        {
            return (responseCode >= 200 && responseCode < 300);
        }

        protected static bool isServerErrorResponseCode(long responseCode)
        {
            return (responseCode >= 500);
        }

    }

}

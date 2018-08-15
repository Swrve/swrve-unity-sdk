#if UNITY_IPHONE || UNITY_ANDROID || UNITY_STANDALONE
#define SWRVE_SUPPORTED_PLATFORM
#endif

#if (UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5)
#define SUPPORTS_GZIP_RESPONSES
#endif

using System;
using System.Collections;
using System.IO;
using System.IO.Compression;

using UnityEngine;
#if UNITY_2017_1_OR_NEWER
using UnityEngine.Networking; /** UnityWebRequest **/
#endif
using System.Collections.Generic;
using SwrveUnity.Helpers;
using System.Text;

#if SUPPORTS_GZIP_RESPONSES
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
#endif

namespace SwrveUnity.REST
{
/// <summary>
/// Used internally to connect to REST services.
/// </summary>
public class RESTClient : IRESTClient
{
    private const string CONTENT_ENCODING_HEADER_KEY = "CONTENT-ENCODING";
    private List<string> metrics = new List<string> ();

    public virtual IEnumerator Get (string url, Action<RESTResponse> listener)
    {
        var headers = new Dictionary<string, string> ();
        if (!Application.isEditor) {
            headers = AddMetricsHeader (headers);
#if SUPPORTS_GZIP_RESPONSES
            headers.Add ("Accept-Encoding", "gzip");
#endif
        }
        long start = SwrveHelper.GetMilliseconds ();
#if UNITY_2017_1_OR_NEWER
        using (var www = CrossPlatformUtils.MakeRequest(url, UnityWebRequest.kHttpVerbGET, null, headers)) {
            yield return www.SendWebRequest();
#else
        using (var www = CrossPlatformUtils.MakeWWW(url, null, headers)) {
            yield return www;
#endif

            long wwwTime = SwrveHelper.GetMilliseconds () - start;
            ProcessResponse (www, wwwTime, url, listener);
        }
    }

    public virtual IEnumerator Post (string url, byte[] encodedData, Dictionary<string, string> headers, Action<RESTResponse> listener)
    {
        if (!Application.isEditor) {
            headers = AddMetricsHeader (headers);
        }
        long start = SwrveHelper.GetMilliseconds ();
#if UNITY_2017_1_OR_NEWER
        using (var www = CrossPlatformUtils.MakeRequest(url, UnityWebRequest.kHttpVerbPOST, encodedData, headers)) {
            yield return www.SendWebRequest();
#else
        using (var www = CrossPlatformUtils.MakeWWW(url, encodedData, headers)) {
            yield return www;
#endif
            long wwwTime = SwrveHelper.GetMilliseconds () - start;
            ProcessResponse (www, wwwTime, url, listener);
        }
    }

    protected Dictionary<string, string> AddMetricsHeader (Dictionary<string, string> headers)
    {
        if (metrics.Count > 0) {
            string metricsString = string.Join (";", metrics.ToArray ());
            headers.Add ("Swrve-Latency-Metrics", metricsString);
            metrics.Clear ();
        }
        return headers;
    }

    private void AddMetrics (string url, long wwwTime, bool error)
    {
        Uri uri = new Uri (url);
        url = String.Format ("{0}{1}{2}", uri.Scheme, "://", uri.Authority);

        string metricString;
        if (error) {
            metricString = String.Format ("u={0},c={1},c_error=1", url, wwwTime.ToString ());
        } else {
            metricString = String.Format ("u={0},c={1},sh={1},sb={1},rh={1},rb={1}", url, wwwTime.ToString ());
        }
        metrics.Add (metricString);
    }

#if UNITY_2017_1_OR_NEWER
protected void ProcessResponse (UnityWebRequest www, long wwwTime, string url, Action<RESTResponse> listener)
    {
#if SWRVE_SUPPORTED_PLATFORM
        try {
            if(!www.isNetworkError && !www.isHttpError) {
            // - made it there and it was ok
            string responseBody = null;
            bool success = ResponseBodyTester.TestUTF8 (www.downloadHandler.data, out responseBody);
            Dictionary<string, string> headers = new Dictionary<string, string> ();

            string contentEncodingHeader = null;
                if (www.GetResponseHeaders() != null) {
                    Dictionary<string, string>.Enumerator headersEnum = www.GetResponseHeaders().GetEnumerator();
                while(headersEnum.MoveNext()) {
                    KeyValuePair<string, string> header = headersEnum.Current;
                    headers.Add (header.Key.ToUpper (), header.Value);
                }
                // Get the content encoding header, if present
                if (headers.ContainsKey(CONTENT_ENCODING_HEADER_KEY)) {
                    contentEncodingHeader = headers[CONTENT_ENCODING_HEADER_KEY];
                }
            }
#if SUPPORTS_GZIP_RESPONSES
                // BitConverter.ToInt32 needs at least 4 bytes
                if (www.downloadHandler.data != null && www.downloadHandler.data.Length > 4 && contentEncodingHeader != null && string.Equals (contentEncodingHeader, "gzip", StringComparison.OrdinalIgnoreCase)) {
                    // Check if the response is gzipped or json (eg. iOS automatically unzips it already)
                    if (responseBody != null && !((responseBody.StartsWith ("{") && responseBody.EndsWith ("}")) || (responseBody.StartsWith ("[") && responseBody.EndsWith ("]")))) {
                        int dataLength = BitConverter.ToInt32 (www.bytes, 0);
                        if (dataLength > 0) {
                            var buffer = new byte[dataLength];

                            using (var ms = new MemoryStream(www.bytes)) {
                                using (var gs = new GZipInputStream(ms)) {
                                    gs.Read (buffer, 0, buffer.Length);
                                    gs.Dispose ();
                                }

                                success = ResponseBodyTester.TestUTF8 (buffer, out responseBody);
                                ms.Dispose ();
                            }
                        }
                    }
                }
#endif

                if (success) {
                    AddMetrics (url, wwwTime, false);
                    listener.Invoke (new RESTResponse (responseBody, headers));
                } else {
                    AddMetrics (url, wwwTime, true);
                    listener.Invoke (new RESTResponse (WwwDeducedError.ApplicationErrorBody));
                }
            } else {
                AddMetrics (url, wwwTime, true);
                listener.Invoke (new RESTResponse (UnityWwwHelper.DeduceWwwError (www)));
            }
        } catch(Exception exp) {
             SwrveLog.LogError(exp);
        }
#endif
    }

#else
protected void ProcessResponse (WWW www, long wwwTime, string url, Action<RESTResponse> listener)
    {
#if SWRVE_SUPPORTED_PLATFORM
        try {
            WwwDeducedError deducedError = UnityWwwHelper.DeduceWwwError (www);
            if (deducedError == WwwDeducedError.NoError) {
                // - made it there and it was ok
                string responseBody = null;
                bool success = ResponseBodyTester.TestUTF8 (www.bytes, out responseBody);
                Dictionary<string, string> headers = new Dictionary<string, string> ();

                string contentEncodingHeader = null;
                if (www.responseHeaders != null) {
                    Dictionary<string, string>.Enumerator headersEnum = www.responseHeaders.GetEnumerator();
                    while(headersEnum.MoveNext()) {
                        KeyValuePair<string, string> header = headersEnum.Current;
                        headers.Add (header.Key.ToUpper (), header.Value);
                    }
                    // Get the content encoding header, if present
                    if (headers.ContainsKey(CONTENT_ENCODING_HEADER_KEY)) {
                        contentEncodingHeader = headers[CONTENT_ENCODING_HEADER_KEY];
                    }
                }
#if SUPPORTS_GZIP_RESPONSES
                // BitConverter.ToInt32 needs at least 4 bytes
                if (www.bytes != null && www.bytes.Length > 4 && contentEncodingHeader != null && string.Equals (contentEncodingHeader, "gzip", StringComparison.OrdinalIgnoreCase)) {
                    // Check if the response is gzipped or json (eg. iOS automatically unzips it already)
                    if (responseBody != null && !((responseBody.StartsWith ("{") && responseBody.EndsWith ("}")) || (responseBody.StartsWith ("[") && responseBody.EndsWith ("]")))) {
                        int dataLength = BitConverter.ToInt32 (www.bytes, 0);
                        if (dataLength > 0) {
                            var buffer = new byte[dataLength];

                            using (var ms = new MemoryStream(www.bytes)) {
                                using (var gs = new GZipInputStream(ms)) {
                                    gs.Read (buffer, 0, buffer.Length);
                                    gs.Dispose ();
                                }

                                success = ResponseBodyTester.TestUTF8 (buffer, out responseBody);
                                ms.Dispose ();
                            }
                        }
                    }
                }
#endif
                if (success) {
                    AddMetrics (url, wwwTime, false);
                    listener.Invoke (new RESTResponse (responseBody, headers));
                } else {
                    AddMetrics (url, wwwTime, true);
                    listener.Invoke (new RESTResponse (WwwDeducedError.ApplicationErrorBody));
                }
            } else {
                AddMetrics (url, wwwTime, true);
                listener.Invoke (new RESTResponse (deducedError));
            }
        } catch(Exception exp) {
            SwrveLog.LogError(exp);
        }
#endif
    }
#endif
}

}

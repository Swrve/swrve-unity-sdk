#if UNITY_IPHONE || UNITY_ANDROID || UNITY_STANDALONE
#define SWRVE_SUPPORTED_PLATFORM
#endif

using System;
using System.Collections;
using System.IO;
using System.IO.Compression;

using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using SwrveUnity.Helpers;
using System.Text;

namespace SwrveUnity.REST
{
/// <summary>
/// Used internally to connect to REST services.
/// </summary>
public class RESTClient : IRESTClient
{
    private List<string> metrics = new List<string> ();

    public virtual IEnumerator Get (string url, Action<RESTResponse> listener)
    {
        var headers = new Dictionary<string, string> ();
        if (!Application.isEditor) {
            headers = AddMetricsHeader (headers);
        }
        long start = SwrveHelper.GetMilliseconds ();
        using (var www = CrossPlatformUtils.MakeRequest(url, UnityWebRequest.kHttpVerbGET, null, headers)) {
            yield return www.SendWebRequest();

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
        using (var www = CrossPlatformUtils.MakeRequest(url, UnityWebRequest.kHttpVerbPOST, encodedData, headers)) {
            yield return www.SendWebRequest();
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

    protected void ProcessResponse (UnityWebRequest www, long wwwTime, string url, Action<RESTResponse> listener)
    {
#if SWRVE_SUPPORTED_PLATFORM
        try {
#if UNITY_2020_1_OR_NEWER
            if (www.result != UnityWebRequest.Result.ConnectionError) {
#else
            if (!www.isNetworkError) {
#endif
                // - made it there and it was ok
                string responseBody = null;
                bool success = ResponseBodyTester.TestUTF8 (www.downloadHandler.data, out responseBody);
                Dictionary<string, string> headers = new Dictionary<string, string> ();

                if (www.GetResponseHeaders() != null) {
                    Dictionary<string, string>.Enumerator headersEnum = www.GetResponseHeaders().GetEnumerator();
                    while(headersEnum.MoveNext()) {
                        KeyValuePair<string, string> header = headersEnum.Current;
                        headers.Add (header.Key.ToUpper (), header.Value);
                    }
                }

                if (success) {
                    AddMetrics (url, wwwTime, false);
                    listener.Invoke (new RESTResponse(error: UnityWwwHelper.DeduceWwwError (www), responseCode: www.responseCode, responseBody: responseBody, headers: headers) );
                } else {
                    AddMetrics (url, wwwTime, true);
                    listener.Invoke (new RESTResponse (error: WwwDeducedError.ApplicationErrorBody, responseCode: www.responseCode, responseBody: responseBody, headers: headers));
                }
            } else {
                AddMetrics (url, wwwTime, true);
                listener.Invoke (new RESTResponse (error: UnityWwwHelper.DeduceWwwError (www)));
            }
        }
        catch(Exception exp) {
            SwrveLog.LogError(exp);
        }
#endif
    }
}

}

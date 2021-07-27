using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SwrveUnity.Helpers;
using SwrveUnity.REST;
using SwrveUnityMiniJSON;
using UnityEngine;
namespace SwrveUnity
{
/// <summary>
/// Used internally by SwrveQaUser to queue/send Qauser logs.
/// </summary>
public class SwrveQaUserQueue : ISwrveQaUserQueue
{
    // Networking Layer
    protected IRESTClient restClient;

    // Queue properties
    protected float queueFlushDelay = 4.0f;
    protected IEnumerator flushTimercoroutine;
    public List<Dictionary<string, object>> qaLogEventQueued = null;

    // External references for the QA request.
    // protected SwrveSDK swrve;
    protected MonoBehaviour container;
    protected string endPoint;
    protected string apiKey;
    protected int appId;
    protected string userId;
    protected string appVersion;
    protected string deviceUUID;

    public SwrveQaUserQueue(MonoBehaviour container, string eventServer, string apiKey, int appId, string userId, string appVersion, string deviceUUID)
    {
        this.container = container;
        this.endPoint = eventServer + "/1/batch";
        this.apiKey = apiKey;
        this.appId = appId;
        this.userId = userId;
        this.appVersion = appVersion;
        this.deviceUUID = deviceUUID;
        qaLogEventQueued = new List<Dictionary<string, object>>();
        restClient = new RESTClient ();
    }

    #region interfaceISwrveQaUserQueue

    public void Queue(Dictionary<string, object> qaLogEvent)
    {
        // Sanity check about some must have fields for a valid QA Event.
        if (!qaLogEvent.ContainsKey("log_type") &&
                !qaLogEvent.ContainsKey("log_source") &&
                !qaLogEvent.ContainsKey("log_details")) {
            return;
        }
        lock (qaLogEventQueued) {
            qaLogEventQueued.Add(qaLogEvent);
            if (flushTimercoroutine == null) {
                flushTimercoroutine = FlushEventsCoroutine();
                container.StartCoroutine(flushTimercoroutine);
            }
        }
    }

    public void FlushEvents()
    {
        string qaEventsStringJson = null;
        lock (qaLogEventQueued) {
            if (qaLogEventQueued.Count > 0) {
                qaEventsStringJson = SwrveUnityMiniJSON.Json.Serialize (this.qaLogEventQueued);
                SwrveLog.LogInfo("Swrve: will flush the QA queue, total of " + qaLogEventQueued.Count + "events") ;
                qaLogEventQueued.Clear();
            }
        }
        MakeRequest(qaEventsStringJson);
    }

    #endregion

    #region Queue Coroutine

    public IEnumerator FlushEventsCoroutine()
    {
        while (qaLogEventQueued.Count > 0) {
            yield return new WaitForSeconds(queueFlushDelay);
            FlushEvents();
        }
        if (flushTimercoroutine != null) {
            container.StopCoroutine(flushTimercoroutine);
            flushTimercoroutine = null;
        }
    }

    #endregion

    #region Networking Layer

    private void MakeRequest (string qaEventsStringJson)
    {
        if (qaEventsStringJson == null) {
            return;
        }

        byte[] qaPostEncodedData = null;
        qaPostEncodedData = PostBodyBuilder.BuildQaEvent(apiKey, appId, userId, this.deviceUUID, appVersion, SwrveHelper.GetMilliseconds(), qaEventsStringJson);

        Dictionary<string, string> requestHeaders = new Dictionary<string, string> {
            { @"Content-Type", @"application/json; charset=utf-8" }
        };

        if(qaPostEncodedData != null) {
            container.StartCoroutine (restClient.Post (endPoint, qaPostEncodedData, requestHeaders, RestListener));
            SwrveLog.LogInfo("Swrve: SwrveQa Json Event sent:" + qaEventsStringJson);
        }
    }

    private void RestListener (RESTResponse response)
    {
        if (response.Error != WwwDeducedError.NoError) {
            SwrveLog.LogError ("QA request to failed with error code " + response.Error.ToString () + ": " + response.Body);
        }
    }

    #endregion
}
}

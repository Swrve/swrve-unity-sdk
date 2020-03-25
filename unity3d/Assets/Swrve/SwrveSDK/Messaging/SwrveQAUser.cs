using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Collections;
using SwrveUnityMiniJSON;
using SwrveUnity.REST;
using SwrveUnity.Helpers;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Used internally to offer QA user functionality.
/// </summary>
public class SwrveQAUser
{
    private const int ApiVersion = 1;
    private const long SessionInterval = 1000;
    private const long TriggerInterval = 500;
    private const long PushNotificationInterval = 1000;
    private const string PushTrackingKey = "_p";

    private readonly SwrveSDK swrve;
    private readonly IRESTClient restClient;
    private readonly string loggingUrl;
    private long lastSessionRequestTime;
    private long lastTriggerRequestTime;
    private long lastPushNotificationRequestTime = 0;

    public readonly bool ResetDevice;
    public readonly bool Logging;

    public Dictionary<int, string> campaignReasons = new Dictionary<int, string> ();
    public Dictionary<int, SwrveBaseMessage> campaignMessages = new Dictionary<int, SwrveBaseMessage> ();

    public SwrveQAUser (SwrveSDK swrve, Dictionary<string, object> jsonQa)
    {
        this.swrve = swrve;
        this.ResetDevice = MiniJsonHelper.GetBool (jsonQa, "reset_device_state", false);
        this.Logging = MiniJsonHelper.GetBool (jsonQa, "logging", false);
        if (Logging) {
            restClient = new RESTClient ();
            this.loggingUrl = MiniJsonHelper.GetString (jsonQa, "logging_url", null);

            this.loggingUrl = this.loggingUrl.Replace ("http://", "https://");

            if (!this.loggingUrl.EndsWith ("/")) {
                this.loggingUrl = this.loggingUrl + "/";
            }
        }

        campaignReasons = new Dictionary<int, string> ();
        campaignMessages = new Dictionary<int, SwrveBaseMessage> ();
    }

    protected string getEndpoint(string path)
    {
        while (path.StartsWith ("/")) {
            path = path.Substring (1);
        }
        return this.loggingUrl + path;
    }

    public void TalkSession (Dictionary<int, string> campaignsDownloaded)
    {
        try {
            if (CanMakeSessionRequest ()) {
                lastSessionRequestTime = SwrveHelper.GetMilliseconds();
                String endpoint = getEndpoint("talk/game/" + swrve.ApiKey + "/user/" + swrve.UserId + "/session");
                Dictionary<string, object> talkSessionJson = new Dictionary<string, object> ();

                // Add campaigns (downloaded or not) to request
                IList<object> campaignsJson = new List<object> ();
                Dictionary<int, string>.Enumerator campaignIt = campaignsDownloaded.GetEnumerator ();
                while (campaignIt.MoveNext()) {
                    int id = campaignIt.Current.Key;
                    string reason = campaignIt.Current.Value;

                    Dictionary<string, object> campaignInfo = new Dictionary<string, object> ();
                    campaignInfo.Add ("id", id);
                    campaignInfo.Add ("reason", (reason == null) ? string.Empty : reason);
                    campaignInfo.Add ("loaded", (reason == null));
                    campaignsJson.Add (campaignInfo);
                }
                talkSessionJson.Add ("campaigns", campaignsJson);
                // Add device info to request
                Dictionary<string, string> deviceJson = swrve.GetDeviceInfo ();
                talkSessionJson.Add ("device", deviceJson);

                MakeRequest (endpoint, talkSessionJson);
            }
        } catch (Exception exp) {
            SwrveLog.LogError ("QA request talk session failed: " + exp.ToString ());
        }
    }

    public void UpdateDeviceInfo ()
    {
        try {
            if (CanMakeRequest ()) {
                String endpoint = getEndpoint("talk/game/" + swrve.ApiKey + "/user/" + swrve.UserId + "/device_info");
                Dictionary<string, object> deviceJson = new Dictionary<string, object> ();
                Dictionary<string, string> deviceData = swrve.GetDeviceInfo ();
                Dictionary<string, string>.Enumerator deviceDataEnum = deviceData.GetEnumerator();
                while(deviceDataEnum.MoveNext()) {
                    deviceJson.Add (deviceDataEnum.Current.Key, deviceDataEnum.Current.Value);
                }
                MakeRequest (endpoint, deviceJson);
            }
        } catch (Exception exp) {
            SwrveLog.LogError ("QA request talk device info update failed: " + exp.ToString ());
        }
    }

    private void MakeRequest (string endpoint, Dictionary<string, object> json)
    {
        json.Add ("version", ApiVersion);
        json.Add ("client_time", DateTime.UtcNow.ToString (@"yyyy-MM-ddTHH\:mm\:ss.fffffffzzz"));
        string qaPostData = SwrveUnityMiniJSON.Json.Serialize (json);

        byte[] qaPostEncodedData = Encoding.UTF8.GetBytes (qaPostData);
        Dictionary<string, string> requestHeaders = new Dictionary<string, string> {
            { @"Content-Type", @"application/json; charset=utf-8" }
        };
        swrve.Container.StartCoroutine (restClient.Post (endpoint, qaPostEncodedData, requestHeaders, RestListener));
    }

    public void TriggerFailure (string eventName, string globalReason)
    {
        try {
            if (CanMakeTriggerRequest () && !string.IsNullOrEmpty(eventName)) {
                string endpoint = getEndpoint("talk/game/" + swrve.ApiKey + "/user/" + swrve.UserId + "/trigger");
                Dictionary<string, object> triggerJson = new Dictionary<string, object> ();
                triggerJson.Add ("trigger_name", eventName);
                triggerJson.Add ("displayed", false);
                triggerJson.Add ("reason", globalReason);
                triggerJson.Add ("campaigns", new List<object> ());

                MakeRequest (endpoint, triggerJson);
            }
        } catch (Exception exp) {
            SwrveLog.LogError ("QA request talk session failed: " + exp.ToString ());
        }
    }

    public void Trigger (string eventName, SwrveBaseMessage baseMessage)
    {
        try {
            if (CanMakeTriggerRequest () && !string.IsNullOrEmpty(eventName)) {

                lastTriggerRequestTime = SwrveHelper.GetMilliseconds ();

                Dictionary<int, string> _reasons = campaignReasons;
                Dictionary<int, SwrveBaseMessage> _messages = campaignMessages;
                campaignReasons = new Dictionary<int, string>();
                campaignMessages = new Dictionary<int, SwrveBaseMessage>();

                String endpoint = getEndpoint("talk/game/" + swrve.ApiKey + "/user/" + swrve.UserId + "/trigger");
                Dictionary<string, object> triggerJson = new Dictionary<string, object> ();
                triggerJson.Add ("trigger_name", eventName);
                triggerJson.Add ("displayed", baseMessage != null);
                triggerJson.Add ("reason", baseMessage == null ? "The loaded campaigns returned no conversation or message" : string.Empty);

                // Add campaigns that were not displayed
                IList<object> campaignsJson = new List<object> ();
                Dictionary<int, string>.Enumerator campaignIt = _reasons.GetEnumerator ();
                while (campaignIt.MoveNext()) {
                    int campaignId = campaignIt.Current.Key;
                    String reason = campaignIt.Current.Value;

                    Dictionary<string, object> campaignInfo = new Dictionary<string, object> ();
                    campaignInfo.Add ("id", campaignId);
                    campaignInfo.Add ("displayed", false);
                    campaignInfo.Add ("reason", (reason == null) ? string.Empty : reason);

                    if (_messages.ContainsKey (campaignId)) {
                        SwrveBaseMessage _baseMessage = _messages [campaignId];
                        campaignInfo.Add (_baseMessage.GetBaseMessageType() + "_id", _baseMessage.Id);
                    }
                    campaignsJson.Add (campaignInfo);
                }

                // Add campaign that was shown, if available
                if(baseMessage != null) {
                    Dictionary<string, object> campaignInfo = new Dictionary<string, object> ();
                    campaignInfo.Add ("id", baseMessage.Campaign.Id);
                    campaignInfo.Add ("displayed", true);
                    campaignInfo.Add (baseMessage.GetBaseMessageType() + "_id", baseMessage.Id);
                    campaignInfo.Add ("reason", string.Empty);

                    campaignsJson.Add (campaignInfo);
                }
                triggerJson.Add ("campaigns", campaignsJson);

                MakeRequest (endpoint, triggerJson);
            }
        } catch (Exception exp) {
            SwrveLog.LogError ("QA request talk session failed: " + exp.ToString ());
        }
    }

#if UNITY_IPHONE
    public void PushNotification (UnityEngine.iOS.RemoteNotification notification)
    {
        try {
            String endpoint = getEndpoint("talk/game/" + swrve.ApiKey + "/user/" + swrve.UserId + "/push");

            if (CanMakePushNotificationRequest()) {
                lastPushNotificationRequestTime = SwrveHelper.GetMilliseconds ();

                Dictionary<string, object> pushJson = new Dictionary<string, object>();
                pushJson.Add("alert", notification.alertBody);
                pushJson.Add("sound", notification.soundName);
                pushJson.Add("badge", notification.applicationIconBadgeNumber);

                if (notification.userInfo != null && notification.userInfo.Contains(PushTrackingKey)) {
                    string pushId = notification.userInfo[PushTrackingKey].ToString();
                    pushJson.Add("id", pushId);
                }

                MakeRequest(endpoint, pushJson);
            }
        } catch(Exception exp) {
            SwrveLog.LogError("QA request talk session failed: " + exp.ToString());
        }
    }
#endif

    private bool CanMakeRequest ()
    {
        return (swrve != null && Logging);
    }

    private bool CanMakeTimedRequest(long lastTime, long intervalTime)
    {
        if (CanMakeRequest ()) {
            if (lastTime == 0 || (SwrveHelper.GetMilliseconds () - lastTime) > SessionInterval) {
                return true;
            }
        }

        return false;
    }

    private bool CanMakeSessionRequest ()
    {
        return CanMakeTimedRequest (lastSessionRequestTime, SessionInterval);
    }

    private bool CanMakeTriggerRequest ()
    {
        return CanMakeTimedRequest (lastTriggerRequestTime, TriggerInterval);
    }

    private bool CanMakePushNotificationRequest ()
    {
        return CanMakeTimedRequest (lastPushNotificationRequestTime, PushNotificationInterval);
    }

    private void RestListener (RESTResponse response)
    {
        if (response.Error != WwwDeducedError.NoError) {
            SwrveLog.LogError ("QA request to failed with error code " + response.Error.ToString () + ": " + response.Body);
        }
    }
}
}

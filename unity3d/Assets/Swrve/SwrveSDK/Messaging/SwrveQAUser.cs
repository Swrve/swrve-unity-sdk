/*
 * SWRVE CONFIDENTIAL
 * 
 * (c) Copyright 2010-2014 Swrve New Media, Inc. and its licensors.
 * All Rights Reserved.
 *
 * NOTICE: All information contained herein is and remains the property of Swrve
 * New Media, Inc or its licensors.  The intellectual property and technical
 * concepts contained herein are proprietary to Swrve New Media, Inc. or its
 * licensors and are protected by trade secret and/or copyright law.
 * Dissemination of this information or reproduction of this material is
 * strictly forbidden unless prior written permission is obtained from Swrve.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Collections;
using SwrveMiniJSON;
using Swrve.REST;
using Swrve.Helpers;

namespace Swrve.Messaging
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

    private readonly SwrveSDK swrve;
    private readonly IRESTClient restClient;
    private readonly string loggingUrl;
    private long lastSessionRequestTime;
    private long lastTriggerRequestTime;
    private long lastPushNotificationRequestTime;

    public readonly bool ResetDevice;
    public readonly bool Logging;

    public SwrveQAUser (SwrveSDK swrve, Dictionary<string, object> jsonQa)
    {
        this.swrve = swrve;
        this.ResetDevice = MiniJsonHelper.GetBool (jsonQa, "reset_device_state", false);
        this.Logging = MiniJsonHelper.GetBool (jsonQa, "logging", false);
        if (Logging) {
            restClient = new RESTClient ();
            this.loggingUrl = MiniJsonHelper.GetString (jsonQa, "logging_url", null);
        }
    }

    public void TalkSession (Dictionary<int, string> campaignsDownloaded)
    {
        try {
            if (CanMakeSessionRequest ()) {
                String endpoint = loggingUrl + "/talk/game/" + swrve.ApiKey + "/user/" + swrve.UserId + "/session";
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
                String endpoint = loggingUrl + "/talk/game/" + swrve.ApiKey + "/user/" + swrve.UserId + "/device_info";
                Dictionary<string, object> deviceJson = new Dictionary<string, object> ();
                Dictionary<string, string> deviceData = swrve.GetDeviceInfo ();
                foreach (String key in deviceData.Keys) {
                    deviceJson.Add (key, deviceData [key]);
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
        string qaPostData = SwrveMiniJSON.Json.Serialize (json);

        byte[] qaPostEncodedData = Encoding.UTF8.GetBytes (qaPostData);
        Dictionary<string, string> requestHeaders = new Dictionary<string, string> {
            { @"Content-Type", @"application/json; charset=utf-8" },
            { @"Content-Length", qaPostEncodedData.Length.ToString () }
        };

        swrve.Container.StartCoroutine (restClient.Post (endpoint, qaPostEncodedData, requestHeaders, RestListener));
    }

    public void TriggerFailure (string eventName, string globalReason)
    {
        try {
            if (CanMakeTriggerRequest ()) {
                string endpoint = loggingUrl + "/talk/game/" + swrve.ApiKey + "/user/" + swrve.UserId + "/trigger";
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

    public void Trigger (string eventName, SwrveMessage messageShown, Dictionary<int, string> campaignReasons, Dictionary<int, int> campaignMessages)
    {
        try {
            if (CanMakeTriggerRequest ()) {
                String endpoint = loggingUrl + "/talk/game/" + swrve.ApiKey + "/user/" + swrve.UserId + "/trigger";
                Dictionary<string, object> triggerJson = new Dictionary<string, object> ();
                triggerJson.Add ("trigger_name", eventName);
                triggerJson.Add ("displayed", (messageShown != null));
                triggerJson.Add ("reason", (messageShown == null) ? "The loaded campaigns returned no message" : string.Empty);

                // Add campaigns that were not displayed
                IList<object> campaignsJson = new List<object> ();
                Dictionary<int, string>.Enumerator campaignIt = campaignReasons.GetEnumerator ();
                while (campaignIt.MoveNext()) {
                    int campaignId = campaignIt.Current.Key;
                    String reason = campaignIt.Current.Value;

                    int? messageId = null;
                    if (campaignMessages.ContainsKey (campaignId)) {
                        messageId = campaignMessages [campaignId];
                    }

                    Dictionary<string, object> campaignInfo = new Dictionary<string, object> ();
                    campaignInfo.Add ("id", campaignId);
                    campaignInfo.Add ("displayed", false);
                    campaignInfo.Add ("message_id", (messageId == null) ? -1 : messageId);
                    campaignInfo.Add ("reason", (reason == null) ? string.Empty : reason);
                    campaignsJson.Add (campaignInfo);
                }

                // Add campaign that was shown, if available
                if (messageShown != null) {
                    Dictionary<string, object> campaignInfo = new Dictionary<string, object> ();
                    campaignInfo.Add ("id", messageShown.Campaign.Id);
                    campaignInfo.Add ("displayed", true);
                    campaignInfo.Add ("message_id", messageShown.Id);
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

    private bool CanMakeRequest ()
    {
        return (swrve != null && Logging);
    }

    private bool CanMakeSessionRequest ()
    {
        if (CanMakeRequest ()) {
            long currentTime = SwrveHelper.GetMilliseconds ();
            if (lastSessionRequestTime == 0 || (currentTime - lastSessionRequestTime) > SessionInterval) {
                lastSessionRequestTime = currentTime;
                return true;
            }
        }

        return false;
    }

    private bool CanMakeTriggerRequest ()
    {
        if (CanMakeRequest ()) {
            long currentTime = SwrveHelper.GetMilliseconds ();
            if (lastTriggerRequestTime == 0 || (currentTime - lastTriggerRequestTime) > TriggerInterval) {
                lastTriggerRequestTime = currentTime;
                return true;
            }
        }

        return false;
    }

#if UNITY_IPHONE
    public void PushNotification (RemoteNotification[] notifications, int count)
    {
        try {
            String endpoint = loggingUrl + "/talk/game/" + swrve.ApiKey + "/user/" + swrve.UserId + "/push";

            for(int i = 0; i < count; i++) {
                if (CanMakePushNotificationRequest()) {
                    Dictionary<string, object> pushJson = new Dictionary<string, object>();
                    RemoteNotification notification = notifications[i];
                    pushJson.Add("alert", notification.alertBody);
                    pushJson.Add("sound", notification.soundName);
                    pushJson.Add("badge", notification.applicationIconBadgeNumber);

                    if (notification.userInfo != null && notification.userInfo.Contains("_p")) {
                        string pushId = notification.userInfo["_p"].ToString();
                        pushJson.Add("id", pushId);
                    }

                    MakeRequest(endpoint, pushJson);
                }
            }
        } catch(Exception exp) {
            SwrveLog.LogError("QA request talk session failed: " + exp.ToString());
        }
    }
#endif

    private bool CanMakePushNotificationRequest ()
    {
        if (swrve != null && Logging) {
            long currentTime = SwrveHelper.GetMilliseconds ();
            if (lastPushNotificationRequestTime == 0 || (currentTime - lastPushNotificationRequestTime) > PushNotificationInterval) {
                lastPushNotificationRequestTime = currentTime;
                return true;
            }
        }

        return false;
    }

    private void RestListener (RESTResponse response)
    {
        if (response.Error != WwwDeducedError.NoError) {
            SwrveLog.LogError ("QA request to failed with error code " + response.Error.ToString () + ": " + response.Body);
        }
    }
}
}


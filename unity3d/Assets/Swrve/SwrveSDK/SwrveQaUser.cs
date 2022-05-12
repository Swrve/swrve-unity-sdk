using System;
using System.Collections.Generic;
using SwrveUnity.Helpers;
using SwrveUnityMiniJSON;
using UnityEngine;

namespace SwrveUnity
{
    /// <summary>
    /// Used internally to queue/send Qauser logs.
    /// </summary>
    public class SwrveQaUser
    {
        private const string LogSourceSDK = "sdk";
        private const string QaLogType = "qa_log_event";
        protected static readonly string QaUserSave = "swrve.q2"; // Saved securely. Must be the same as native qa key.
        private string userId;
        private ISwrveStorage storage;

        public bool resetDevice;
        public bool loggingEnabled;

        public ISwrveQaUserQueue qaUserQueue;

        protected static SwrveQaUser instance;
        private static readonly object OBJECT_LOCK = new object();

        public static SwrveQaUser Instance
        {
            get
            {
                lock (OBJECT_LOCK)
                {
                    if (instance == null)
                    {
                        instance = new SwrveQaUser();
                    }
                    return instance;
                }
            }
            set
            {
                lock (OBJECT_LOCK)
                {
                    instance = value;
                }
            }
        }

        public static void Init(MonoBehaviour container, string eventServer, string apiKey, int appId, string userId, string appVersion, string deviceUUID, ISwrveStorage storage)
        {
            if (Instance.qaUserQueue != null)
            {
                Instance.qaUserQueue.FlushEvents();
            }

            SwrveQaUser.Instance.qaUserQueue = new SwrveQaUserQueue(container, eventServer, apiKey, appId, userId, appVersion, deviceUUID);
            SwrveQaUser.Instance.userId = userId;
            SwrveQaUser.Instance.storage = storage;

            Dictionary<string, object> qaUserDictionary = null;
            qaUserDictionary = (Dictionary<string, object>)Json.Deserialize(instance.LoadQaUserFromCache());
            SwrveQaUser.Update(qaUserDictionary);

        }

        public static void Update(Dictionary<string, object> qaUserDictionary)
        {
            if (qaUserDictionary == null)
            {
                SwrveQaUser.Instance.loggingEnabled = false;
                SwrveQaUser.Instance.resetDevice = false;
            }
            else
            {
                if (qaUserDictionary.ContainsKey("logging"))
                {
                    SwrveQaUser.Instance.loggingEnabled = (bool)qaUserDictionary["logging"];

                    if (SwrveQaUser.Instance.loggingEnabled)
                    {
                        SwrveLog.LogInfo("Swrve: user just updated as QaUser!");
                    }

                }
                if (qaUserDictionary.ContainsKey("reset_device_state"))
                {
                    SwrveQaUser.Instance.resetDevice = (bool)qaUserDictionary["reset_device_state"];
                }
            }
        }

        private string LoadQaUserFromCache()
        {
            string qaCached = null;
            qaCached = storage.Load(QaUserSave, SwrveQaUser.Instance.userId);
            return qaCached;
        }

        public static void SaveQaUser(Dictionary<string, object> qaUserDictionary)
        {
            if (SwrveQaUser.Instance != null)
            {
                string qaJson = "";
                string userID = SwrveQaUser.Instance.userId;
                if (qaUserDictionary != null && qaUserDictionary.Count > 0)
                {
                    qaJson = SwrveUnityMiniJSON.Json.Serialize(qaUserDictionary);
                }
                instance.storage.SaveSecure(QaUserSave, qaJson, userID);
            }
        }


        public static void CampaignsDownloaded(List<SwrveQaUserCampaignInfo> campaignInfoList)
        {
            if (!CanLog())
            {
                return;
            }

            try
            {
                List<Dictionary<string, object>> logDetailsCampaignsList = new List<Dictionary<string, object>>();
                for (int i = 0; i < campaignInfoList.Count; ++i)
                {
                    SwrveQaUserCampaignInfo qaUserCampaignInfo = campaignInfoList[i];
                    Dictionary<string, object> logDetailsCampaigns = new Dictionary<string, object>();
                    logDetailsCampaigns.Add("id", qaUserCampaignInfo.id);
                    logDetailsCampaigns.Add("variant_id", qaUserCampaignInfo.variantId);
                    logDetailsCampaigns.Add("type", qaUserCampaignInfo.type.Value);
                    logDetailsCampaignsList.Add(logDetailsCampaigns);
                }

                Dictionary<string, object> logDetails = new Dictionary<string, object>();
                logDetails.Add("campaigns", logDetailsCampaignsList);

                SwrveQaUser qaUser = SwrveQaUser.Instance;
                qaUser.QueueQaLogEvent("campaigns-downloaded", logDetails);
            }
            catch (Exception ex)
            {
                SwrveLog.LogError("SwrveQaUser: CampaignsDownloaded exception:" + ex.ToString());
            }
        }

        public static void CampaignTriggeredConversationNoDisplay(string eventName, IDictionary<string, string> eventPayload)
        {
            if (!CanLog())
            {
                return;
            }
            SwrveQaUser qaUser = SwrveQaUser.Instance;
            string reason = "No Conversation triggered because In App Message displayed";
            qaUser.CampaignTriggered(eventName, eventPayload, false, reason);
        }

        public static void CampaignTriggeredConversation(string eventName, IDictionary<string, string> eventPayload, bool displayed, List<SwrveQaUserCampaignInfo> campaignInfoList)
        {
            if (!CanLog())
            {
                return;
            }

            try
            {
                SwrveQaUser qaUser = SwrveQaUser.Instance;
                string noCampaignTriggeredReason = displayed ? "" : "The loaded campaigns returned no conversation";
                qaUser.CampaignTriggered(eventName, eventPayload, displayed, noCampaignTriggeredReason, campaignInfoList);
            }
            catch (Exception ex)
            {
                SwrveLog.LogError("SwrveQaUser: CampaignTriggeredConversation exception:" + ex.ToString());
            }
        }

        public static void CampaignTriggeredMessage(string eventName, IDictionary<string, string> eventPayload, bool displayed, List<SwrveQaUserCampaignInfo> campaignInfoList)
        {
            if (!CanLog())
            {
                return;
            }

            try
            {
                SwrveQaUser qaUser = SwrveQaUser.Instance;
                string noCampaignTriggeredReason = displayed ? "" : "The loaded campaigns returned no message";
                qaUser.CampaignTriggered(eventName, eventPayload, displayed, noCampaignTriggeredReason, campaignInfoList);
            }
            catch (Exception ex)
            {
                SwrveLog.LogError("SwrveQaUser: CampaignTriggeredMessage exception:" + ex.ToString());
            }
        }

        private void CampaignTriggered(string eventName, IDictionary<string, string> eventPayload, bool displayed, string reason, List<SwrveQaUserCampaignInfo> campaignInfoList = null)
        {
            Dictionary<string, object> logDetails = new Dictionary<string, object>();
            logDetails.Add("event_name", eventName);
            if (eventPayload == null)
            {
                eventPayload = new Dictionary<string, string>();
            }

            if (campaignInfoList == null)
            {
                campaignInfoList = new List<SwrveQaUserCampaignInfo>();
            }

            logDetails.Add("event_payload", eventPayload);
            logDetails.Add("displayed", displayed);
            logDetails.Add("reason", reason);

            List<Dictionary<string, object>> logDetailsCampaignsList = new List<Dictionary<string, object>>();
            for (int i = 0; i < campaignInfoList.Count; ++i)
            {
                SwrveQaUserCampaignInfo qaUserCampaignInfo = campaignInfoList[i];
                Dictionary<string, object> logDetailsCampaigns = new Dictionary<string, object>();
                logDetailsCampaigns.Add("id", qaUserCampaignInfo.id);
                logDetailsCampaigns.Add("variant_id", qaUserCampaignInfo.variantId);
                logDetailsCampaigns.Add("type", qaUserCampaignInfo.type.Value);
                logDetailsCampaigns.Add("displayed", qaUserCampaignInfo.displayed);
                logDetailsCampaigns.Add("reason", qaUserCampaignInfo.reason);
                logDetailsCampaignsList.Add(logDetailsCampaigns);
            }
            logDetails.Add("campaigns", logDetailsCampaignsList);

            SwrveQaUser qaUser = SwrveQaUser.Instance;
            qaUser.QueueQaLogEvent("campaign-triggered", logDetails);
        }

        public static void CampaignButtonClicked(int campaignId, int variantId, string buttonName, string actionType, string actionValue)
        {
            if (!CanLog())
            {
                return;
            }

            try
            {
                Dictionary<string, object> logDetails = new Dictionary<string, object>();
                logDetails.Add("campaign_id", campaignId);
                logDetails.Add("variant_id", variantId);
                logDetails.Add("button_name", buttonName);
                logDetails.Add("action_type", actionType);
                logDetails.Add("action_value", actionValue);

                SwrveQaUser qaUser = SwrveQaUser.Instance;
                qaUser.QueueQaLogEvent("campaign-button-clicked", logDetails);
            }
            catch (Exception ex)
            {
                SwrveLog.LogError("SwrveQaUser: CampaignButtonClicked exception:" + ex.ToString());
            }
        }

        public static void AssetFailedToDownload(string assetName, string resolvedUrl, string reason)
        {
            if (!CanLog())
            {
                return;
            }

            try
            {
                Dictionary<string, object> logDetails = new Dictionary<string, object>();
                logDetails.Add("asset_name", assetName);
                logDetails.Add("image_url", resolvedUrl);
                logDetails.Add("reason", reason);

                SwrveQaUser qaUser = SwrveQaUser.Instance;
                qaUser.QueueQaLogEvent("asset-failed-to-download", logDetails);
            }
            catch (Exception ex)
            {
                SwrveLog.LogError("SwrveQaUser: Asset failed to download exception:" + ex.ToString());
            }
        }

        public static void AssetFailedToDisplay(int campaignId, int variantId, string assetName, string unresolvedUrl, string resolvedUrl, bool hasFallback, string reason)
        {
            if (!CanLog())
            {
                return;
            }

            try
            {
                Dictionary<string, object> logDetails = new Dictionary<string, object>();
                logDetails.Add("campaign_id", campaignId);
                logDetails.Add("variant_id", variantId);
                logDetails.Add("unresolved_url", unresolvedUrl);
                logDetails.Add("has_fallback", hasFallback);
                logDetails.Add("reason", reason);

                if (!string.IsNullOrEmpty(resolvedUrl))
                {
                    logDetails.Add("image_url", resolvedUrl);
                }

                if (!string.IsNullOrEmpty(assetName))
                {
                    logDetails.Add("asset_name", assetName);
                }

                SwrveQaUser qaUser = SwrveQaUser.Instance;
                qaUser.QueueQaLogEvent("asset-failed-to-display", logDetails);
            }
            catch (Exception ex)
            {
                SwrveLog.LogError("SwrveQaUser: Asset failed to display exception:" + ex.ToString());
            }
        }

        public static void EmbeddedPersonalizationFailed(int campaignId, int variantId, string unresolvedData, string reason)
        {
            if (!CanLog())
            {
                return;
            }

            try
            {
                Dictionary<string, object> logDetails = new Dictionary<string, object>();
                logDetails.Add("campaign_id", campaignId);
                logDetails.Add("variant_id", variantId);
                logDetails.Add("unresolved_data", unresolvedData);
                logDetails.Add("reason", reason);

                SwrveQaUser qaUser = SwrveQaUser.Instance;
                qaUser.QueueQaLogEvent("embedded-personalization-failed", logDetails);
            }
            catch (Exception ex)
            {
                SwrveLog.LogError("SwrveQaUser: Embedded personalization failed exception:" + ex.ToString());
            }
        }

        public static void WrappedEvent(Dictionary<string, object> eventQueued)
        {
            if (!CanLog())
            {
                return;
            }

            try
            {
                // build dictionary of logDetails.
                Dictionary<string, object> logDetails = new Dictionary<string, object>();

                if (eventQueued.ContainsKey("type"))
                {
                    logDetails.Add("type", eventQueued["type"]);
                    eventQueued.Remove("type");
                }
                if (eventQueued.ContainsKey("seqnum"))
                {
                    logDetails.Add("seqnum", eventQueued["seqnum"]);
                    eventQueued.Remove("seqnum");
                }
                if (eventQueued.ContainsKey("time"))
                {
                    logDetails.Add("client_time", eventQueued["time"]);
                    eventQueued.Remove("time");
                }
                string payloadString = "{}"; // babble currently only accepting payload jsonobject as a string, and not a proper jsonobject
                if (eventQueued.ContainsKey("payload"))
                {
                    if (eventQueued["payload"] is Dictionary<string, string>)
                    {
                        Dictionary<string, string> payloadDictionary = (Dictionary<string, string>)eventQueued["payload"];
                        payloadString = Json.Serialize(payloadDictionary);
                    }
                    eventQueued.Remove("payload");
                }
                logDetails.Add("payload", payloadString);

                // add remaining details as parameters
                logDetails.Add("parameters", eventQueued); // parameters required even if empty

                SwrveQaUser qaUser = SwrveQaUser.Instance;
                qaUser.QueueQaLogEvent("event", logDetails);
            }
            catch (Exception ex)
            {
                SwrveLog.LogError("SwrveQaUser: WrappedEvent exception:" + ex.ToString());
            }
        }

        private static bool CanLog()
        {
            bool canLog = true;
            SwrveQaUser qaUser = SwrveQaUser.Instance;
            if (qaUser == null || !qaUser.loggingEnabled)
            {
                canLog = false;
            }
            return canLog;
        }

        public virtual void QueueQaLogEvent(string logType, Dictionary<string, object> logDetails)
        {
            Dictionary<string, object> qaLogEvent = new Dictionary<string, object>();
            qaLogEvent.Add("time", GetTime());
            qaLogEvent.Add("type", QaLogType);
            qaLogEvent.Add("log_source", LogSourceSDK);
            qaLogEvent.Add("log_type", logType);
            qaLogEvent.Add("log_details", logDetails);

            qaUserQueue.Queue(qaLogEvent);
        }

        protected virtual long GetTime()
        {
            return SwrveHelper.GetMilliseconds();
        }
    }
}

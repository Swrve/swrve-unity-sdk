using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using SwrveUnity;
using SwrveUnityMiniJSON;
using SwrveUnity.Helpers;
using SwrveUnity.REST;
using SwrveUnity.Messaging;

namespace SwrveUnity
{
public class SwrveDeeplinkManager
{
    const string SWRVE_AD_CAMPAIGN_URL   = "/api/1/ad_journey_campaign";
    const string SWRVE_AD_CAMPAIGN_ID    = "in_app_campaign_id";
    const string SWRVE_AD_INSTALL        = "install";
    const string SWRVE_AD_REENGAGE       = "reengage";
    const string SWRVE_AD_CAMPAIGN       = "ad_campaign";
    const string SWRVE_AD_SOURCE         = "ad_source";
    const string SWRVE_AD_CONTENT        = "ad_content";
    const int EXTERNAL_CAMPAIGN_RESPONSE_VERSION  = 2;

    private SwrveSDK sdk;
    private MonoBehaviour container;

    /** constructor for initialising the SDK */
    public SwrveDeeplinkManager(MonoBehaviour container, SwrveSDK sdkinstance)
    {
        this.sdk = sdkinstance;
        this.container = container;
    }

    public void HandleDeeplink(string url)
    {
        HandleDeeplink(url, null);
    }

    public void HandleDeferredDeeplink(string url)
    {
        HandleDeeplink(url, SWRVE_AD_INSTALL);
    }

    public void HandleNotificationToCampaign(string campaignID)
    {
        if(String.IsNullOrEmpty(campaignID)) {
            SwrveLog.Log("campaignID was nil or an empty string. Campaign will not be displayed");
            return;
        }
        string getRequest = CreateCampaignUrl(campaignID);
        container.StartCoroutine(GetExternalCampaign_Coroutine(getRequest));
    }

    public static bool IsSwrveDeeplink(string url)
    {
        if(!String.IsNullOrEmpty(url)) {
            try {
                Dictionary<string,string> queryParams = SwrveHelper.GetUriQueryParameters((new Uri(url)).Query);
                if(queryParams.ContainsKey(SWRVE_AD_CONTENT)) {
                    return true;
                }
            } catch (Exception exception) {
                SwrveLog.LogError("SwrveDeeplink URI parsing failed with the following exception: " + exception);
            }
        }
        return false;
    }

    protected void HandleDeeplink(string url, string actionType)
    {
        if(actionType == null) {
            actionType = SWRVE_AD_REENGAGE;
        }

        if(!IsSwrveDeeplink(url)) {
            return;
        }

        Dictionary<string, string> queryParams = SwrveHelper.GetUriQueryParameters((new Uri(url)).Query);
        if(queryParams.Count > 0) {
            string adSource = queryParams [SWRVE_AD_SOURCE];
            string campaignName = queryParams [SWRVE_AD_CAMPAIGN];
            string campaignID = queryParams [SWRVE_AD_CONTENT];

            string getRequest = CreateCampaignUrl(campaignID);
            container.StartCoroutine(GetExternalCampaign_Coroutine(getRequest));
            QueueDeeplinkGenericEvent(adSource, campaignID, campaignName, actionType);
        }
    }

    protected IEnumerator GetExternalCampaign_Coroutine (string getRequest)
    {
        yield return container.StartCoroutine(this.sdk.restClient.Get(getRequest, delegate(RESTResponse response) {
            if (response.Error == WwwDeducedError.NoError) {
                if (!string.IsNullOrEmpty(response.Body)) {
                    Dictionary<string, object> root = (Dictionary<string, object>)Json.Deserialize (response.Body);

                    if (root != null) {
                        if (root.ContainsKey("campaign") && root.ContainsKey("additional_info")) {
                            Dictionary<string, object> campaignData = (Dictionary<string, object>)root["campaign"];
                            Dictionary<string, object> additionalInfo = (Dictionary<string, object>)root["additional_info"];

                            this.sdk.UpdateCdnPaths(additionalInfo);
                            int version = MiniJsonHelper.GetInt (additionalInfo, "version");

                            if (this.sdk.config.MessagingEnabled && version == EXTERNAL_CAMPAIGN_RESPONSE_VERSION) {
                                this.sdk.SaveExternalCampaignCache(response.Body);
                                processCampaignJSON(campaignData);
                            }
                        }
                    }
                }
            } else {
                SwrveLog.LogError("Ad Journey campaign request error: " + response.Error.ToString () + ":" + response.Body);
            }
        }));
    }

    protected virtual void processCampaignJSON (Dictionary<string, object> campaignData)
    {
        HashSet<SwrveAssetsQueueItem> assetsQueue = new HashSet<SwrveAssetsQueueItem>();
        ISwrveAssetsManager assetsManager = this.sdk.GetSwrveAssetsManager();

        try {
            // Stop if we got an empty json
            if (campaignData != null) {
                SwrveBaseCampaign campaign = SwrveBaseCampaign.LoadFromJSONWithNoValidation (assetsManager,
                                             campaignData,
                                             this.sdk.GetInitialisedTime(),
                                             this.sdk.GetQAUser(),
                                             this.sdk.config.DefaultBackgroundColor);
                if(campaign == null) {
                    throw new Exception("Campaign was not in a format that could be parsed");
                }

                if (campaign.GetType() == typeof(SwrveConversationCampaign)) {
                    SwrveConversationCampaign conversationCampaign = (SwrveConversationCampaign) campaign;
                    assetsQueue.UnionWith(conversationCampaign.Conversation.ConversationAssets);
                } else if (campaign.GetType() == typeof(SwrveMessagesCampaign)) {
                    SwrveMessagesCampaign messageCampaign = (SwrveMessagesCampaign) campaign;
                    assetsQueue.UnionWith(messageCampaign.GetImageAssets());
                }
                container.StartCoroutine (assetsManager.DownloadAssets(assetsQueue, AddCampaignToQueue, campaign));
            }
        } catch (Exception exp) {
            SwrveLog.LogError ("Could not process ad journey campaign: " + exp.ToString ());
        }
    }

    protected void AddCampaignToQueue(object campaignObject)
    {
        SwrveBaseCampaign campaign = (SwrveBaseCampaign)campaignObject;
        this.sdk.ShowCampaign(campaign, true);
    }

    private string CreateCampaignUrl (string campaignId)
    {
        string baseUrl = this.sdk.config.ContentServer + SWRVE_AD_CAMPAIGN_URL;
        string queriedCampaignUrl = this.sdk.GetCampaignsAndResourcesUrl(baseUrl);

        StringBuilder campaignUrl = new StringBuilder (queriedCampaignUrl);
        campaignUrl.AppendFormat ("&{0}={1}", SWRVE_AD_CAMPAIGN_ID, campaignId);

        return campaignUrl.ToString();
    }

    private void QueueDeeplinkGenericEvent(string adSource, string campaignID, string campaignName, string actionType)
    {
        if(String.IsNullOrEmpty(adSource)) {
            SwrveLog.Log("DeeplinkCampaign adSource was nil or an empty string. Generic event not queued");
            return;
        }
        adSource =  "external_source_" + adSource;

        Dictionary<string, object> eventData = new Dictionary<string, object>();
        eventData.Add("campaignType", adSource);
        eventData.Add("actionType", actionType);
        eventData.Add("campaignId", campaignID);
        eventData.Add("contextId", campaignName);
        eventData.Add("id", -1);
        this.sdk.QueueGenericCampaignEvent(eventData);
    }
}
}

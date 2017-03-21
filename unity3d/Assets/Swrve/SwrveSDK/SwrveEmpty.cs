using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using SwrveUnity;
using SwrveUnityMiniJSON;
using SwrveUnity.Messaging;
using SwrveUnity.Helpers;
using SwrveUnity.ResourceManager;
using SwrveUnity.Device;
using SwrveUnity.IAP;

/// <summary>
/// Dummy class implementation of the Swrve SDK. Unsupported Versions go into here to prevent traffic.
/// </summary>
/// <remarks>
/// </remarks>
public class SwrveEmpty : SwrveSDK
{
    public override void Init (MonoBehaviour container, int appId, string apiKey, string userId)
    {
        SwrveConfig config = new SwrveConfig();
        config.UserId = userId;
        Init (container, 0, "", config);
    }

    public override void Init (MonoBehaviour container, int appId, string apiKey, string userId, SwrveConfig config)
    {
        config.UserId = userId;
        Init (container, 0, "", config);
    }

    public override void Init (MonoBehaviour container, int appId, string apiKey, SwrveConfig config)
    {
        this.Container = container;
        this.ResourceManager = new SwrveUnity.ResourceManager.SwrveResourceManager ();
        this.prefabName = container.name;
        this.appId = appId;
        this.apiKey = apiKey;
        this.config = config;
        this.userId = config.UserId;
        this.Language = config.Language;
        this.Initialised = true;
    }

    public override bool SendQueuedEvents ()
    {
        return true;
    }

    public override void GetUserResources (Action<Dictionary<string, Dictionary<string, string>>, string> onResult, Action<Exception> onError)
    {
        //do nothing
    }

    public override void GetUserResourcesDiff (Action<Dictionary<string, Dictionary<string, string>>, Dictionary<string, Dictionary<string, string>>, string> onResult, Action<Exception> onError)
    {
        //do nothing
    }

    public override void FlushToDisk (bool saveEventsBeingSent = false)
    {
        //do nothing
    }

    public override bool IsMessageDispaying ()
    {
        return false;
    }

    public override bool IsMessageDisplaying ()
    {
        return false;
    }

    public override SwrveMessage GetMessageForEvent (string eventName, IDictionary<string, string> payload)
    {
        return null;
    }

    public override SwrveConversation GetConversationForEvent (string eventName, IDictionary<string, string> payload=null)
    {
        return null;
    }

    public override IEnumerator ShowMessageForEvent (string eventName, SwrveMessage message, ISwrveInstallButtonListener installButtonListener = null, ISwrveCustomButtonListener customButtonListener = null, ISwrveMessageListener messageListener = null)
    {
        yield return null;
    }

    public override IEnumerator ShowConversationForEvent (string eventName, SwrveConversation conversation)
    {
        yield return null;
    }

    public override void DismissMessage ()
    {
        // do nothing
    }

    public override void RefreshUserResourcesAndCampaigns ()
    {
        // do nothing
    }

    public override void SessionStart()
    {
        // do nothing
    }

    public override void NamedEvent (string name, Dictionary<string, string> payload = null)
    {
        // do nothing
    }

    public override void UserUpdate (Dictionary<string, string> attributes)
    {
        // do nothing
    }

    public override void UserUpdate (string name, DateTime date)
    {
        // do nothing
    }

    public override void Purchase (string item, string currency, int cost, int quantity)
    {
    }

    public override void Iap (int quantity, string productId, double productPrice, string currency)
    {
    }

    public override void Iap (int quantity, string productId, double productPrice, string currency, IapRewards rewards)
    {
    }

    public override void CurrencyGiven (string givenCurrency, double amount)
    {
    }

    public override void LoadFromDisk ()
    {
    }

    public override Dictionary<string, string> GetDeviceInfo ()
    {
        return new Dictionary<string, string> ();
    }

    public override void OnSwrvePause ()
    {
    }

    public override void OnSwrveResume ()
    {
    }

    public override void OnSwrveDestroy ()
    {
    }

    public override List<SwrveBaseCampaign> GetCampaigns ()
    {
        return new List<SwrveBaseCampaign> ();
    }

    public override void ButtonWasPressedByUser (SwrveButton button)
    {
    }

    public override void MessageWasShownToUser (SwrveMessageFormat messageFormat)
    {
    }

    public override void ShowMessageCenterCampaign(SwrveBaseCampaign campaign)
    {
    }

    public override void ShowMessageCenterCampaign(SwrveBaseCampaign campaign, SwrveOrientation orientation)
    {
    }

    public override List<SwrveBaseCampaign> GetMessageCenterCampaigns()
    {
        return new List<SwrveBaseCampaign> ();
    }

    public override List<SwrveBaseCampaign> GetMessageCenterCampaigns(SwrveOrientation orientation)
    {
        return new List<SwrveBaseCampaign> ();
    }

    public override void RemoveMessageCenterCampaign(SwrveBaseCampaign campaign)
    {
    }

    public override SwrveMessage GetMessageForId (int messageId)
    {
        return null;
    }
}

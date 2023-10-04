#pragma warning disable 0436
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
    public override void Init(MonoBehaviour container, int appId, string apiKey, SwrveConfig config = null)
    {
        this.Container = container;
        this.ResourceManager = new SwrveUnity.ResourceManager.SwrveResourceManager();
        this.prefabName = container.name;
        this.appId = appId;
        this.apiKey = apiKey;
        this.config = config;
        this.Language = config.Language;
        this.Initialised = true;
    }

    public override bool SendQueuedEvents()
    {
        return true;
    }

    public override void GetUserResources(Action<Dictionary<string, Dictionary<string, string>>, string> onResult, Action<Exception> onError)
    {
        //do nothing
    }

    public override void GetUserResourcesDiff(Action<Dictionary<string, Dictionary<string, string>>, Dictionary<string, Dictionary<string, string>>, string> onResult, Action<Exception> onError)
    {
        //do nothing
    }

    public override void GetRealtimeUserProperties(Action<Dictionary<string, string>, string> onResult, Action<Exception> onError)
    {
        //do nothing
    }

    public override void FlushToDisk(bool saveEventsBeingSent = false)
    {
        //do nothing
    }

    public override bool IsMessageDisplaying()
    {
        return false;
    }

    protected override IEnumerator ShowMessageForEvent(string eventName, SwrveBaseMessage message, ISwrveCustomButtonListener customButtonListener = null, ISwrveMessageListener messageListener = null, ISwrveClipboardButtonListener clipboardButtonListener = null, ISwrveEmbeddedMessageListener embeddedMessageListener = null, ISwrveEmbeddedListener embeddedListener = null)
    {
        yield return null;
    }

    protected override IEnumerator ShowMessageForEvent(string eventName, IDictionary<string, string> payload, SwrveBaseMessage message, ISwrveCustomButtonListener customButtonListener = null, ISwrveMessageListener messageListener = null, ISwrveClipboardButtonListener clipboardButtonListener = null, ISwrveEmbeddedMessageListener embeddedMessageListener = null, ISwrveEmbeddedListener embeddedListener = null)
    {
        yield return null;
    }

    protected override IEnumerator ShowConversationForEvent(string eventName, SwrveConversation conversation)
    {
        yield return null;
    }

    public override void DismissMessage()
    {
        // do nothing
    }

    public override void RefreshUserResourcesAndCampaigns()
    {
        // do nothing
    }

    public override void SessionStart()
    {
        // do nothing
    }

    public override void NamedEvent(string name, Dictionary<string, string> payload = null)
    {
        // do nothing
    }

    public override void UserUpdate(Dictionary<string, string> attributes)
    {
        // do nothing
    }

    public override void UserUpdate(string name, DateTime date)
    {
        // do nothing
    }

    public override void Purchase(string item, string currency, int cost, int quantity)
    {
    }

    public override void Iap(int quantity, string productId, double productPrice, string currency, IapRewards rewards = null)
    {
    }

    public override void CurrencyGiven(string givenCurrency, double amount)
    {
    }

    public override void HandleDeeplink(string url)
    {
    }

    public override void HandleDeferredDeeplink(string url)
    {
    }

    public override void LoadFromDisk()
    {
    }

    protected override Dictionary<string, string> GetDeviceInfo()
    {
        return new Dictionary<string, string>();
    }

    public override void OnSwrvePause()
    {
    }

    public override void OnSwrveResume()
    {
    }

    public override void OnSwrveDestroy()
    {
    }

    protected override void MessageWasShownToUser(SwrveMessageFormat messageFormat)
    {
    }

    public override void EmbeddedControlMessageImpressionEvent(SwrveEmbeddedMessage message)
    {
    }

    public override void EmbeddedMessageWasShownToUser(SwrveEmbeddedMessage message)
    {
    }

    public override void EmbeddedMessageButtonWasPressed(SwrveEmbeddedMessage message, string buttonName)
    {
    }

    public override string GetPersonalizedEmbeddedMessageData(SwrveEmbeddedMessage message, Dictionary<string, string> personalizationProperties)
    {
        return null;
    }
    public override string GetPersonalizedText(string text, Dictionary<string, string> personalizationProperties)
    {
        return null;
    }

    public override void ShowMessageCenterCampaign(SwrveBaseCampaign campaign, SwrveOrientation? orientation = null, Dictionary<string, string> properties = null)
    {
    }

    public override List<SwrveBaseCampaign> GetMessageCenterCampaigns(SwrveOrientation? orientation = null, Dictionary<string, string> properties = null)
    {
        return new List<SwrveBaseCampaign>();
    }

    public override void RemoveMessageCenterCampaign(SwrveBaseCampaign campaign)
    {
    }

    public override void MarkMessageCenterCampaignAsSeen(SwrveBaseCampaign campaign)
    {
    }

    public override void Identify(string userId, OnSuccessIdentify onSuccess, OnErrorIdentify onError)
    {
        // do nothing
    }

    public override void Start(String userId = null)
    {
    }

    public override bool IsStarted()
    {
        return false;
    }
}

#pragma warning restore 0436

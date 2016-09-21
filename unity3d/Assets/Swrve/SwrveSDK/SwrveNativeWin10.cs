#if (UNITY_WSA_10_0 && SWRVE_WINDOWS_SDK)

using SwrveUnityWindows;
using Swrve.Conversation;
using System.Collections.Generic;
using System;
using SwrveUnity.IAP;
using SwrveUnity.Messaging;

public partial class SwrveSDK
{   
    private SwrveCommon _sdk;
    
    private void initNative()
    {
        _sdk = new SwrveCommon(proxyEvent);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        if(config.PushNotificationEnabled)
        {
            RegisterForPush();
        }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

    }

    private async void RegisterForPush()
    {
        string uri = await SwrveUnityBridge.RegisterForPush(_sdk);
        if(!string.IsNullOrEmpty(uri))
        {
            NativeCommunicationHelper.CallOnUnity(() =>
            {
                UserUpdate(new Dictionary<string, string> { { "swrve.wns_uri", uri } });
                SendQueuedEvents();
            });
        }
    }

    private string getNativeLanguage () {
        return SwrveUnityBridge.GetAppLanguage (null);
    }

    private void setNativeAppVersion () {
        config.AppVersion = SwrveUnityBridge.GetAppVersion ();
    }

    private void setNativeConversationVersion () {
        SetConversationVersion (SwrveUnityBridge.GetConversationVersion ());
    }

    private void showNativeConversation (string conversation) {
        NativeCommunicationHelper.CallOnWindows (() => SwrveUnityBridge.ShowConversation (_sdk, conversation));
    }

    private void proxyEvent(string name, Dictionary<string, string> payload)
    {
        NativeCommunicationHelper.CallOnUnity(() => NamedEventInternal(name, payload, false));
    }

    private void setNativeInfo(Dictionary<string, string> deviceInfo) { }
    private void startNativeLocation () {}
    private void startNativeLocationAfterPermission () {}
    private bool NativeIsBackPressed () { return false; }

    /// <summary>
    /// Buffer the event of a purchase using real currency, where a single item
    /// (that isn't an in-app currency) was purchased.
    /// The receipt provided will be validated against the Windows Store.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "iap" event.
    /// Note that this method is currently only supported for the Windows Store,
    /// and a valid receipt needs to be provided for verification.
    /// </remarks>
    /// <param name="receipt">
    /// The receipt sent back from the Windows Store upon successful purchase - this receipt will be verified by Swrve
    /// </param>
    public void IapWindows(IapReceipt receipt)
    {
        IapRewards no_rewards = new IapRewards();
        IapWindows(receipt, no_rewards);
    }

    /// <summary>
    /// Buffer the event of a purchase using real currency, where a single item
    /// (that isn't an in-app currency) was purchased.
    /// The receipt provided will be validated against the Windows Store.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "iap" event.
    /// Note that this method is currently only supported for the Windows Store,
    /// and a valid receipt needs to be provided for verification.
    /// </remarks>
    /// <param name="receipt">
    /// The receipt sent back from the Windows Store upon successful purchase - this receipt will be verified by Swrve
    /// </param>
    /// <param name="rewards">
    /// SwrveIAPRewards object containing any in-app currency and/or additional items
    /// included in this purchase that need to be recorded.
    /// This parameter is optional.
    /// </param>
    public void IapWindows(IapReceipt receipt, IapRewards rewards)
    {
        if (config.AppStore != SwrveAppStore.Windows)
        {
            throw new Exception ("This function can only be called to validate IAP events from Windows Store");
        }
        else
        {
            string encodedReceipt = (receipt != null) ? receipt.GetBase64EncodedReceipt() : null;
            if (receipt != null && string.IsNullOrEmpty(encodedReceipt))
            {
                SwrveLog.LogError("IAP event not sent: receipt cannot be empty for Windows Store verification");
                return;
            }
            // Windows Store IAP is always of quantity 1
            Dictionary<string, object> json = new Dictionary<string, object>();
            json.Add("app_store", config.AppStore);
            if (!string.IsNullOrEmpty(GetAppVersion()))
            {
                json.Add("app_version", GetAppVersion());
            }
            json.Add("receipt", encodedReceipt);
            AppendEventToBuffer("iap", json);

            if (config.AutoDownloadCampaignsAndResources)
            {
                // Send events automatically and check for changes
                CheckForCampaignsAndResourcesUpdates(false);
            }
        }
    }

    class SwrveCommon : Swrve.ISwrveCommon
    {
        private Action<string, Dictionary<string, string>> _proxyEvent;

        public SwrveCommon(Action<string, Dictionary<string, string>> proxyEvent)
        {
            _proxyEvent = proxyEvent;
        }

        public void EventInternal(string eventName, Dictionary<string, string> payload)
        {
            _proxyEvent.Invoke(eventName, payload);
        }

        public void ConversationWasShownToUser(ISwrveConversationCampaign campaign)
        {
            SwrveLog.Log("" + campaign);
        }

        public void PushNotificationWasEngaged(string pushId, Dictionary<string, string> payload)
        {
            SwrveLog.Log("" + pushId + ", " + payload);
        }

        public void TriggerConversationClosed(ISwrveConversationCampaign conversationCampaign)
        {
            SwrveLog.Log("" + conversationCampaign);
        }

        public void TriggerConversationOpened(ISwrveConversationCampaign conversationCampaign)
        {
            SwrveLog.Log("" + conversationCampaign);
        }
    }

    static class NativeCommunicationHelper
    {
        public static void CallOnWindows(Action lambda, bool waitUntilDone = true)
        {
            UnityEngine.WSA.Application.InvokeOnUIThread(() => lambda(), waitUntilDone);
        }

        public static void CallOnUnity(Action lambda, bool waitUntilDone = false)
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() => lambda(), waitUntilDone);
        }
    }
}

#endif

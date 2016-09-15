#if (UNITY_WSA_10_0 && SWRVE_WINDOWS_SDK)

using SwrveUnityWindows;
using System.Collections.Generic;
using System;
using SwrveUnity.IAP;
using SwrveUnity.Messaging;

public partial class SwrveSDK
{
    private SwrveCommon _sdk;

    private void setNativeInfo (Dictionary<string, string> deviceInfo)
    {
        _sdk = new SwrveCommon();
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

    private void callOnUnity(Action lambda, bool waitUntilDone=false) {
    #if UNITY_WSA
        UnityEngine.WSA.Application.InvokeOnAppThread(lambda, waitUntilDone);
    #endif
    }

    private void callOnWindows(Action lambda, bool waitUntilDone=true) {
    #if UNITY_WSA
        UnityEngine.WSA.Application.InvokeOnUIThread(lambda, waitUntilDone);
    #endif
    }

    private void showNativeConversation (string conversation) {
        callOnWindows(() =>
            {
                SwrveUnityBridge.ShowConversation(_sdk, conversation);
            },
            true
        );
    }

    private void initNative () {}
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
        public void EventInternal(string eventName, Dictionary<string, string> payload)
        {
            SwrveLog.Log("" + eventName);
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
            SwrveLog.i("" + conversationCampaign);
        }
    }
}

#endif

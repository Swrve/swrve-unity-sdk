#if UNITY_IPHONE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SwrveUnity.IAP;
using SwrveUnity.Helpers;
using SwrveUnityMiniJSON;
using SwrveUnity.SwrveUsers;

#if UNITY_5 || UNITY_2017_1_OR_NEWER
using UnityEngine.iOS;
#endif

public partial class SwrveSDK
{

private const string PushNotificationStatusKey = "Swrve.permission.ios.push_notifications";
private const string SilentPushNotificationStatusKey = "Swrve.permission.ios.push_bg_refresh";

private string pushNotificationStatus;
private string silentPushNotificationStatus;
/// <summary>
/// Buffer the event of a purchase using real currency, where a single item
/// (that isn't an in-app currency) was purchased.
/// The receipt provided will be validated against the iTunes Store.
/// </summary>
/// <remarks>
/// See the REST API documentation for the "iap" event.
/// Note that this method is currently only supported for the Apple App Store,
/// and a valid receipt needs to be provided for verification.
/// </remarks>
/// <param name="quantity">
/// Quantity purchased.
/// </param>
/// <param name="productId">
/// Unique product identifier for the item bought. This should match the Swrve resource name.
/// </param>
/// <param name="productPrice">
/// Price of the product purchased in real money. Note that this is the price
/// per product, not the total price of the transaction (when quantity > 1).
/// </param>
/// <param name="currency">
/// Real world currency used for this transaction. This must be an ISO currency code.
/// </param>
/// <param name="receipt">
/// The receipt sent back from the iTunes Store upon successful purchase - this receipt will be verified by Swrve.
/// Use either Base64EncodedReceipt or RawReceipt depending on what is offered by your plugin.
/// </param>
public void IapApple (int quantity, string productId, double productPrice, string currency, IapReceipt receipt)
    {
        IapApple (quantity, productId, productPrice, currency, receipt, string.Empty);
    }

    /// <summary>
    /// Buffer the event of a purchase using real currency, where a single item
    /// (that isn't an in-app currency) was purchased.
    /// The receipt provided will be validated against the iTunes Store.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "iap" event.
    /// Note that this method is currently only supported for the Apple App Store,
    /// and a valid receipt needs to be provided for verification.
    /// </remarks>
    /// <param name="quantity">
    /// Quantity purchased.
    /// </param>
    /// <param name="productId">
    /// Unique product identifier for the item bought. This should match the Swrve resource name.
    /// </param>
    /// <param name="productPrice">
    /// Price of the product purchased in real money. Note that this is the price
    /// per product, not the total price of the transaction (when quantity > 1).
    /// </param>
    /// <param name="currency">
    /// Real world currency used for this transaction. This must be an ISO currency code.
    /// </param>
    /// <param name="receipt">
    /// The receipt sent back from the iTunes Store upon successful purchase - this receipt will be verified by Swrve.
    /// Use either Base64EncodedReceipt or RawReceipt depending on what is offered by your plugin.
    /// </param>
    /// <param name="transactionId">
    /// The transaction id identifying the purchase iOS7+ (see SKPaymentTransaction::transactionIdentifier).
    /// </param>
    public void IapApple (int quantity, string productId, double productPrice, string currency, IapReceipt receipt, string transactionId)
    {
        IapRewards no_rewards = new IapRewards();
        IapApple (quantity, productId, productPrice, currency, no_rewards, receipt, transactionId);
    }

    /// <summary>
    /// Buffer the event of a purchase using real currency, where any in-app
    /// currencies were purchased, or where multiple items were purchased as part of a bundle.
    /// The receipt provided will be validated against the iTunes Store.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "iap" event.
    /// Note that this method is currently only supported for the Apple App Store,
    /// and a valid receipt needs to be provided for verification.
    /// </remarks>
    /// <param name="quantity">
    /// Quantity purchased.
    /// </param>
    /// <param name="productId">
    /// Unique product identifier for the item bought. This should match the Swrve resource name.
    /// </param>
    /// <param name="productPrice">
    /// Price of the product purchased in real money. Note that this is the price
    /// per product, not the total price of the transaction (when quantity > 1).
    /// </param>
    /// <param name="currency">
    /// Real world currency used for this transaction. This must be an ISO currency code.
    /// </param>
    /// <param name="rewards">
    /// SwrveIAPRewards object containing any in-app currency and/or additional items
    /// included in this purchase that need to be recorded.
    /// This parameter is optional.
    /// </param>
    /// <param name="receipt">
    /// The receipt sent back from the iTunes Store upon successful purchase - this receipt will be verified by Swrve.
    /// Use either Base64EncodedReceipt or RawReceipt depending on what is offered by your plugin.
    /// </param>
    public void IapApple (int quantity, string productId, double productPrice, string currency, IapRewards rewards, IapReceipt receipt)
    {
        IapApple (quantity, productId, productPrice, currency, rewards, receipt, string.Empty);
    }

    /// <summary>
    /// Buffer the event of a purchase using real currency, where any in-app
    /// currencies were purchased, or where multiple items were purchased as part of a bundle.
    /// The receipt provided will be validated against the iTunes Store.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "iap" event.
    /// Note that this method is currently only supported for the Apple App Store,
    /// and a valid receipt needs to be provided for verification.
    /// </remarks>
    /// <param name="quantity">
    /// Quantity purchased.
    /// </param>
    /// <param name="productId">
    /// Unique product identifier for the item bought. This should match the Swrve resource name.
    /// </param>
    /// <param name="productPrice">
    /// Price of the product purchased in real money. Note that this is the price
    /// per product, not the total price of the transaction (when quantity > 1).
    /// </param>
    /// <param name="currency">
    /// Real world currency used for this transaction. This must be an ISO currency code.
    /// </param>
    /// <param name="rewards">
    /// SwrveIAPRewards object containing any in-app currency and/or additional items
    /// included in this purchase that need to be recorded.
    /// This parameter is optional.
    /// </param>
    /// <param name="receipt">
    /// The receipt sent back from the iTunes Store upon successful purchase - this receipt will be verified by Swrve.
    /// Use either Base64EncodedReceipt or RawReceipt depending on what is offered by your plugin.
    /// </param>
    /// <param name="transactionId">
    /// The transaction id identifying the purchase iOS7+ (see SKPaymentTransaction::transactionIdentifier).
    /// </param>
    public void IapApple (int quantity, string productId, double productPrice, string currency, IapRewards rewards, IapReceipt receipt, string transactionId)
    {
        if (config.AppStore != "apple") {
            throw new Exception("This function can only be called to validate IAP events from Apple");
        } else {
            string encodedReceipt = null;
            if (receipt != null) {
                encodedReceipt = receipt.GetBase64EncodedReceipt();
            }
            if (String.IsNullOrEmpty(encodedReceipt)) {
                SwrveLog.LogError("IAP event not sent: receipt cannot be empty for Apple Store verification");
                return;
            }
            _Iap (quantity, productId, productPrice, currency, rewards, encodedReceipt, string.Empty, transactionId, config.AppStore);
        }
    }

    /// <summary>
    /// Obtains the device token if available.
    /// </summary>
    /// <returns>
    /// If the token was correctly obtained.
    /// </returns>
    public bool ObtainIOSDeviceToken()
    {
        if (config.PushNotificationEnabled) {
            byte[] token = NotificationServices.deviceToken;

            if (token != null) {
                // Send token as user update and to Babble if QA user
                string hexToken = SwrveHelper.FilterNonAlphanumeric(System.BitConverter.ToString(token));
                bool sendDeviceInfo = (iOSdeviceToken != hexToken);
                if (sendDeviceInfo) {
                    iOSdeviceToken = hexToken;
                    // Save device token for future launches
                    storage.Save (iOSdeviceTokenSave, iOSdeviceToken);
                    SendDeviceInfo();

                    if (qaUser != null) {
                        qaUser.UpdateDeviceInfo();
                    }
                }

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Processe remote notifications and clear them.
    /// </summary>
    public void ProcessRemoteNotifications()
    {
        if (config.PushNotificationEnabled) {
            // Process push notifications
            int notificationCount = NotificationServices.remoteNotificationCount;
            if(notificationCount > 0) {
                SwrveLog.Log("Got " + notificationCount + " remote notifications");

                for(int i = 0; i < notificationCount; i++) {
                    ProcessRemoteNotification(NotificationServices.remoteNotifications[i]);
                }
                NotificationServices.ClearRemoteNotifications();
            }
        }
    }

    /// <summary>
    /// Obtain from the native layer the status of the push permissions
    /// </summary>
    public void RefreshPushPermissions()
    {
        if(config.PushNotificationEnabled) {
#if !UNITY_EDITOR
            this.pushNotificationStatus = _swrvePushNotificationStatus (this.prefabName);
            this.silentPushNotificationStatus = _swrveBackgroundRefreshStatus ();
#endif
        }
    }

    public void SetPushNotificationsPermissionStatus(string pushStatus)
    {
        // Called asynchronously by the iOS native code with the UNUserNotification status
        if (!string.IsNullOrEmpty (pushStatus)) {
            bool sendEvents = (this.pushNotificationStatus != pushStatus);
            this.pushNotificationStatus = pushStatus;
            if (sendEvents) {
                QueueDeviceInfo();
            }
        }
    }

#if !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern string _swrveiOSLanguage();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSTimeZone();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSAppVersion();

    [DllImport ("__Internal")]
    private static extern void _swrveiOSRegisterForPushNotifications(string unJsonCategory, bool isProvisional);

    [DllImport ("__Internal")]
    private static extern string _swrveiOSLocaleCountry();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSIDFA();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSIDFV();

    [DllImport ("__Internal")]
    private static extern void _swrveiOSInitNative(string jsonConfig);

    [DllImport ("__Internal")]
    private static extern void _swrveiOSShowConversation(string conversation);

    [DllImport ("__Internal")]
    private static extern int _swrveiOSConversationVersion();

    [DllImport ("__Internal")]
    public static extern bool _swrveiOSIsSupportedOSVersion();

    [DllImport ("__Internal")]
    public static extern bool _swrveiOSIsConversationDisplaying();

    [DllImport ("__Internal")]
    public static extern string _swrveInfluencedDataJson();

    [DllImport ("__Internal")]
    public static extern string _swrvePushNotificationStatus(string componentName);

    [DllImport ("__Internal")]
    public static extern string _swrveBackgroundRefreshStatus();

    [DllImport ("__Internal")]
    private static extern void _swrveiOSUpdateQaUser(string jsonMap);

    [DllImport ("__Internal")]
    public static extern void _swrveUserId(string userId);

    [DllImport ("__Internal")]
    public static extern void _clearAllAuthenticatedNotifications();

#endif

    private string iOSdeviceToken;

    protected void RegisterForPushNotificationsIOS(bool isProvisional)
    {
#if !UNITY_EDITOR
        try {
            _swrveiOSRegisterForPushNotifications (Json.Serialize (config.NotificationCategories.Select(a => a.toDict ()).ToList ()), isProvisional);
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't invoke native code to register for push notifications, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());

#if UNITY_5 || UNITY_5_OR_NEWER || UNITY_2017_1_OR_NEWER
            NotificationServices.RegisterForNotifications(NotificationType.Alert | NotificationType.Badge | NotificationType.Sound);
#else
            NotificationServices.RegisterForRemoteNotificationTypes(RemoteNotificationType.Alert | RemoteNotificationType.Badge | RemoteNotificationType.Sound);
#endif
        }
#endif
    }

    protected string GetSavediOSDeviceToken()
    {
        string savedValue = storage.Load (iOSdeviceTokenSave);
        if (!string.IsNullOrEmpty(savedValue)) {
            return savedValue;
        }

        return null;
    }

    protected void ProcessRemoteNotification (RemoteNotification notification)
    {
        if (config.PushNotificationEnabled) {
            ProcessRemoteNotificationUserInfo(notification.userInfo);
            // Do not call listener for silent pushes
            if (notification.userInfo == null || !notification.userInfo.Contains(SilentPushTrackingKey)) {
                if (config.PushNotificationListener != null) {
                    config.PushNotificationListener.OnRemoteNotification(notification);
                }
            }
            if(qaUser != null) {
                qaUser.PushNotification(notification);
            }
        }
    }

    public static bool IsSupportediOSVersion()
    {
#if !UNITY_EDITOR
        try {
            return _swrveiOSIsSupportedOSVersion();
        } catch(Exception exp) {
            SwrveLog.LogWarning("Couldn't get init the native side correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
#endif
        return true;
    }

    protected void ProcessRemoteNotificationUserInfo(IDictionary userInfo)
    {
        // First check if it is processed natively or empty
        bool processedNatively = (userInfo != null && userInfo.Contains(PushUnityDoNotProcessKey));
        if (!processedNatively) {
            if (userInfo != null && userInfo.Contains(PushTrackingKey)) {
                // It is a Swrve push, we need to check if it was sent while the app was in the background
                bool whileInBackground = !userInfo.Contains("_swrveForeground");
                if (whileInBackground) {
                    object rawId = userInfo[PushTrackingKey];
                    string pushId = rawId.ToString();
                    // SWRVE-5613 Hack
                    if (rawId is Int64) {
                        pushId = ConvertInt64ToInt32Hack ((Int64)rawId).ToString ();
                    }

                    SendPushEngagedEvent(pushId);

                    // Evaluate and process any default push actions available
                    object deeplinkUrl = userInfo[PushDeeplinkKey];
                    if (deeplinkUrl != null) {
                        OpenURL(deeplinkUrl.ToString());
                    }

                    ProcessNotificationForCampaign(userInfo);

                } else {
                    SwrveLog.Log("Swrve remote notification received while in the foreground");
                }
            } else {
                if (userInfo != null && userInfo.Contains(SilentPushTrackingKey)) {
                    SwrveLog.Log("Swrve silent push received");
                } else {
                    SwrveLog.Log("Got unidentified notification");
                }
            }
        } else {
            // On the native layer, we modify the userInfo if there is an action required on the unity layer.
            if(userInfo != null && userInfo.Contains(PushButtonToCampaignIdKey)) {
                object campaignId = userInfo[PushButtonToCampaignIdKey];
                if(campaignId != null) {
                    HandleCampaignFromNotification(campaignId.ToString());
                }
            }
        }
    }


    private void ProcessNotificationForCampaign(IDictionary userInfo)
    {
        if(userInfo.Contains(PushContentKey)) {
            IDictionary content = userInfo[PushContentKey] as IDictionary;
            if(content != null && HasCorrectVersion(content)) {
                IDictionary campaign = content["campaign"] as IDictionary;
                if(campaign != null) {
                    object campaignId = campaign["id"];
                    if(campaignId != null) {
                        HandleCampaignFromNotification(campaignId.ToString());
                    }
                }
            }
        }
    }

    private bool HasCorrectVersion (IDictionary content)
    {
        /** Check the push version number **/
        object version = content["version"];
        if (version != null) {
            int contentVersion = Int32.Parse(version.ToString());
            return (contentVersion >= PushContentVersion);
        }
        return false;
    }

    private void initNative()
    {
#if !UNITY_EDITOR
        try {
            _swrveiOSInitNative(GetNativeDetails ());
            RefreshPushPermissions();
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get init the native side correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
#endif
    }

    private void setNativeInfo(Dictionary<string, string> deviceInfo)
    {
        if (!string.IsNullOrEmpty(iOSdeviceToken)) {
            deviceInfo["swrve.ios_token"] = iOSdeviceToken;
        }

#if !UNITY_EDITOR
        try {
            deviceInfo ["swrve.timezone_name"] = _swrveiOSTimeZone();
        } catch (Exception e) {
            SwrveLog.LogWarning("Couldn't get device timezone on iOS, make sure you have the plugin inside your project and you are running on a device: " + e.ToString());
        }

        try {
            deviceInfo ["swrve.device_region"] = _swrveiOSLocaleCountry();
        } catch (Exception e) {
            SwrveLog.LogWarning("Couldn't get device region on iOS, make sure you have the plugin inside your project and you are running on a device: " + e.ToString());
        }

        if (config.LogAppleIDFV) {
            try {
                String idfv = _swrveiOSIDFV();
                if (!string.IsNullOrEmpty(idfv)) {
                    deviceInfo ["swrve.IDFV"] = idfv;
                }
            } catch (Exception e) {
                SwrveLog.LogWarning("Couldn't get device IDFV, make sure you have the plugin inside your project and you are running on a device: " + e.ToString());
            }
        }
        if (config.LogAppleIDFA) {
            try {
                String idfa = _swrveiOSIDFA();
                if (!string.IsNullOrEmpty(idfa)) {
                    deviceInfo ["swrve.IDFA"] = idfa;
                }
            } catch (Exception e) {
                SwrveLog.LogWarning("Couldn't get device IDFA, make sure you have the plugin inside your project and you are running on a device: " + e.ToString());
            }
        }

        if (!string.IsNullOrEmpty (pushNotificationStatus)) {
            deviceInfo[PushNotificationStatusKey] = pushNotificationStatus;
        }

        if (!string.IsNullOrEmpty (silentPushNotificationStatus)) {
            deviceInfo[SilentPushNotificationStatusKey] = silentPushNotificationStatus;
        }

        // Authenticated Push
        deviceInfo["swrve.can_receive_authenticated_push"] = Boolean.TrueString.ToLower();

        UpdateNativeUserId();
#endif
    }

    private string getNativeLanguage()
    {
#if !UNITY_EDITOR
        try {
            return _swrveiOSLanguage();
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get the device language, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
#endif
        return null;
    }

    private void setNativeAppVersion()
    {
#if !UNITY_EDITOR
        try {
            config.AppVersion = _swrveiOSAppVersion();
            SwrveLog.Log ("got iOS version name " + config.AppVersion);
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get the device app version, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
#endif
    }
    private void UpdateNativeUserId()
    {
#if !UNITY_EDITOR
        _swrveUserId(UserId);
#endif
    }
    private void ClearAllAuthenticatedNotifications()
    {
#if !UNITY_EDITOR
        _clearAllAuthenticatedNotifications();
#endif
    }
    private void setNativeConversationVersion()
    {
#if !UNITY_EDITOR
        try {
            SetConversationVersion (_swrveiOSConversationVersion ());
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't start Locations on iOS correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
#endif
    }

    private static bool IsConversationDisplaying()
    {
#if !UNITY_EDITOR
        try {
            return _swrveiOSIsConversationDisplaying();
        } catch(Exception exp) {
            SwrveLog.LogWarning("Couldn't init the native side correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
#endif
        return false; // Defaulting to false so messages can still appear in the editor
    }

    private void showNativeConversation(string conversation)
    {
#if !UNITY_EDITOR
        try {
            _swrveiOSShowConversation(conversation);
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't show conversation correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
#endif
    }

    private bool NativeIsBackPressed ()
    {
        return false;
    }

    public void updateQAUser(Dictionary<string, object> map)
    {
#if !UNITY_EDITOR
        try {
            _swrveiOSUpdateQaUser(Json.Serialize(map));
        } catch (Exception exp) {
            SwrveLog.LogWarning ("Couldn't update QA user from iOS: " + exp.ToString ());
        }
#endif
    }

}

// Added interface for our NativeiOS layer for our Helper Class.
namespace SwrveUnity.Helpers
{
public static partial class SwrveHelper
{
#if !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern string _swrveiOSUUID();
#endif

    public static string getNativeRandomUUID()
    {
        string uuid = null;
#if !UNITY_EDITOR
        try {
            uuid = _swrveiOSUUID();
        } catch (Exception exp) {
            SwrveLog.LogWarning ("Couldn't get random UUID: " + exp.ToString ());
        }
#endif
        return uuid;
    }
}

}

#endif //#endif for "#if UNITY_IPHONE" - Begining of this File.

#if UNITY_IOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SwrveUnity.IAP;
using SwrveUnity.Helpers;
using SwrveUnityMiniJSON;
using SwrveUnity.SwrveUsers;

public partial class SwrveSDK
{

    private const string PushNotificationStatusKey = "Swrve.permission.ios.push_notifications";
    private const string SilentPushNotificationStatusKey = "Swrve.permission.ios.push_bg_refresh";

    private string lastRespondedNotificationIdentifier;
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
    public void IapApple(int quantity, string productId, double productPrice, string currency, IapReceipt receipt)
    {
        IapApple(quantity, productId, productPrice, currency, receipt, string.Empty);
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
    public void IapApple(int quantity, string productId, double productPrice, string currency, IapReceipt receipt, string transactionId)
    {
        IapRewards no_rewards = new IapRewards();
        IapApple(quantity, productId, productPrice, currency, no_rewards, receipt, transactionId);
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
    public void IapApple(int quantity, string productId, double productPrice, string currency, IapRewards rewards, IapReceipt receipt)
    {
        IapApple(quantity, productId, productPrice, currency, rewards, receipt, string.Empty);
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
    public void IapApple(int quantity, string productId, double productPrice, string currency, IapRewards rewards, IapReceipt receipt, string transactionId)
    {
        if (config.AppStore != "apple")
        {
            throw new Exception("This function can only be called to validate IAP events from Apple");
        }
        else
        {
            string encodedReceipt = null;
            if (receipt != null)
            {
                encodedReceipt = receipt.GetBase64EncodedReceipt();
            }
            if (String.IsNullOrEmpty(encodedReceipt))
            {
                SwrveLog.LogError("IAP event not sent: receipt cannot be empty for Apple Store verification");
                return;
            }
            _Iap(quantity, productId, productPrice, currency, rewards, encodedReceipt, string.Empty, transactionId, config.AppStore);
        }
    }


#if UNITY_2019_4_OR_NEWER
    /// <summary>
    /// Process remote notifications and clear them.
    /// </summary>
    public void ProcessRemoteNotifications()
    {
        if (config.PushNotificationEnabled)
        {
            //Note GetLastRespondedNotification is only cleared when the app is closed so we need to keep track of it's Identifier
            //so as to not process the push multiple times.
            var notification = Unity.Notifications.iOS.iOSNotificationCenter.GetLastRespondedNotification();
            if (notification != null && !string.IsNullOrEmpty(notification.Identifier) && lastRespondedNotificationIdentifier != notification.Identifier)
            {
                SwrveLog.Log("Found Notification: " + notification.Identifier);
                this.lastRespondedNotificationIdentifier = notification.Identifier;
                ProcessRemoteNotification(notification);
                Unity.Notifications.iOS.iOSNotificationCenter.RemoveDeliveredNotification(notification.Identifier);
            }
        }
    }
#endif

    /// <summary>
    /// Obtain from the native layer the status of the push permissions
    /// </summary>
    public void RefreshPushPermissions()
    {
        if (config.PushNotificationEnabled)
        {
#if !UNITY_EDITOR
            this.pushNotificationStatus = _swrvePushNotificationStatus (this.prefabName);
            this.silentPushNotificationStatus = _swrveBackgroundRefreshStatus ();
#endif
        }
    }

    public void SetPushNotificationsPermissionStatus(string pushStatus)
    {
        // Called asynchronously by the iOS native code with the UNUserNotification status
        if (!string.IsNullOrEmpty(pushStatus))
        {
            bool sendEvents = (this.pushNotificationStatus != pushStatus);
            this.pushNotificationStatus = pushStatus;
            if (sendEvents)
            {
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
    private static extern string _swrveiOSLocaleCountry();

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
    public static extern string _swrveiOSGetOSDeviceType();

    [DllImport ("__Internal")]
    public static extern string _swrveiOSGetPlatformOS();

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
    public static extern void _swrveTrackingStateStopped(bool isTrackingStateStopped);

    [DllImport ("__Internal")]
    public static extern void _saveConfigForPushDelivery();

    [DllImport ("__Internal")]
    public static extern void _clearAllAuthenticatedNotifications();

    [DllImport ("__Internal")]
    public static extern void _swrveCopyToClipboard(string content);

#endif

    private string iOSdeviceToken;

    protected void RegisterForPushNotificationsIOS(bool isProvisional)
    {
#if !UNITY_EDITOR
#if (UNITY_2019_4_OR_NEWER)
        if (isProvisional){
            Container.StartCoroutine(RegisterForPushNotificationsUsingAuthorizationRequest(Unity.Notifications.iOS.AuthorizationOption.Provisional));
        } else {
            Container.StartCoroutine(RegisterForPushNotificationsUsingAuthorizationRequest(Unity.Notifications.iOS.AuthorizationOption.Alert | Unity.Notifications.iOS.AuthorizationOption.Badge | Unity.Notifications.iOS.AuthorizationOption.Sound));
        }
#endif
#endif // !UNITY_EDITOR
    }

    protected string GetSavediOSDeviceToken()
    {
        string savedValue = storage.Load(iOSdeviceTokenSave);
        if (!string.IsNullOrEmpty(savedValue))
        {
            return savedValue;
        }

        return null;
    }

    protected void SaveConfigForPushDelivery()
    {
#if !UNITY_EDITOR
        if(config.PushNotificationEnabled) {
            _saveConfigForPushDelivery();
        }
#endif
    }

#if (UNITY_2019_4_OR_NEWER)

    protected void ProcessRemoteNotification(Unity.Notifications.iOS.iOSNotification notification)
    {
        if (config.PushNotificationEnabled)
        {
            string actionIdentifier = Unity.Notifications.iOS.iOSNotificationCenter.GetLastRespondedNotificationAction();
            if (!string.IsNullOrEmpty(actionIdentifier))
            {
                ProcessRemoteNotificationUserInfo(notification.UserInfo, actionIdentifier);
            }
            else
            {
                SwrveLog.LogWarning("Push action identifier is null, not processing push");
            }

            // Do not call listener for silent pushes
            if (notification.UserInfo == null || !notification.UserInfo.ContainsKey(SilentPushTrackingKey))
            {
                if (config.PushNotificationListener != null)
                {
                    config.PushNotificationListener.OnRemoteNotification(notification);
                }
            }
        }
    }

    public IEnumerator RegisterForPushNotificationsUsingAuthorizationRequest(Unity.Notifications.iOS.AuthorizationOption authorizationOption)
    {
        // Register and obtain the token
        using (var req = new Unity.Notifications.iOS.AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsFinished)
            {
                yield return null;
            }

            if (string.IsNullOrEmpty(req.Error))
            {
                if (!string.IsNullOrEmpty(req.DeviceToken))
                {
                    SetDeviceToken(req.DeviceToken);
                }
            }
            else
            {
                SwrveLog.LogError("Could not register push through Mobile Notifications package: '" + req.Error + "'");
            }
        }
    }

#endif // (UNITY_2019_4_OR_NEWER)

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

    public static string GetOSDeviceType()
    {
#if !UNITY_EDITOR
        try {
            return _swrveiOSGetOSDeviceType();
        } catch(Exception exp) {
            SwrveLog.LogWarning("Couldn't get init the native side correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
#endif
        return "mobile"; // if there is no native response, we default to "mobile"
    }

    public static string GetiOSPlatformOS()
    {
#if !UNITY_EDITOR
        try {
            return _swrveiOSGetPlatformOS();
        } catch(Exception exp) {
            SwrveLog.LogWarning("Couldn't get init the native side correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
#endif
        return "ios"; // if there is no native response, we default to "ios"
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
        if (!string.IsNullOrEmpty(iOSdeviceToken))
        {
            deviceInfo["swrve.ios_token"] = iOSdeviceToken;
        }

#if !UNITY_EDITOR
        try {
            deviceInfo ["swrve.timezone_name"] = _swrveiOSTimeZone();
        } catch (Exception e) {
            SwrveLog.LogWarning("Couldn't get device timezone on iOS, make sure you have the plugin inside your project and you are running on a device: " + e.ToString());
        }

        try {
            string deviceRegion = _swrveiOSLocaleCountry();
            if (!string.IsNullOrEmpty(deviceRegion))
            {
                deviceInfo["swrve.device_region"] = deviceRegion;
            }
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

    private void UpdateTrackingStateStopped(bool isTrackingStateStopped)
    {
#if !UNITY_EDITOR
        _swrveTrackingStateStopped(isTrackingStateStopped);
#endif
    }

    public string GetInfluencedDataJson()
    {
#if !UNITY_EDITOR
        try {
            return _swrveInfluencedDataJson();
        } catch(Exception exp) {
            SwrveLog.LogWarning("Couldn't get InfluencedDataJson from the native side correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
#endif
        return null;
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
            SwrveLog.LogWarning("Couldn't start Conversations on iOS correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
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

    private bool NativeIsBackPressed()
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

    private void CopyToClipboard(string content)
    {
#if !UNITY_EDITOR
        _swrveCopyToClipboard(content);
#endif
    }

    private void SetDeviceToken(string token)
    {
        string hexToken = SwrveHelper.FilterNonAlphanumeric(token);
        bool sendDeviceInfo = (iOSdeviceToken != hexToken);
        if (sendDeviceInfo)
        {
            iOSdeviceToken = hexToken;
            // Save device token for future launches
            storage.Save(iOSdeviceTokenSave, iOSdeviceToken);

            if (IsSDKReady())
            {
                SendDeviceInfo();
            }
        }
    }

    private void OpenDeeplink(IDictionary userInfo)
    {
        if (userInfo.Contains(PushDeeplinkKey))
        {
            object deeplinkUrl = userInfo[PushDeeplinkKey];
            if (deeplinkUrl != null)
            {
                OpenURL(deeplinkUrl.ToString());
            }
        }
    }

    private string GetPushId(IDictionary userInfo)
    {
        object rawId = userInfo[PushTrackingKey];
        string pushId = rawId.ToString();
        // MOBILE-5613 Hack
        if (rawId is Int64)
        {
            pushId = ConvertInt64ToInt32Hack((Int64)rawId).ToString();
        }
        return pushId;
    }

    private void SendButtonClickEvent(Dictionary<string, object> selectedButton, string pushId, string actionIdentifier)
    {
        if (selectedButton == null)
        {
            SwrveLog.Log("Selected button is null");
            return;
        }
        Dictionary<string, object> eventData = new Dictionary<string, object>();
        eventData.Add("id", pushId);
        eventData.Add("campaignType", "push");
        eventData.Add("actionType", "button_click");
        eventData.Add("contextId", actionIdentifier);

        if (selectedButton.ContainsKey("title"))
        {
            string actionText = (string)selectedButton["title"];
            Dictionary<string, string> payloadData = new Dictionary<string, string>();
            payloadData.Add("buttonText", actionText);
            eventData.Add("payload", payloadData);
        }

        QueueGenericCampaignEvent(eventData);
    }

    private Dictionary<string, object> GetSelectedButtonDictionary(Dictionary<string, object> swrvePayload, string actionIdentifier)
    {
        if (swrvePayload != null && swrvePayload.ContainsKey("buttons"))
        {
            int position = Convert.ToInt16(actionIdentifier);
            List<object> buttons = (List<object>)swrvePayload["buttons"];
            Dictionary<string, object> selectedButton = (Dictionary<string, object>)buttons[position];
            return selectedButton;
        }
        return null;
    }

    private void ProcessButtonAction(Dictionary<string, object> selectedButton, string actionIdentifier)
    {
        if (selectedButton == null)
        {
            SwrveLog.Log("Selected button is null");
            return;
        }

        string actionType = (string)selectedButton["action_type"];
        if (actionType == "open_url")
        {
            string url = (string)selectedButton["action"];
            SwrveLog.Log("Opening url: " + url);
            OpenURL(url);
        }
        else if (actionType == "open_campaign")
        {
            string campaignId = (string)selectedButton["action"];
            HandleCampaignFromNotification(campaignId);
        }
    }

    private void ProcessNotificationForCampaign(IDictionary swrvePayload)
    {
        if (swrvePayload != null && swrvePayload.Contains("campaign"))
        {
            Dictionary<string, object> campaign = (Dictionary<string, object>)swrvePayload["campaign"];
            if (campaign != null)
            {
                object campaignId = campaign["id"];
                if (campaignId != null)
                {
                    HandleCampaignFromNotification(campaignId.ToString());
                }
            }
        }
    }

    protected void ProcessRemoteNotificationUserInfo(IDictionary userInfo, string actionIdentifier)
    {
        if (userInfo != null && userInfo.Contains(PushTrackingKey))
        {
            //Note Influence data is cleared on the native side in userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:

            string pushId = GetPushId(userInfo);
            Dictionary<string, object> swrvePayload = (Dictionary<string, object>)Json.Deserialize((string)userInfo[PushContentKey]);
            if (actionIdentifier == @"com.apple.UNNotificationDefaultActionIdentifier")
            {
                // direct click on push
                SendPushEngagedEvent(pushId);
                OpenDeeplink(userInfo);
                ProcessNotificationForCampaign(swrvePayload);
            }
            else
            {
                // button click on push, actionIdentifer will be button position clicked: 0, 1, 2 etc.
                Dictionary<string, object> selectedButton = GetSelectedButtonDictionary(swrvePayload, actionIdentifier);
                SendButtonClickEvent(selectedButton, pushId, actionIdentifier);
                SendPushEngagedEvent(pushId);
                ProcessButtonAction(selectedButton, actionIdentifier);
            }
        }
        else
        {
            // silent push should be processed through SilentPushListener
        }
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

#endif //#endif for "#if UNITY_IOS" - Begining of this File.

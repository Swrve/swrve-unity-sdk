#if UNITY_IPHONE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Swrve.IAP;
using Swrve.Helpers;
using SwrveMiniJSON;

#if UNITY_5
using UnityEngine.iOS;
#endif

public partial class SwrveSDK
{
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

    [DllImport ("__Internal")]
    private static extern string _swrveiOSGetLanguage();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSGetTimeZone();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSGetAppVersion();

    [DllImport ("__Internal")]
    private static extern void _swrveiOSRegisterForPushNotifications(string jsonCategory);

    [DllImport ("__Internal")]
    private static extern string _swrveiOSUUID();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSLocaleCountry();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSIDFA();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSIDFV();

    [DllImport ("__Internal")]
    private static extern void _swrveiOSStartPlot();

    [DllImport ("__Internal")]
    private static extern void _swrveiOSInitNative(string jsonConfig);

    [DllImport ("__Internal")]
    private static extern void _swrveiOSShowConversation(string conversation);

    [DllImport ("__Internal")]
    private static extern int _swrveiOSConversationVersion();

    private string iOSdeviceToken;

    protected void RegisterForPushNotificationsIOS()
    {
        try {
            _swrveiOSRegisterForPushNotifications (Json.Serialize (config.pushCategories.Select (a => a.toDict ()).ToList ()));
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't invoke native code to register for push notifications, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());

#if UNITY_5
            NotificationServices.RegisterForNotifications(NotificationType.Alert | NotificationType.Badge | NotificationType.Sound);
#else
            NotificationServices.RegisterForRemoteNotificationTypes(RemoteNotificationType.Alert | RemoteNotificationType.Badge | RemoteNotificationType.Sound);
#endif
        }
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
        if(config.PushNotificationEnabled) {
            ProcessRemoteNotificationUserInfo(notification.userInfo);
            if(PushNotificationListener != null) {
                PushNotificationListener.OnRemoteNotification(notification);
            }
            if(qaUser != null) {
                qaUser.PushNotification(notification);
            }
        }
    }

    protected void ProcessRemoteNotificationUserInfo(IDictionary userInfo) {
        if (userInfo != null && userInfo.Contains(PushTrackingKey)) {
            // It is a Swrve push, we need to check if it was sent while the app was in the background
            bool whileInBackground = !userInfo.Contains("_swrveForeground");
            if (whileInBackground) {
                object rawId = userInfo[PushTrackingKey];
                string pushId = rawId.ToString();
                // SWRVE-5613 Hack
                if (rawId is Int64) {
                    pushId = ConvertInt64ToInt32Hack((Int64)rawId).ToString();
                }
                SendPushNotificationEngagedEvent(pushId);
            } else {
                SwrveLog.Log("Swrve remote notification received while in the foreground");
            }
        } else {
            SwrveLog.Log("Got unidentified notification");
        }

        // Process push deeplink
        if (userInfo != null && userInfo.Contains (PushDeeplinkKey)) {
            object deeplinkUrl = userInfo[PushDeeplinkKey];
            if (deeplinkUrl != null) {
                OpenURL(deeplinkUrl.ToString());
            }
        }
    }

    private void initNative(string jsonString)
    {
        try {
            _swrveiOSInitNative(jsonString);
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get init the native side correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
    }

    private void setNativeInfo(Dictionary<string, string> deviceInfo)
    {
        if (!string.IsNullOrEmpty(iOSdeviceToken)) {
            deviceInfo["swrve.ios_token"] = iOSdeviceToken;
        }

        try {
            deviceInfo ["swrve.timezone_name"] = _swrveiOSGetTimeZone();
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
    }

    private string getNativeLanguage()
    {
        try {
            return _swrveiOSGetLanguage();
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get the device language, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
        return null;
    }

    private void setNativeAppVersion()
    {
        try {
            config.AppVersion = _swrveiOSGetAppVersion();
            SwrveLog.Log ("got iOS version name " + config.AppVersion);
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get the device app version, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
    }

    private string getNativeRandomUUID()
    {
        string uuid = null;
        try {
            uuid = _swrveiOSUUID();
        } catch (Exception exp) {
            SwrveLog.LogWarning ("Couldn't get random UUID: " + exp.ToString ());
        }
        return uuid;
    }

    private void setNativeConversationVersion()
    {
        try {
            conversationVersion = _swrveiOSConversationVersion();
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't start Locations on iOS correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
    }

    private void showNativeConversation(string conversation)
    {
        try {
            _swrveiOSShowConversation(conversation);
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get show conversation correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
    }

    private void startNativeLocation()
    {
        try {
            _swrveiOSStartPlot();
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't start Location on iOS correctly, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
    }

    private void startNativeLocationAfterPermission()
    {
        startNativeLocation ();
    }

}
#endif

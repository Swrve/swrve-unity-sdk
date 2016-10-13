#if UNITY_ANDROID
using System.Collections.Generic;
using System;
using UnityEngine;
using SwrveUnity.Helpers;
using SwrveUnityMiniJSON;
using SwrveUnity;

public partial class SwrveSDK
{
    private const string SwrveAndroidGCMPushPluginPackageName = "com.swrve.unity.gcm.SwrveGcmDeviceRegistration";
    private const string SwrveAndroidADMPushPluginPackageName = "com.swrve.unity.adm.SwrveAdmPushSupport";
    private const string SwrveAndroidUnityCommonName = "com.swrve.sdk.SwrveUnityCommon";
    private const string SwrveAndroidPlotName = "com.swrve.sdk.SwrvePlot";

    private const string IsInitialisedName = "isInitialised";
    private const string GetConversationVersionName = "getConversationVersion";
    private const string ShowConversationName = "showConversation";
    private const string SwrvePlotOnCreateName = "onCreate";

    private const string UnityPlayerName = "com.unity3d.player.UnityPlayer";
    private const string UnityCurrentActivityName = "currentActivity";

    private string registrationToken;
    private static AndroidJavaObject androidPlugin;
    private static bool androidPluginInitialized = false;
    private static bool androidPluginInitializedSuccessfully = false;
    private string googlePlayAdvertisingId;
    private static bool startedPlot;

    private const int GooglePlayPushPluginVersion = 4;
    private const int ADMPushPluginVersion = 1;

    private void setNativeInfo(Dictionary<string, string> deviceInfo)
    {
        if (!string.IsNullOrEmpty(registrationToken)) {
            if (config.AndroidPushProvider == AndroidPushProvider.GOOGLE_GCM) {
  	            deviceInfo["swrve.gcm_token"] = registrationToken;
            } else if (config.AndroidPushProvider == AndroidPushProvider.AMAZON_ADM) {
                deviceInfo["swrve.adm_token"] = registrationToken;
            }
        }

        string timezone = AndroidGetTimezone();
        if (!string.IsNullOrEmpty(timezone)) {
            deviceInfo ["swrve.timezone_name"] = timezone;
        }

        string deviceRegion = AndroidGetRegion();
        if (!string.IsNullOrEmpty(deviceRegion)) {
            deviceInfo ["swrve.device_region"] = deviceRegion;
        }

        if (config.LogAndroidId) {
            try {
                deviceInfo ["swrve.android_id"] = AndroidGetAndroidId();
            } catch (Exception e) {
                SwrveLog.LogWarning("Couldn't get device IDFA, make sure you have the plugin inside your project and you are running on a device: " + e.ToString());
            }
        }

        if (config.LogGoogleAdvertisingId) {
            if (!string.IsNullOrEmpty(googlePlayAdvertisingId)) {
                deviceInfo ["swrve.GAID"] = googlePlayAdvertisingId;
            }
        }
    }

    private string getNativeLanguage()
    {
        string language = null;
        try {
            using (AndroidJavaClass localeJavaClass = new AndroidJavaClass("java.util.Locale")) {
                AndroidJavaObject defaultLocale = localeJavaClass.CallStatic<AndroidJavaObject>("getDefault");
                language = defaultLocale.Call<string>("getLanguage");
                string country = defaultLocale.Call<string>("getCountry");
                if (!string.IsNullOrEmpty (country)) {
                    language += "-" + country;
                }
                string variant = defaultLocale.Call<string>("getVariant");
                if (!string.IsNullOrEmpty (variant)) {
                    language += "-" + variant;
                }
            }
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get the device language, make sure you are running on an Android device: " + exp.ToString());
        }
        return language;
    }

    private void InitialiseAndroidPushPlugin() {
        //Only execute this once
        if (androidPluginInitialized) {
            return;
        }
        androidPluginInitialized = true;

        string pluginPackageName = "";
        int pluginVersion = -1;
        if (config.AndroidPushProvider == AndroidPushProvider.GOOGLE_GCM) {
            pluginPackageName = SwrveAndroidGCMPushPluginPackageName;
            pluginVersion = GooglePlayPushPluginVersion;
        } else if (config.AndroidPushProvider == AndroidPushProvider.AMAZON_ADM) {
            pluginPackageName = SwrveAndroidADMPushPluginPackageName;
            pluginVersion = ADMPushPluginVersion;
		} else if (config.AndroidPushProvider == AndroidPushProvider.NONE) {
            return;
        }

        string jniPluginClassName = pluginPackageName.Replace(".", "/");
        if (AndroidJNI.FindClass(jniPluginClassName).ToInt32() == 0) {
            SwrveLog.LogError("Could not find class: " 
                + jniPluginClassName + 
                " Are you using the correct SwrveSDKPushSupport plugin given the swrve config.AndroidPushProvider setting?");

            //Force crash by calling another JNI call without clearing exceptions.
            //This is to enforce proper integration
            AndroidJNI.FindClass(jniPluginClassName); 
            return;
        }

        androidPlugin = new AndroidJavaClass(pluginPackageName);
        if (androidPlugin == null) {
            SwrveLog.LogError("Found class, but unable to construct AndroidJavaClass: " + jniPluginClassName);
            return;
        }

        // Check that the version is the same
        int testPluginVersion = androidPlugin.CallStatic<int>("getVersion");

        if (testPluginVersion != pluginVersion) {
            // Plugin with changes to the public API not supported
            androidPlugin = null;
            throw new Exception("The version of the Swrve Android Push plugin" + pluginPackageName + "is different. This Swrve SDK needs version " + pluginVersion);
        } else {
            androidPluginInitializedSuccessfully = true;
        }
    }

    private void InitialisePushGCM(MonoBehaviour container, string senderId) {
        try {
            this.registrationToken = storage.Load(GcmDeviceTokenSave);
            bool registered = false;
            if (androidPluginInitializedSuccessfully) {
                registered = androidPlugin.CallStatic<bool>(
                    "registerDevice", container.name, senderId, config.GCMPushNotificationTitle, config.GCMPushNotificationIconId, config.GCMPushNotificationMaterialIconId, config.GCMPushNotificationLargeIconId, config.GCMPushNotificationAccentColor);
            }
            if (!registered) {
                SwrveLog.LogError("Could not communicate with the Swrve Android Push plugin.");
            }
        } catch (Exception exp) {
            SwrveLog.LogError("Could not initalise push: " + exp.ToString());
        }
    }

    private void InitialisePushADM(MonoBehaviour container, string senderId) {
        try {
            this.registrationToken = storage.Load(AdmDeviceTokenSave);
            bool registered = false;
            if (androidPluginInitializedSuccessfully) {
                registered = androidPlugin.CallStatic<bool>(
                    "initialiseAdm", container.name, config.ADMPushNotificationTitle, config.ADMPushNotificationIconId, config.ADMPushNotificationMaterialIconId, config.ADMPushNotificationLargeIconId, config.ADMPushNotificationAccentColor);
            }
            if (!registered) {
                SwrveLog.LogError("Could not communicate with the Swrve Android Push plugin.");
            }
        } catch (Exception exp) {
            SwrveLog.LogError("Could not initalise push: " + exp.ToString());
        }
    }

    public void SetGooglePlayAdvertisingId(string advertisingId)
    {
        this.googlePlayAdvertisingId = advertisingId;
        storage.Save(GoogleAdvertisingIdSave, advertisingId);
    }

    private void RequestGooglePlayAdvertisingId(MonoBehaviour container)
    {
        if (SwrveHelper.IsOnDevice ()) {
            try {
                this.googlePlayAdvertisingId = storage.Load(GoogleAdvertisingIdSave);
                string jniPluginClassName = SwrveAndroidGCMPushPluginPackageName.Replace(".", "/");

                if (AndroidJNI.FindClass(jniPluginClassName).ToInt32() != 0) {
                    androidPlugin = new AndroidJavaClass(SwrveAndroidGCMPushPluginPackageName);
                    if (androidPlugin != null) {
                        androidPlugin.CallStatic<bool>("requestAdvertisingId", container.name);
                    }
                }
            } catch (Exception exp) {
                SwrveLog.LogError("Could not request Advertising Id: " + exp.ToString());
            }
        }
    }

    private string AndroidGetTimezone()
    {
        try {
            AndroidJavaObject cal = new AndroidJavaObject("java.util.GregorianCalendar");
            return cal.Call<AndroidJavaObject>("getTimeZone").Call<string>("getID");
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get the device timezone, make sure you are running on an Android device: " + exp.ToString());
        }

        return null;
    }

    private string AndroidGetRegion()
    {
        try {
            using (AndroidJavaClass localeJavaClass = new AndroidJavaClass("java.util.Locale")) {
                AndroidJavaObject defaultLocale = localeJavaClass.CallStatic<AndroidJavaObject>("getDefault");
                return defaultLocale.Call<string>("getCountry");
            }
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get the device region, make sure you are running on an Android device: " + exp.ToString());
        }

        return null;
    }

    private string AndroidGetAppVersion()
    {
        if (SwrveHelper.IsOnDevice ()) {
            try {
                using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass (UnityPlayerName)) {
                    AndroidJavaObject context = unityPlayerClass.GetStatic<AndroidJavaObject> (UnityCurrentActivityName);
                    string packageName = context.Call<string> ("getPackageName");
                    string versionName = context.Call<AndroidJavaObject> ("getPackageManager")
                    .Call<AndroidJavaObject> ("getPackageInfo", packageName, 0).Get<string> ("versionName");
                    return versionName;
                }
            } catch (Exception exp) {
                SwrveLog.LogWarning ("Couldn't get the device app version, make sure you are running on an Android device: " + exp.ToString ());
            }
        }

        return null;
    }

    private string _androidId;
    private string AndroidGetAndroidId()
    {
        if (SwrveHelper.IsOnDevice () && (_androidId == null)) {
            try {
                using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass(UnityPlayerName)) {
                    AndroidJavaObject context = unityPlayerClass.GetStatic<AndroidJavaObject>(UnityCurrentActivityName);
                    AndroidJavaObject contentResolver = context.Call<AndroidJavaObject> ("getContentResolver");
                    AndroidJavaClass settingsSecure = new AndroidJavaClass ("android.provider.Settings$Secure");
                    _androidId = settingsSecure.CallStatic<string> ("getString", contentResolver, "android_id");
                }
            } catch (Exception exp) {
                SwrveLog.LogWarning("Couldn't get the \"android_id\" resource, make sure you are running on an Android device: " + exp.ToString());
            }
        }
        return _androidId;
    }

    /// <summary>
    /// Used internally by the Android Push plugin to notify
    /// of a device registration id.
    /// </summary>
    /// <param name="registrationId">
    /// The new device registration id.
    /// </param>
    public void RegistrationIdReceived(string registrationId)
    {
        if (!string.IsNullOrEmpty(registrationId)) {
            bool sendDeviceInfo = (this.registrationToken != registrationId);
            if (sendDeviceInfo) {
                this.registrationToken = registrationId;
	            if (config.AndroidPushProvider == SwrveUnity.AndroidPushProvider.AMAZON_ADM) {
	                storage.Save(AdmDeviceTokenSave, this.registrationToken);
            	} else if (config.AndroidPushProvider == SwrveUnity.AndroidPushProvider.GOOGLE_GCM) {
	                storage.Save(GcmDeviceTokenSave, this.registrationToken);
            	}
                if (qaUser != null) {
                    qaUser.UpdateDeviceInfo();
                }
                SendDeviceInfo();
            }
        }
    }

    /// <summary>
    /// Used internally by the Android Push plugin to notify
    /// of a received push notification, without any user interaction.
    /// </summary>
    /// <param name="notificationJson">
    /// Serialized push notification information.
    /// </param>
    public void NotificationReceived(string notificationJson)
    {
        Dictionary<string, object> notification = (Dictionary<string, object>)Json.Deserialize (notificationJson);
        if (androidPlugin != null && notification != null) {
            string pushId = GetPushId(notification);
            if (pushId != null) {
                // Acknowledge the received notification
                androidPlugin.CallStatic("sdkAcknowledgeReceivedNotification", pushId);
            }
        }

        if (PushNotificationListener != null) {
            try {
                PushNotificationListener.OnNotificationReceived(notification);
            } catch (Exception exp) {
                SwrveLog.LogError("Error processing the push notification: " + exp.Message);
            }
        }
    }

    /// <summary>
    /// Obtain the Swrve identifier from a received push notification.
    /// </summary>
    /// <param name="notification">
    /// Push notification received by the app.
    /// </param>
    private string GetPushId(Dictionary<string, object> notification)
    {
        if  (notification != null && notification.ContainsKey(PushTrackingKey)) {
            return notification[PushTrackingKey].ToString();
        } else {
            SwrveLog.Log("Got unidentified notification");
        }

        return null;
    }

    /// <summary>
    /// Used internally by the Android Push plugin to notify
    /// of a received push notification when the app was opened from it.
    /// </summary>
    /// <param name="notificationJson">
    /// Serialized push notification information.
    /// </param>
    public void OpenedFromPushNotification(string notificationJson)
    {
        Dictionary<string, object> notification = (Dictionary<string, object>)Json.Deserialize (notificationJson);
        string pushId = GetPushId(notification);
        SendPushNotificationEngagedEvent(pushId);
        if (pushId != null && androidPlugin != null) {
            // Acknowledge the received notification
            androidPlugin.CallStatic("sdkAcknowledgeOpenedNotification", pushId);
        }

        // Process push deeplink
        if (notification != null && notification.ContainsKey (PushDeeplinkKey)) {
            object deeplinkUrl = notification[PushDeeplinkKey];
            if (deeplinkUrl != null) {
                OpenURL(deeplinkUrl.ToString());
            }
        }

        if (PushNotificationListener != null) {
            try {
                PushNotificationListener.OnOpenedFromPushNotification(notification);
            } catch (Exception exp) {
                SwrveLog.LogError("Error processing the push notification: " + exp.Message);
            }
        }
    }

    private void initNative ()
    {
        AndroidInitNative();
    }

    private void AndroidInitNative()
    {
        try {
            AndroidGetBridge ();
        } catch (Exception exp) {
            SwrveLog.LogWarning ("Couldn't init common from Android: " + exp.ToString ());
        }
    }

    private AndroidJavaObject _androidBridge;
    private AndroidJavaObject AndroidGetBridge()
    {
        if (SwrveHelper.IsOnDevice ()) {
            using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass (SwrveAndroidUnityCommonName)) {
                if (null == _androidBridge || !unityPlayerClass.CallStatic<bool> (IsInitialisedName)) {
                    _androidBridge = new AndroidJavaObject (SwrveAndroidUnityCommonName, GetNativeDetails ());
                }
            }
        }
        return _androidBridge;
    }

    private void setNativeAppVersion ()
    {
        config.AppVersion = AndroidGetAppVersion();
    }

    private void showNativeConversation (string conversation)
    {
        try {
            AndroidGetBridge().Call(ShowConversationName, conversation);
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't show conversation from Android: " + exp.ToString());
        }
    }

    private void startNativeLocation()
    {
        if (SwrveHelper.IsOnDevice ()) {
            try {
                AndroidGetBridge ();
                AndroidJavaClass swrvePlotClass = new AndroidJavaClass (SwrveAndroidPlotName);

                using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass (UnityPlayerName)) {
                    AndroidJavaObject context = unityPlayerClass.GetStatic<AndroidJavaObject>(UnityCurrentActivityName);
                    swrvePlotClass.CallStatic (SwrvePlotOnCreateName, context);
                }
                startedPlot = true;
            } catch (Exception exp) {
                SwrveLog.LogWarning ("Couldn't StartPlot from Android: " + exp.ToString ());
            }
        }
    }

    private void setNativeConversationVersion()
    {
        try {
            SetConversationVersion (AndroidGetBridge().Call<int> (GetConversationVersionName));
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get conversations version from Android: " + exp.ToString());
        }
    }

    private bool NativeIsBackPressed ()
    {
        return Input.GetKeyDown (KeyCode.Escape);
    }

}

#endif
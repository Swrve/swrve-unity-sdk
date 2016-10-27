#if !UNITY_5_0_0
#define SWRVE_USE_HTTPS_DEFAULTS
#endif

using System;
using SwrveUnity.Messaging;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SwrveUnity
{

  /// <summary>
  /// Available stacks to choose from
  /// </summary>
  public enum Stack
  {
      US, EU
  }

  /// <summary>
  /// Available stacks to choose from
  /// </summary>
  public enum AndroidPushProvider
  {
      GOOGLE_GCM, 
      AMAZON_ADM,
      NONE
  }


/// <summary>
/// Configuration for the Swrve SDK.
/// </summary>
[System.Serializable]
public class SwrveConfig
{
    /// <summary>
    /// Custom unique user id. The SDK will fill this property
    /// with a unique or random user id if you provide none.
    /// </summary>
    public string UserId;

    /// <summary>
    /// Version of the app.
    /// </summary>
    public string AppVersion;

    /// <summary>
    /// App store where the app will be submitted.
    /// </summary>
#if UNITY_ANDROID
    public string AppStore = SwrveAppStore.Google;
#elif UNITY_IPHONE
    public string AppStore = SwrveAppStore.Apple;
#elif UNITY_WSA_10_0
    public string AppStore = SwrveAppStore.Windows;
#else
    public string AppStore = null;
#endif

    /// <summary>
    /// Device language. The SDK will use native plugins
    /// to obtain the language if you provide none.
    /// </summary>
    public string Language;

    /// <summary>
    /// Sets the current language from the given culture.
    /// The SDK will use native plugins to obtain the
    /// language if you provide none.
    /// </summary>
    public CultureInfo Culture
    {
        set {
            Language = value.Name;
        }
    }

    /// <summary>
    /// Default language if there was none specified
    /// or it couldn't be retrieved by the native plugins.
    /// </summary>
    public string DefaultLanguage = "en";

    /// <summary>
    /// Enable or disable Talk features for in-app campaigns/
    /// </summary>
    public bool TalkEnabled = true;

    /// <summary>
    /// Enable or disable Conversations features for in-app campaigns/
    /// </summary>
    public bool ConversationsEnabled = true;

    /// <summary>
    /// Enable or disable Location features for in-app campaigns/
    /// </summary>
    public bool LocationEnabled = false;

    /// <summary>
    /// Set whether Location (plot) will autostart, or whether you want to enable it manually (after asking for permission)
    /// </summary>
    public bool LocationAutostart = false;

    /// <summary>
    /// Automatically download campaigns and user resources.
    /// </summary>
    public bool AutoDownloadCampaignsAndResources = true;

    /// <summary>
    /// Orientations supported by the app.
    /// </summary>
    public SwrveOrientation Orientation = SwrveOrientation.Both;

    /// <summary>
    /// The URL of the server to send events to.
    /// </summary>
    public string EventsServer = DefaultEventsServer;
    public const string DefaultEventsServer =
#if SWRVE_USE_HTTPS_DEFAULTS
        "https://api.swrve.com";
#else
        "http://api.swrve.com";
#endif

    /// <summary>
    /// Use HTTPS for the event server.
    /// </summary>
    public bool UseHttpsForEventsServer =
#if SWRVE_USE_HTTPS_DEFAULTS
        true;
#else
        false;
#endif

    /// <summary>
    /// The URL of the server to request campaign and resources data from.
    /// </summary>
    public string ContentServer = DefaultContentServer;
    public const string DefaultContentServer =
#if SWRVE_USE_HTTPS_DEFAULTS
        "https://content.swrve.com";
#else
        "http://content.swrve.com";
#endif

    /// <summary>
    /// Use HTTPS for the in-app message and resources server.
    /// </summary>
    public bool UseHttpsForContentServer =
#if SWRVE_USE_HTTPS_DEFAULTS
        true;
#else
        false;
#endif

    /// <summary>
    /// The SDK will send a session start on init and manage app pauses and resumes.
    /// </summary>
    public bool AutomaticSessionManagement = true;

    /// <summary>
    /// Threshold in seconds to send a new session start after the app lost focus and regained it again.
    /// </summary>
    public int NewSessionInterval = 30;

    /// <summary>
    /// The maximum number of event characters to buffer before events are discarded.
    /// </summary>
    public int MaxBufferChars = 262144;

    /// <summary>
    /// The maximum number of event characters to buffer before events are discarded.
    /// </summary>
    public bool SendEventsIfBufferTooLarge = true;

    /// <summary>
    /// Force saved data into PlayerPrefs.
    /// </summary>
    /// <remarks>
    /// Used for debugging; an appropriate location is otherwise automatically chosen for saved data.
    /// </remarks>
    public bool StoreDataInPlayerPrefs = false;

    /// <summary>
    /// Selected Swrve stack.
    /// </summary>
    public Stack SelectedStack = Stack.US;

    /// <summary>
    /// Enable push notification on this app.
    /// </summary>
    public bool PushNotificationEnabled = false;

    /// <summary>
    /// Events that will trigger a push notification permission dialog in iOS.
    /// </summary>
    public HashSet<String> PushNotificationEvents = new HashSet<string> ()
    { "Swrve.session.start"
    };

    /// <summary>
    /// The Google Cloud Messaaging Sender Id obtained from the Google Cloud Console.
    /// </summary>
    public string GCMSenderId = null;

    /// <summary>
    /// The title that will appear for each push notification received through Google Cloud Messaging.
    /// </summary>
    public string GCMPushNotificationTitle = "#Your App Title";

    /// <summary>
    /// The resource identifier for the icon that will be displayed on your GCM notifications.
    /// </summary>
    public string GCMPushNotificationIconId = null;

    /// <summary>
    /// The resource identifier for the Material icon that will be displayed on your GCM notifications
    /// on Android L+.
    /// https://developer.android.com/about/versions/android-5.0-changes.html#BehaviorNotifications
    /// </summary>
    public string GCMPushNotificationMaterialIconId = null;

    /// <summary>
    /// The resource identifier for the large icon that will be displayed on your GCM notifications.
    /// </summary>
    public string GCMPushNotificationLargeIconId = null;

    /// <summary>
    /// The color (argb) that will be used as accent color for your GCM notifications.
    /// </summary>
    public int GCMPushNotificationAccentColor = -1;


    /// <summary>
    /// The title that will appear for each push notification received through Amazon Device Messaging.
    /// </summary>
    public string ADMPushNotificationTitle = "#Your App Title";

    /// <summary>
    /// The resource identifier for the icon that will be displayed on your ADM notifications.
    /// </summary>
    public string ADMPushNotificationIconId = null;

    /// <summary>
    /// The resource identifier for the Material icon that will be displayed on your ADM notifications
    /// on Android L+.
    /// https://developer.android.com/about/versions/android-5.0-changes.html#BehaviorNotifications
    /// </summary>
    public string ADMPushNotificationMaterialIconId = null;

    /// <summary>
    /// The resource identifier for the large icon that will be displayed on your ADM notifications.
    /// </summary>
    public string ADMPushNotificationLargeIconId = null;

    /// <summary>
    /// The color (argb) that will be used as accent color for your GCM notifications.
    /// </summary>
    public int ADMPushNotificationAccentColor = -1;

    /// <summary>
    /// Push provider type. GCM is the default. Set to AMAZON_ADM if using Kindle
    /// Requires the use of the correct native Android plugin (Google or Amazon variant).
    /// </summary>
    public AndroidPushProvider AndroidPushProvider = AndroidPushProvider.GOOGLE_GCM;

    /// <summary>
    /// Maximum delay in seconds for in-app messages to appear after initialization.
    /// </summary>
    public float AutoShowMessagesMaxDelay = 5;

    /// <summary>
    /// Default in-app background color if none is set in the template.
    /// </summary>
    public Color? DefaultBackgroundColor = null;

    /// <summary>
    /// Log Google's Advertising ID as "swrve.GAID". Requires the use of the native google Android plugin.
    /// </summary>
    public bool LogGoogleAdvertisingId = false;

    /// <summary>
    /// Log Android ID as "swrve.android_id"
    /// </summary>
    public bool LogAndroidId = false;

    /// <summary>
    /// Log iOS IDFV as "swrve.IDFV"
    /// </summary>
    public bool LogAppleIDFV = false;

    /// <summary>
    /// Log iOS IDFV as "swrve.IDFA"
    /// </summary>
    public bool LogAppleIDFA = false;

    // iOS Push Categories
    public List<UIUserNotificationCategory> pushCategories = new List<UIUserNotificationCategory>();

    public void CalculateEndpoints (int appId)
    {
        // Default values are saved in the prefab or component instance.
        if (string.IsNullOrEmpty(EventsServer) || EventsServer == DefaultEventsServer) {
            EventsServer = CalculateEndpoint(UseHttpsForEventsServer, appId, SelectedStack, "api.swrve.com");
        }
        if (string.IsNullOrEmpty(ContentServer) || ContentServer == DefaultContentServer) {
            ContentServer = CalculateEndpoint(UseHttpsForContentServer, appId, SelectedStack, "content.swrve.com");
        }
    }

    private static string GetStackPrefix(Stack stack)
    {
        if (stack == Stack.EU) {
            return "eu-";
        }
        return "";
    }

    private static string HttpSchema(bool useHttps)
    {
        return useHttps? "https" : "http";
    }

    private static string CalculateEndpoint(bool useHttps, int appId, Stack stack, string suffix)
    {
        return HttpSchema(useHttps) + "://" + appId + "." + GetStackPrefix(stack) + suffix;
    }
}
}

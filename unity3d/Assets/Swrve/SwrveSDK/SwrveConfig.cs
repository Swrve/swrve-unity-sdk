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
public enum Stack {
    US, EU
}

/// <summary>
/// Available stacks to choose from
/// </summary>
public enum AndroidPushProvider {
    AMAZON_ADM,
    GOOGLE_FIREBASE,
    NONE
}

public enum SwrveInitMode {
    AUTO, 
    MANAGED
}


/// <summary>
/// Configuration for the Swrve SDK.
/// </summary>
[System.Serializable]
public class SwrveConfig
{
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
    /// Enable or disable messaaging features.
    /// </summary>
    public bool MessagingEnabled = true;

    /// <summary>
    /// Enable or disable Conversations features for in-app campaigns.
    /// </summary>
    public bool ConversationsEnabled = true;

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
        "https://api.swrve.com";

    /// <summary>
    /// The URL of the server to identity the user.
    /// </summary>
    public string IdentityServer = DefaultIdentityServer;
    public const string DefaultIdentityServer = "https://identity.swrve.com";
    /// <summary>
    /// The URL of the server to request campaign and resources data from.
    /// </summary>
    public string ContentServer = DefaultContentServer;
    public const string DefaultContentServer =
        "https://content.swrve.com";

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
    /// Events that will register the device for provisional push in iOS.
    /// </summary>
    public HashSet<String> ProvisionalPushNotificationEvents = null;

    /// <summary>
    /// Events that will trigger a push notification permission dialog in iOS.
    /// </summary>
    public HashSet<String> PushNotificationEvents = new HashSet<string> ()
    { "Swrve.session.start"
    };

    /// <summary>
    /// The resource identifier for the icon that will be displayed on your Android notifications.
    /// </summary>
    public string AndroidPushNotificationIconId = null;

    /// <summary>
    /// The resource identifier for the Material icon that will be displayed on your Android L+ notifications.
    /// https://developer.android.com/about/versions/android-5.0-changes.html#BehaviorNotifications
    /// </summary>
    public string AndroidPushNotificationMaterialIconId = null;

    /// <summary>
    /// The resource identifier for the large icon that will be displayed on your Android notifications.
    /// </summary>
    public string AndroidPushNotificationLargeIconId = null;

    /// <summary>
    /// The color (Hex) that will be used as accent color for your Android notifications.
    /// </summary>
    public string AndroidPushNotificationAccentColorHex = null;

    /// <summary>
    /// Push provider type. GOOGLE_FIREBASE is the default. Set to AMAZON_ADM if using Kindle.
    /// Requires the use of the correct native Android plugin (Firebase or Amazon variant).
    /// See Docs for integration guide.
    /// </summary>
    public AndroidPushProvider AndroidPushProvider = AndroidPushProvider.GOOGLE_FIREBASE;

    /// <summary>
    /// Default Android O+ channel that will be used to display notifications.
    /// </summary>
    public AndroidChannel DefaultAndroidChannel;

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
    /// Log iOS IDFV as "swrve.IDFA". This also requires the addition of the custom define 'SWRVE_LOG_IDFA' in your project settings.
    /// </summary>
    public bool LogAppleIDFA = false;

    /// <summary>
    // iOS Push Categories
    /// </summary>
    public List<UNNotificationCategory> NotificationCategories = new List<UNNotificationCategory>();

    /// <summary>
    /// Obtain information about the AB Tests a user is part of.
    /// </summary>
    public bool ABTestDetailsEnabled = false;

    /// <summary>
    /// Install button listener for all in-app messages.
    /// </summary>
    public ISwrveInstallButtonListener InAppMessageInstallButtonListener = null;

    /// <summary>
    /// Custom button listener for all in-app messages.
    /// </summary>
    public ISwrveCustomButtonListener InAppMessageCustomButtonListener = null;

    /// <summary>
    /// In-app message listener.
    /// </summary>
    public ISwrveMessageListener InAppMessageListener = null;

    /// <summary>
    /// Listener for push notifications received in the app.
    /// </summary>
    public ISwrvePushNotificationListener PushNotificationListener = null;

    /// <summary>
    /// Disable default In-app renderer and manage messages manually.
    /// </summary>
    public ISwrveTriggeredMessageListener TriggeredMessageListener = null;

    /// <summary>
    /// Initialisation Mode. AUTO is the default. 
    /// </summary>
    public SwrveInitMode InitMode = SwrveInitMode.AUTO;

    /// <summary>
    /// When initMode is set to a MANAGED state. Continue on startup with the previously given user
    /// </summary>
    public bool ManagedModeAutoStartLastUser = true;

    /// <summary>
    /// A callback to get notified when user resources have been updated.
    /// If Config.AutoDownloadCampaignsAndResources is TRUE (default) user resources will be kept up to date automatically
    /// and this listener will be called whenever there has been a change.
    /// Instead of using the listener, you could use the SwrveResourceManager ([swrve getResourceManager]) to get
    /// the latest value for each attribute at the time you need it. Resources and attributes in the resourceManager
    /// are kept up to date.
    ///
    /// When Config.AutoDownloadCampaignsAndResources is FALSE resources will not be kept up to date, and you will have
    /// to manually call RefreshCampaignsAndResources - which will call this listener on completion.
    ///
    /// This listener does not have any argument, use the resourceManager to get the updated resources.
    /// </summary>
    public Action ResourcesUpdatedCallback;

    public void CalculateEndpoints (int appId)
    {
        // Default values are saved in the prefab or component instance.
        if (string.IsNullOrEmpty(EventsServer) || EventsServer == DefaultEventsServer) {
            EventsServer = CalculateEndpoint(appId, SelectedStack, "api.swrve.com");
        }
        if (string.IsNullOrEmpty(ContentServer) || ContentServer == DefaultContentServer) {
            ContentServer = CalculateEndpoint(appId, SelectedStack, "content.swrve.com");
        }
        if (string.IsNullOrEmpty(IdentityServer) || IdentityServer == DefaultIdentityServer) {
            IdentityServer = CalculateEndpoint(appId, SelectedStack, "identity.swrve.com");
        }
    }

    private static string GetStackPrefix(Stack stack)
    {
        if (stack == Stack.EU) {
            return "eu-";
        }
        return "";
    }

    private static string CalculateEndpoint(int appId, Stack stack, string suffix)
    {
        return "https://" + appId + "." + GetStackPrefix(stack) + suffix;
    }
}
}

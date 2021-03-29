#if UNITY_IPHONE || UNITY_ANDROID || UNITY_STANDALONE
#define SWRVE_SUPPORTED_PLATFORM
#endif
using UnityEngine;
using System;
using System.Linq;
using System.Collections;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using SwrveUnity;
using SwrveUnityMiniJSON;
using SwrveUnity.Messaging;
using SwrveUnity.Helpers;
using SwrveUnity.ResourceManager;
using SwrveUnity.Device;
using SwrveUnity.IAP;
using SwrveUnity.SwrveUsers;

#if UNITY_IPHONE
using System.Runtime.InteropServices;
#endif

#if (UNITY_WP8 || UNITY_METRO) && !UNITY_WSA_10_0
#warning "Please note that the Windows build of the Unity SDK is not supported by Swrve and customers use it at their own risk.  It is not covered by any performance warranty otherwise offered by Swrve"
#endif

#if (UNITY_2_6 || UNITY_2_6_1 || UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || !UNITY_2017_4_OR_NEWER)
#error "The Swrve SDK needs to be compiled with Unity 2017.4+"
#endif

/// <summary>
/// Main class implementation of the Swrve SDK.
/// </summary>
public partial class SwrveSDK
{
    public const string SdkVersion = "7.3.1";

    protected int appId;
    /// <summary>
    /// App ID used to initialize the SDK.
    /// </summary>
    public int AppId
    {
        get {
            return appId;
        }
    }

    protected string apiKey;
    /// <summary>
    /// Secret API key used to initialize the SDK.
    /// </summary>
    public string ApiKey
    {
        get {
            return apiKey;
        }
    }

    /// <summary>
    /// User ID assigned to this device.
    /// </summary>
    public string UserId
    {
        get {
            return profileManager.userId;
        }
    }

    internal SwrveConfig config;

    /// <summary>
    /// Current language of the app or device.
    /// </summary>
    public string Language;

    /// <summary>
    /// Use this object to obtain the latest resources
    /// and values for this user.
    /// </summary>
    public SwrveResourceManager ResourceManager;

    protected ISwrveAssetsManager SwrveAssetsManager;

    /// <summary>
    /// Container MonoBehaviour object in the scene. Used
    /// to call coroutines and other Unity3D specific
    /// methods.
    /// </summary>
    public MonoBehaviour Container;

#if UNITY_EDITOR
    public Action<string> ConversationEditorCallback;
#endif

    /// <summary>
    /// OnSuccessIdentify delegate for when Identify API call completes successfully
    /// </summary>
    public delegate void OnSuccessIdentify(string status, string swrve_id);
    /// <summary>
    /// OnErrorIdentify delegate for when Identify API call completes fails
    /// </summary>
    public delegate void OnErrorIdentify(long httpCode, string errorMessage);

    /// <summary>
    /// Flag to indicate that the SDK was initialised.
    /// </summary>
    public bool Initialised = false;

    /// <summary>
    /// Flag to indicate that the SDK was destroyed.
    /// </summary>
    public bool Destroyed = false;

    /// <summary>
    /// Initialise the SDK with the given app id, api key and config.
    /// </summary>
    /// <param name="container">
    /// MonoBehaviour objecto to act as a container.
    /// </param>
    /// <param name="appId">
    /// App ID for your app, as provided by Swrve.
    /// </param>
    /// <param name="apiKey">
    /// API key for your app, as provided by Swrve.
    /// </param>
    /// <param name="config">
    /// Optional configuration object with advanced settings.
    /// </param>
    public virtual void Init(MonoBehaviour container, int appId, string apiKey, SwrveConfig config = null)
    {

        if (config == null) {
            config = new SwrveConfig();
        }

        this.Container = container;
        this.ResourceManager = new SwrveUnity.ResourceManager.SwrveResourceManager();
        this.config = config;
        this.prefabName = container.name;
        this.appId = appId;
        this.apiKey = apiKey;
        this.Language = config.Language;
#if SWRVE_SUPPORTED_PLATFORM

        //Init profile manager (it already load/create a default userId)
        profileManager = new SwrveProfileManager(config.InitMode);

        // Storage path/init.
        swrvePath = GetSwrvePath();
        storage = CreateStorage();
        storage.SetSecureFailedListener(delegate () {
            NamedEventInternal("Swrve.signature_invalid", null, false);
        });
        swrveTemporaryPath = GetSwrveTemporaryCachePath();
        this.InitAssetsManager(container, swrveTemporaryPath);

        // Check for migrations.
        SwrveMigrationsManager migrationManager = new SwrveMigrationsManager(storage, profileManager);
        migrationManager.CheckMigrations();

        // Load install date
        string installTimeSecondsFromFile = storage.Load(AppInstallTimeSecondsSave);
        if (string.IsNullOrEmpty(installTimeSecondsFromFile)) {
            installTimeSeconds = GetSessionTime();
            storage.Save(AppInstallTimeSecondsSave, userInitTimeSeconds.ToString());
        } else {
            long.TryParse(installTimeSecondsFromFile, out installTimeSeconds);
        }
        installTimeSecondsFormatted = SwrveHelper.EpochToFormat(installTimeSeconds, InstallTimeFormat);

        // Check API key
        if (string.IsNullOrEmpty(apiKey)) {
            throw new Exception("The api key has not been specified.");
        }

        // Language
        if (string.IsNullOrEmpty(Language)) {
            // Get language on the device
            Language = GetDeviceLanguage();
            if (string.IsNullOrEmpty(Language)) {
                Language = config.DefaultLanguage;
            }
        }

        // End points
        config.CalculateEndpoints(appId);
        string abTestServer = config.ContentServer;
        eventsUrl = config.EventsServer + "/1/batch";
        identifyUrl = config.IdentityServer + "/identify";
        abTestResourcesDiffUrl = abTestServer + "/api/1/user_resources_diff";
        resourcesAndCampaignsUrl = abTestServer + "/api/1/user_content";

        // Event Buffer
        eventBufferStringBuilder = new StringBuilder(config.MaxBufferChars);

        // REST Client
        restClient = CreateRestClient();

#if UNITY_ANDROID && !UNITY_EDITOR
        // set the default notification channel for location and remote notifications (for oreo+)
        SetDefaultNotificationChannel();

        // Ask for Android registration id
        if (config.AndroidPushProvider == AndroidPushProvider.AMAZON_ADM) {
            if (config.PushNotificationEnabled) {
                InitialisePushADM(Container);
            }
        } else if (config.AndroidPushProvider == AndroidPushProvider.GOOGLE_FIREBASE) {
            if (config.PushNotificationEnabled) {
                FirebaseRegisterForPushNotification(Container);
            }

            if (config.LogGoogleAdvertisingId) {
                RequestGooglePlayAdvertisingIdFirebase(Container);
            }
        }
#endif
        SwrveQaUser.Init(Container, config.EventsServer, apiKey, appId, UserId, GetAppVersion(), GetDeviceUUID(), storage);

        if (SwrveHelper.IsOnDevice()) {
            InitNative();
        }

        sdkStarted = ShouldAutoStart();

        if (sdkStarted) {
            this.InitUser();
            ProcessInfluenceData();

            // In-app messaging features
            if (config.MessagingEnabled) {
                // Check language and link token
                if (string.IsNullOrEmpty(Language)) {
                    throw new Exception("Language needed to use messaging");
                } else if (string.IsNullOrEmpty(config.AppStore)) {
                    // Default app-store by platform
#if UNITY_ANDROID
                    config.AppStore = SwrveAppStore.Google;
#elif UNITY_IPHONE
                    config.AppStore = SwrveAppStore.Apple;
#else
                    throw new Exception ("App store must be apple, google, amazon or a custom app store");
#endif
                }
            }
#endif
        }
    }

    protected virtual void InitAssetsManager(MonoBehaviour container, String swrveTemporaryPath)
    {
        this.SwrveAssetsManager = new SwrveAssetsManager(container, swrveTemporaryPath);
    }

    /// <summary>
    /// Buffer the event of the start of a play session.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "session_start" event.
    /// </remarks>
    public virtual void SessionStart()
    {
#if SWRVE_SUPPORTED_PLATFORM
        QueueSessionStart();
        SendQueuedEvents();
#endif
    }

    /// <summary>
    /// Buffer the event of the end of a play session.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "session_end" event.
    /// </remarks>
    public virtual void SessionEnd()
    {
#if SWRVE_SUPPORTED_PLATFORM
        Dictionary<string, object> json = new Dictionary<string, object>();
        AppendEventToBuffer("session_end", json);
#endif
    }

    /// <summary>
    /// Buffer a custom named event.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "event" event.
    /// </remarks>
    /// <param name="name">
    /// The name of the event that was triggered.
    /// </param>
    /// <param name="payload">
    /// Payload data for the event.
    /// </param>
    public virtual void NamedEvent(string name, Dictionary<string, string> payload = null)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return;
        }

        if (!string.IsNullOrEmpty(name) && !name.ToLower().StartsWith("swrve.")) {
            NamedEventInternal(name, payload);
        } else {
            SwrveLog.LogError("Event cannot be null, empty or begin with \"Swrve.\". The event " + name + " will not be sent");
        }
#endif
    }

    /// <summary>
    /// Buffer the event of a user update.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "user" event.
    /// </remarks>
    /// <param name="attributes">
    /// Attributes data for the user update.
    /// </param>
    public virtual void UserUpdate(Dictionary<string, string> attributes)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return;
        }

        if (attributes != null && attributes.Count > 0) {
            Dictionary<string, object> json = new Dictionary<string, object>();
            json.Add("attributes", attributes);
            AppendEventToBuffer("user", json);
        } else {
            SwrveLog.LogError("Invoked user update with no update attributes");
        }
#endif
    }

    /// <summary>
    /// Buffer the event of a user update with a Date Object
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "user" event.
    /// </remarks>
    /// <param name="name">
    /// Identifier associated with user update
    /// </param>
    /// <param name="date">
    /// DateTime object for user update
    /// </param>
    public virtual void UserUpdate(string name, DateTime date)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return;
        }

        if (name != null) {
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            var dateUTC = date.Date.ToUniversalTime();
            string dateAttribute = dateUTC.ToString(@"yyyy-MM-ddTHH:mm:ss.fffZ");
            attributes.Add(name, dateAttribute);

            Dictionary<string, object> json = new Dictionary<string, object>();
            json.Add("attributes", attributes);
            AppendEventToBuffer("user", json);
        } else {
            SwrveLog.LogError("Invoked user update with date with no name specified");
        }
#endif
    }

    /// <summary>
    /// Buffer the event of an item purchase.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "purchase" event.
    /// </remarks>
    /// <param name="item">
    /// UID of the item being purchased.
    /// </param>
    /// <param name="currency">
    /// Name of the currency used to purchase the item.
    /// </param>
    /// <param name="cost">
    /// Per-item cost of the item being purchased.
    /// </param>
    /// <param name="quantity">
    /// Quantity of the item being purchased.
    /// </param>
    public virtual void Purchase(string item, string currency, int cost, int quantity)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return;
        }

        Dictionary<string, object> json = new Dictionary<string, object>();
        json.Add("item", item);
        json.Add("currency", currency);
        json.Add("cost", cost);
        json.Add("quantity", quantity);
        AppendEventToBuffer("purchase", json);
#endif
    }

    /// <summary>
    /// Buffer the event of a purchase using real currency, where any in-app
    /// currencies were purchased, or where multiple items were purchased as part of a bundle.
    /// </summary>
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
    public virtual void Iap(int quantity, string productId, double productPrice, string currency, IapRewards rewards = null)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return;
        }

        if (rewards == null) {
            rewards = new IapRewards();
        }

        _Iap(quantity, productId, productPrice, currency, rewards, string.Empty, string.Empty, string.Empty, "unknown_store");
#endif
    }

    /// <summary>
    /// Buffer the event of a gift of in-app currency.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "currency_given" event.
    /// </remarks>
    /// <param name="givenCurrency">
    /// The name of the in-app currency that the player was rewarded with.
    /// </param>
    /// <param name="amount">
    /// The amount of in-app currency that the player was rewarded with.
    /// </param>
    public virtual void CurrencyGiven(string givenCurrency, double amount)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return;
        }

        Dictionary<string, object> json = new Dictionary<string, object>();
        json.Add("given_currency", givenCurrency);
        json.Add("given_amount", amount);
        AppendEventToBuffer("currency_given", json);
#endif
    }

    /// <summary>
    /// Send buffered events to the server.
    /// </summary>
    public virtual bool SendQueuedEvents()
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (trackingState == SwrveSdkState.EVENT_SENDING_PAUSED) {
            this.LogTrackingState();
            return false;
        }
        bool sentEvents = false;
        if (!eventsConnecting) {
            byte[] eventsPostEncodedData = null;
            if (eventsPostString == null || eventsPostString.Length == 0) {
                eventsPostString = eventBufferStringBuilder.ToString();
                eventBufferStringBuilder.Length = 0;
            }

            if (eventsPostString.Length > 0) {
                long time = GetSessionTime();
                eventsPostEncodedData = PostBodyBuilder.BuildEvent(apiKey, appId, this.UserId, GetDeviceUUID(), GetAppVersion(), time, eventsPostString);
            }

            // eventsConnecting will be true until there is a response or an error from the POST
            if (eventsPostEncodedData != null) {
                eventsConnecting = true;
                SwrveLog.Log("Sending events to Swrve");
                Dictionary<string, string> requestHeaders = new Dictionary<string, string> {
                    { @"Content-Type", @"application/json; charset=utf-8" },
                };
                sentEvents = true;
                StartTask("PostEvents_Coroutine", PostEvents_Coroutine(requestHeaders, eventsPostEncodedData));
            } else {
                eventsPostString = null;
            }
        } else {
            SwrveLog.LogWarning("Sending events already in progress");
        }

        return sentEvents;
#else
        return false;
#endif
    }

    /// <summary>
    /// Handle an deeplink event from an Ad Journey Campaign
    /// </summary>
    /// <param name="url">
    /// The deeplink url to process.
    /// </param>
    public virtual void HandleDeeplink(string url)
    {
        if (!IsSDKReady()) {
            return;
        }

        if (deeplinkManager == null) {
            deeplinkManager = new SwrveDeeplinkManager(Container, this);
        }

        deeplinkManager.HandleDeeplink(url);
    }

    /// <summary>
    /// This method is used to inform SDK that the App had to be installed first and the url loaded in a deferred manner.
    /// </summary>
    /// <param name="url">
    /// The deeplink url to process.
    /// </param>
    public virtual void HandleDeferredDeeplink(string url)
    {
        if (!IsSDKReady()) {
            return;
        }

        if (deeplinkManager == null) {
            deeplinkManager = new SwrveDeeplinkManager(Container, this);
        }

        deeplinkManager.HandleDeferredDeeplink(url);
    }

    /// <summary>
    /// Obtain the latest resources and values applied to this user.
    /// </summary>
    /// <param name="onResult">
    /// This callback will be called when the latest values are available.
    /// </param>
    /// <param name="onError">
    /// Callback for managing errors.
    /// </param>
    public virtual void GetUserResources(Action<Dictionary<string, Dictionary<string, string>>, string> onResult, Action<Exception> onError)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return;
        }

        if (trackingState == SwrveSdkState.EVENT_SENDING_PAUSED) return;
        if (Initialised) {
            if (userResources != null) {
                onResult.Invoke(userResources, userResourcesRaw);
            } else {
                onResult.Invoke(new Dictionary<string, Dictionary<string, string>>(), "[]");
            }
        }
#endif
    }

    /// <summary>
    /// Obtain the latst resources and values for this user in diff format.
    /// </summary>
    /// <param name="onResult">
    /// This callback will be called when the latest values are available.
    /// </param>
    /// <param name="onError">
    /// Callback for managing errors.
    /// </param>
    public virtual void GetUserResourcesDiff(Action<Dictionary<string, Dictionary<string, string>>, Dictionary<string, Dictionary<string, string>>, string> onResult, Action<Exception> onError)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return;
        }

        if (trackingState == SwrveSdkState.EVENT_SENDING_PAUSED) return;
        if (Initialised && !abTestUserResourcesDiffConnecting) {
            abTestUserResourcesDiffConnecting = true;
            StringBuilder getRequest = new StringBuilder(abTestResourcesDiffUrl);
            getRequest.AppendFormat("?user={0}&api_key={1}&app_version={2}&joined={3}", SwrveHelper.EscapeURL(this.UserId), apiKey, SwrveHelper.EscapeURL(GetAppVersion()), userInitTimeSeconds);

            SwrveLog.Log("AB Test User Resources Diff request: " + getRequest.ToString());

            StartTask("GetUserResourcesDiff_Coroutine", GetUserResourcesDiff_Coroutine(getRequest.ToString(), onResult, onError, AbTestUserResourcesDiffSave));
        } else {
            string errorStr = "Failed to initiate A/B test Diff GET request";
            SwrveLog.LogError(errorStr);
            if (onError != null) {
                onError.Invoke(new Exception(errorStr));
            }
        }
#endif
    }

    /// <summary>
    /// Obtain the latst real time user properties.
    /// </summary>
    /// <param name="onResult">
    /// This callback will be called when the latest values are available.
    /// </param>
    /// <param name="onError">
    /// Callback for managing errors.
    /// </param>
    public virtual void GetRealtimeUserProperties(Action<Dictionary<string, string>, string> onResult, Action<Exception> onError)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return;
        }

        if (trackingState == SwrveSdkState.EVENT_SENDING_PAUSED) return;
        if (Initialised) {
            if (realtimeUserProperties != null) {
                onResult.Invoke(realtimeUserProperties, realtimeUserPropertiesRaw);
            } else {
                onResult.Invoke(new Dictionary<string, string>(), "{}");
            }
        }
#endif
    }

    /// <summary>
    /// Loads unsent events and A/B test data from disk.
    /// </summary>
    public virtual void LoadFromDisk()
    {
#if SWRVE_SUPPORTED_PLATFORM
        LoadEventsFromDisk();
#endif
    }

    /// <summary>
    /// Save unsent events to disk.
    /// </summary>
    /// <param name="saveEventsBeingSent">
    /// Also save events that are in the outgoing buffer, trying to be sent.
    /// </param>
    public virtual void FlushToDisk(bool saveEventsBeingSent = false)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (trackingState == SwrveSdkState.EVENT_SENDING_PAUSED) {
            this.LogTrackingState();
            return;
        }
        if (Initialised) {
            // Concatenate existing events and buffer
            if (eventBufferStringBuilder != null) {
                StringBuilder savedEventStringBuilder = new StringBuilder();
                string bufferedEvents = eventBufferStringBuilder.ToString();
                // Clean event buffer to avoid sending twice
                eventBufferStringBuilder.Length = 0;
                // Save events that could be trying to be sent
                if (saveEventsBeingSent) {
                    savedEventStringBuilder.Append(eventsPostString);
                    eventsPostString = null;
                    if (bufferedEvents.Length > 0) {
                        if (savedEventStringBuilder.Length != 0) {
                            savedEventStringBuilder.Append(",");
                        }
                        savedEventStringBuilder.Append(bufferedEvents);
                    }
                } else {
                    savedEventStringBuilder.Append(bufferedEvents);
                }

                // Load old events saved in file
                try {
                    string loadedEvents = storage.Load(EventsSave, this.UserId);
                    if (!string.IsNullOrEmpty(loadedEvents)) {
                        if (savedEventStringBuilder.Length != 0) {
                            savedEventStringBuilder.Append(",");
                        }
                        savedEventStringBuilder.Append(loadedEvents);
                    }
                } catch (Exception e) {
                    SwrveLog.LogWarning("Could not read events from cache (" + e.ToString() + ")");
                }

                // Save all the events in the storage
                string savedEventString = savedEventStringBuilder.ToString();
                storage.Save(EventsSave, savedEventString, this.UserId);
            }
        }
#endif
    }

    /// <summary>
    /// Returns the base path for the file storage.
    /// </summary>
    public string BasePath()
    {
        return swrvePath;
    }

    /// <summary>
    /// Returns a map of Swrve device properties.
    /// </summary>
    public virtual Dictionary<string, string> GetDeviceInfo()
    {
        string deviceModel = GetDeviceModel();
        string osVersion = SystemInfo.operatingSystem;
        string os = GetPlatformOS();

        float dpi = (Screen.dpi == 0) ? DefaultDPI : Screen.dpi;

        Dictionary<string, string> deviceInfo = new Dictionary<string, string>() {
            { "swrve.device_name", deviceModel },
            { "swrve.os", os },
            { "swrve.device_width", deviceWidth.ToString () },
            { "swrve.device_height", deviceHeight.ToString () },
            { "swrve.device_dpi", dpi.ToString () },
            { "swrve.language", Language },
            { "swrve.os_version", osVersion },
            { "swrve.app_store", config.AppStore },
            { "swrve.sdk_version", Platform + SdkVersion },
            { "swrve.unity_version", Application.unityVersion },
            { "swrve.install_date", installTimeSecondsFormatted },
            { "swrve.device_type", GetDeviceType() }
        };

        String tzUtcOffsetSeconds = DateTimeOffset.Now.Offset.TotalSeconds.ToString();
        deviceInfo["swrve.utc_offset_seconds"] = tzUtcOffsetSeconds;

        setNativeInfo(deviceInfo);

        // Carrier info
        ICarrierInfo carrierInfo = GetCarrierInfoProvider();
        if (carrierInfo != null) {
            string carrierInfoName = carrierInfo.GetName();
            if (!string.IsNullOrEmpty(carrierInfoName)) {
                deviceInfo["swrve.sim_operator.name"] = carrierInfoName;
            }
            string carrierInfoIsoCountryCode = carrierInfo.GetIsoCountryCode();
            if (!string.IsNullOrEmpty(carrierInfoIsoCountryCode)) {
                deviceInfo["swrve.sim_operator.iso_country_code"] = carrierInfoIsoCountryCode;
            }
            string carrierInfoCarrierCode = carrierInfo.GetCarrierCode();
            if (!string.IsNullOrEmpty(carrierInfoCarrierCode)) {
                deviceInfo["swrve.sim_operator.code"] = carrierInfoCarrierCode;
            }
        }

        return deviceInfo;
    }

    /// <summary>
    /// Call this function when the app pauses (phone call, move to another app, etc.).
    /// This method is called automatically if using the SwrveComponent.
    /// </summary>
    public virtual void OnSwrvePause()
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return;
        }

        applicationPaused = true;
        if (Initialised) {
            FlushToDisk();
            // Session management
            GenerateNewSessionInterval();

            if (config != null && config.AutoDownloadCampaignsAndResources) {
                StopCheckForCampaignAndResources();
            }
        }
#endif
    }

    /// <summary>
    /// Call this function when the app resumes.
    /// This method is called automatically if using the SwrveComponent.
    /// </summary>
    public virtual void OnSwrveResume()
    {
#if SWRVE_SUPPORTED_PLATFORM
        applicationPaused = false;
        if (Initialised) {
            if (!IsSDKReady()) {
                return;
            }
#if UNITY_IPHONE
            RefreshPushPermissions();
#endif
            LoadFromDisk();
            QueueDeviceInfo();
            // Session management
            long currentTime = GetSessionTime();
            if (currentTime >= lastSessionTick) {
                SessionStart();
            } else {
                SendQueuedEvents();
            }
            GenerateNewSessionInterval();

            StartCampaignsAndResourcesTimer();
            DisableAutoShowAfterDelay();
            ProcessInfluenceData();

            if (IsMessageDisplaying() == false) {
                ConversationClosed();
            }

            DownloadAnyMissingAssets();
        }
#endif
    }

    /// <summary>
    /// Call this function when the app is being shutdown.
    /// This method is called automatically if using the SwrveComponent.
    /// </summary>
    public virtual void OnSwrveDestroy()
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!Destroyed) {
            Destroyed = true;
            if (Initialised) {
                FlushToDisk(true);
            }

            if (config != null && config.AutoDownloadCampaignsAndResources) {
                StopCheckForCampaignAndResources();
            }
        }
#endif
    }

    /// <summary>
    /// Get the latest available in-app campaigns.
    /// </summary>
    /// <returns>
    /// Latest in-app campagins available to the user.
    /// </returns>
    public virtual List<SwrveBaseCampaign> GetCampaigns()
    {
#if SWRVE_SUPPORTED_PLATFORM
        return campaigns;
#else
        return new List<SwrveBaseCampaign>();
#endif
    }

    /// <summary>
    /// Notify that a in-app message button was pressed.
    /// This method is automatically called by the SDK unless
    /// you are implementing your own rendering code.
    /// </summary>
    /// <param name="button">
    /// Button pressed by the user.
    /// </param>
    public virtual void ButtonWasPressedByUser(SwrveButton button)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (button != null) {
            try {
                SwrveLog.Log("Button " + button.ActionType + ": " + button.Action + " app id: " + button.AppId);
                if (button.ActionType != SwrveActionType.Dismiss) {
                    // Button other than dismiss pressed
                    String clickEvent = "Swrve.Messages.Message-" + button.Message.Id + ".click";
                    SwrveLog.Log("Sending click event: " + clickEvent);
                    Dictionary<string, string> clickPayload = new Dictionary<string, string>();
                    clickPayload.Add("name", button.Name);
                    clickPayload.Add("embedded", "false");
                    NamedEventInternal(clickEvent, clickPayload, false);
                }
            } catch (Exception e) {
                SwrveLog.LogError("Error while processing button click " + e);
            }

        }
#endif
    }

    /// <summary>
    /// Notify that a message was displayed to the user.
    /// This method is automatically called by the SDK unless
    /// you are implementing your own rendering code.
    /// </summary>
    /// <param name="messageFormat">
    /// Message format displayed to the user
    /// </param>
    public virtual void MessageWasShownToUser(SwrveMessageFormat messageFormat)
    {
#if SWRVE_SUPPORTED_PLATFORM
        try {
            // The message was shown. Take the current time so that we can throttle messages
            // from being shown too quickly.
            SetMessageMinDelayThrottle();
            this.messagesLeftToShow = this.messagesLeftToShow - 1;

            // Update next for round robin
            SwrveInAppCampaign campaign = (SwrveInAppCampaign)messageFormat.Message.Campaign;
            if (campaign != null) {
                campaign.MessageWasShownToUser(messageFormat);
                SaveCampaignData(campaign);
            }
            // Add a custom payload that define that isn't an embedded Message.
            Dictionary<string, string> payload = new Dictionary<string, string>();
            payload.Add("embedded", "false");
            // Send Impression event
            String viewEvent = "Swrve.Messages.Message-" + messageFormat.Message.Id + ".impression";
            SwrveLog.Log("Sending view event: " + viewEvent);
            NamedEventInternal(viewEvent, payload, false);
        } catch (Exception e) {
            SwrveLog.LogError("Error while processing message impression " + e);
        }
#endif
    }

    /// <summary>
    /// Determines if an in-app message is being displayed.
    /// </summary>
    /// <returns>
    /// True if there are any in-app messages currently on screen.
    /// </returns>
    public virtual bool IsMessageDisplaying()
    {
#if SWRVE_SUPPORTED_PLATFORM
        return (currentMessage != null);
#else
        return false;
#endif
    }

    /// <summary>
    /// Gets the app store link of a given app, that was
    /// setup in the dashboard for the current app store.
    /// </summary>
    /// <param name="appId">
    /// App identifier.
    /// </param>
    /// <returns>
    /// The app store link for a given app.
    /// </returns>
    public string GetAppStoreLink(int appId)
    {
#if SWRVE_SUPPORTED_PLATFORM
        string appStoreLink = null;
        if (appStoreLinks != null) {
            appStoreLinks.TryGetValue(appId.ToString(), out appStoreLink);
        }
        return appStoreLink;
#else
        return null;
#endif
    }
    [System.Obsolete("Use GetBaseMessageForEvent instead. This will be removed in 8.0")]
    /// <summary>
    /// Obtain an In-app message for the given event.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "event" event.
    /// </remarks>
    /// <param name="eventName">
    /// The name of the event that was triggered.
    /// </param>
    /// <returns>
    /// In-app message for the given event.
    /// </returns>
    public virtual SwrveMessage GetMessageForEvent(string eventName, IDictionary<string, string> eventPayload = null)
    {
        SwrveBaseMessage message = GetBaseMessageForEvent(eventName, eventPayload);
        if (message != null && message is SwrveMessage) {
            return (SwrveMessage)message;
        }
        return null;
    }

    /// <summary>
    /// Obtain an SwrveBaseMessage message for the given event.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "event" event.
    /// </remarks>
    /// <param name="eventName">
    /// The name of the event that was triggered.
    /// </param>
    /// <returns>
    /// SwrveBaseMessage message for the given event.
    /// </returns>
    public virtual SwrveBaseMessage GetBaseMessageForEvent(string eventName, IDictionary<string, string> eventPayload = null)
    {
        if (!IsSDKReady()) {
            return null;
        }

        if (!checkGlobalRules(eventName, eventPayload, SwrveHelper.GetNow())) {
            return null;
        }

        try {
            return _getBaseMessageForEvent(eventName, eventPayload);
        } catch (Exception e) {
            SwrveLog.LogError(e.ToString(), "message");
        }
        return null;
    }

    private SwrveBaseMessage _getBaseMessageForEvent(string eventName, IDictionary<string, string> eventPayload)
    {
#if SWRVE_SUPPORTED_PLATFORM
        SwrveBaseMessage result = null;
        SwrveBaseCampaign campaign = null;

        SwrveLog.Log("Trying to get message for: " + eventName);

        IEnumerator<SwrveBaseCampaign> itCampaign = campaigns.GetEnumerator();
        List<SwrveBaseMessage> availableMessages = new List<SwrveBaseMessage>();
        // Select messages with higher priority
        int minPriority = int.MaxValue;
        List<SwrveBaseMessage> candidateMessages = new List<SwrveBaseMessage>();
        SwrveOrientation deviceOrientation = GetDeviceOrientation();

        List<SwrveQaUserCampaignInfo> qaCampaignInfoList = new List<SwrveQaUserCampaignInfo>();
        while (itCampaign.MoveNext() && result == null) {
            if (!(itCampaign.Current is SwrveInAppCampaign || itCampaign.Current is SwrveEmbeddedCampaign)) {
                continue;
            }

            SwrveBaseCampaign nextCampaign = itCampaign.Current;
            SwrveBaseMessage nextMessage = null;
            if(nextCampaign is SwrveEmbeddedCampaign) {
                nextMessage = ((SwrveEmbeddedCampaign)nextCampaign).GetMessageForEvent(eventName, eventPayload, qaCampaignInfoList);
            } else if(nextCampaign is SwrveInAppCampaign) {
                nextMessage = ((SwrveInAppCampaign)nextCampaign).GetMessageForEvent(eventName, eventPayload, qaCampaignInfoList);
            }

            // Check if the message supports the current orientation
            if (nextMessage != null) {
                if (nextMessage.SupportsOrientation(deviceOrientation)) {
                    availableMessages.Add(nextMessage);
                    if (nextMessage.Priority <= minPriority) {
                        if (nextMessage.Priority < minPriority) {
                            // If it is lower than any of the previous ones
                            // remove those from being candidates
                            candidateMessages.Clear();
                        }
                        minPriority = nextMessage.Priority;
                        candidateMessages.Add(nextMessage);
                    }
                } else {
                    string reason = "Message didn't support the current device orientation: " + deviceOrientation;
                    SwrveQaUserCampaignInfo campaignInfo = new SwrveQaUserCampaignInfo(nextCampaign.Id, nextMessage.Id, nextCampaign.GetCampaignType(), false, reason);
                    qaCampaignInfoList.Add(campaignInfo);
                }
            }
        }

        // Select randomly from the highest messages
        if (candidateMessages.Count > 0) {
            candidateMessages.Shuffle();
            result = candidateMessages[0];
            campaign = result.Campaign;
        }

        if (SwrveQaUser.Instance.loggingEnabled && campaign != null && result != null) {
            // A message was chosen, check if other campaigns would have returned a message
            IEnumerator<SwrveBaseMessage> itOtherMessage = availableMessages.GetEnumerator();
            while (itOtherMessage.MoveNext()) {
                SwrveBaseCampaign otherCampaign = itOtherMessage.Current.Campaign;
                if (otherCampaign != result.Campaign) {
                    int otherCampaignId = otherCampaign.Id;
                    int otherVariantId = itOtherMessage.Current.Id;
                    string reason = "Campaign " + campaign.Id + " was selected for display ahead of this campaign";
                    SwrveQaUserCampaignInfo campaignInfo = new SwrveQaUserCampaignInfo(otherCampaignId, otherVariantId, otherCampaign.GetCampaignType(), false, reason);
                    qaCampaignInfoList.Add(campaignInfo);
                }
                SwrveQaUserCampaignInfo triggeredCampaignInfo = new SwrveQaUserCampaignInfo(campaign.Id, result.Id, campaign.GetCampaignType(), true);
                qaCampaignInfoList.Add(triggeredCampaignInfo);
            }
        }

        SwrveQaUser.CampaignTriggeredMessage(eventName, eventPayload, result != null, qaCampaignInfoList);

        return result;
#else
        return null;
#endif
    }

    /// <summary>
    /// Obtain a Swrve Message for the given event.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "event" event.
    /// </remarks>
    /// <param name="eventName">
    /// The name of the event that was triggered.
    /// </param>
    /// <returns>
    /// Swrve Message for the given event.
    /// </returns>
    public virtual SwrveConversation GetConversationForEvent(string eventName, IDictionary<string, string> eventPayload = null)
    {
        if (!IsSDKReady()) {
            return null;
        }

        if (!checkGlobalRules(eventName, eventPayload, SwrveHelper.GetNow())) {
            return null;
        }

        try {
            return _getConversationForEvent(eventName, eventPayload);
        } catch (Exception e) {
            SwrveLog.LogError(e.ToString(), SwrveQaUserCampaignInfo.SwrveCampaignType.Conversation.Value);
        }
        return null;
    }

    private SwrveConversation _getConversationForEvent(string eventName, IDictionary<string, string> eventPayload = null)
    {
#if SWRVE_SUPPORTED_PLATFORM
        SwrveConversation result = null;
        SwrveBaseCampaign campaign = null;
        SwrveLog.Log("Trying to get conversation for: " + eventName);

        IEnumerator<SwrveBaseCampaign> itCampaign = campaigns.GetEnumerator();
        List<SwrveConversation> availableConversations = new List<SwrveConversation>();

        // Select conversations with higher priority
        int minPriority = int.MaxValue;
        List<SwrveConversation> candidateConversations = new List<SwrveConversation>();
        List<SwrveQaUserCampaignInfo> qaCampaignInfoList = new List<SwrveQaUserCampaignInfo>();
        while (itCampaign.MoveNext() && result == null) {
            if (!(itCampaign.Current is SwrveConversationCampaign)) {
                continue;
            }

            SwrveConversationCampaign nextCampaign = (SwrveConversationCampaign)itCampaign.Current;
            SwrveConversation nextConversation = nextCampaign.GetConversationForEvent(eventName, eventPayload, qaCampaignInfoList);
            // Check if the message supports the current orientation
            if (nextConversation != null) {
                availableConversations.Add(nextConversation);
                if (nextConversation.Priority <= minPriority) {
                    if (nextConversation.Priority < minPriority) {
                        // If it is lower than any of the previous ones
                        // remove those from being candidates
                        candidateConversations.Clear();
                    }
                    minPriority = nextConversation.Priority;
                    candidateConversations.Add(nextConversation);
                }
            }
        }
        if (candidateConversations.Count > 0) {
            candidateConversations.Shuffle(); // Select randomly
            result = candidateConversations[0];
            campaign = result.Campaign;
        }

        if (SwrveQaUser.Instance.loggingEnabled && campaign != null && result != null) {
            // A message was chosen, check if other campaigns would have returned a message
            IEnumerator<SwrveConversation> itOtherMessage = availableConversations.GetEnumerator();
            while (itOtherMessage.MoveNext()) {
                SwrveBaseCampaign otherCampaign = itOtherMessage.Current.Campaign;
                if (otherCampaign != result.Campaign) {
                    int otherCampaignId = otherCampaign.Id;
                    int otherVariantId = itOtherMessage.Current.Id;
                    string reason = "Campaign " + campaign.Id + " was selected for display ahead of this campaign";
                    SwrveQaUserCampaignInfo campaignInfo = new SwrveQaUserCampaignInfo(otherCampaignId, otherVariantId, otherCampaign.GetCampaignType(), false, reason);
                    qaCampaignInfoList.Add(campaignInfo);
                }
                SwrveQaUserCampaignInfo triggeredCampaignInfo = new SwrveQaUserCampaignInfo(campaign.Id, result.Id, result.Campaign.GetCampaignType(), true);
                qaCampaignInfoList.Add(triggeredCampaignInfo);
            }
        }

        SwrveQaUser.CampaignTriggeredConversation(eventName, eventPayload, result != null, qaCampaignInfoList);

        return result;
#else
        return null;
#endif
    }

    private bool checkGlobalRules(string eventName, IDictionary<string, string> eventPayload, DateTime now)
    {
        if ((campaigns == null) || (campaigns.Count == 0)) {
            NoMessagesWereShown(eventName, eventPayload, "No campaigns available");
            return false;
        }

        if (!string.Equals(eventName, DefaultAutoShowMessagesTrigger, StringComparison.OrdinalIgnoreCase) && IsTooSoonToShowMessageAfterLaunch(now)) {
            NoMessagesWereShown(eventName, eventPayload, "{App throttle limit} Too soon after launch. Wait until " + showMessagesAfterLaunch.ToString(WaitTimeFormat));
            return false;
        }

        if (IsTooSoonToShowMessageAfterDelay(now)) {
            NoMessagesWereShown(eventName, eventPayload, "{App throttle limit} Too soon after last base message. Wait until " + showMessagesAfterDelay.ToString(WaitTimeFormat));
            return false;
        }

        if (HasShowTooManyMessagesAlready()) {
            NoMessagesWereShown(eventName, eventPayload, "{App throttle limit} Too many base messages shown");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Display the given campaign for a given orientation without the need to trigger an event and skipping
    /// the configured rules.
    /// </summary>
    /// <param name="campaign">
    /// The campaign to show.
    /// </param>
    /// <param name="orientation">
    /// The required orientation.
    /// </param>
    /// <param name="properties">
    /// Personalization properties.
    /// </param>
    public virtual void ShowMessageCenterCampaign(SwrveBaseCampaign campaign, SwrveOrientation? orientation = null, Dictionary<string, string> properties = null)
    {
        if (!IsSDKReady() || campaign == null) {
            return;
        }

        if (!orientation.HasValue) {
            orientation = GetDeviceOrientation();
        }

        ShowCampaign(campaign, false, orientation.Value, properties);
        campaign.Status = SwrveCampaignState.Status.Seen;
        SaveCampaignData(campaign);
    }

    /// <summary>
    /// Get the list active MessageCenter campaigns targeted for this user.
    /// It will exclude campaigns that have been deleted with the
    /// RemoveMessageCenterCampaign method and those that do not support the given orientation.
    /// </summary>
    /// <param name="orientation">
    /// Orientation which the message has to support. If null is provided the current device orientation is used.
    /// </param>
    /// <param name="properties">
    /// The personalization properties used to filter campaigns. If a campaign's personalization cannot be resolved it won't be returned if a non null value is provided.
    /// </param>
    /// <returns>
    /// List of active MessageCenter campaigns that support the given orientation.
    /// </returns>
    public virtual List<SwrveBaseCampaign> GetMessageCenterCampaigns(SwrveOrientation? orientation = null, Dictionary<string, string> properties = null)
    {
        if (!IsSDKReady()) {
            return null;
        }

        if (!orientation.HasValue) {
            orientation = GetDeviceOrientation();
        }

        List<SwrveBaseCampaign> result = new List<SwrveBaseCampaign>();
        IEnumerator<SwrveBaseCampaign> itCampaign = campaigns.GetEnumerator();

        while (itCampaign.MoveNext()) {
            SwrveBaseCampaign campaign = itCampaign.Current;
            if (IsValidMessageCenter(campaign, orientation.Value)) {
                bool add = true;
                if (properties != null && (campaign is SwrveInAppCampaign)) {
                    SwrveMessageTextTemplatingResolver resolver = new SwrveMessageTextTemplatingResolver();
                    add = resolver.ResolveTemplating((SwrveInAppCampaign)campaign, properties);
                }
                if (add) {
                    result.Add(campaign);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Remove this campaign. It won't be returned anymore by the 'GetMessageCenterCampaigns' methods.
    /// </summary>
    /// <param name="campaign">
    /// The campaign to remove.
    /// </param>
    public virtual void RemoveMessageCenterCampaign(SwrveBaseCampaign campaign)
    {
        if (campaign != null) {
            campaign.Status = SwrveCampaignState.Status.Deleted;
            SaveCampaignData(campaign);
        }
    }

    /// <summary>
    /// Mark this campaign as seen. This is done automatically by Swrve but you can call this if you are rendering the messages on your own.
    /// </summary>
    /// <param name="campaign">
    /// The campaign to mark as seen.
    /// </param>
    public virtual void MarkMessageCenterCampaignAsSeen(SwrveBaseCampaign campaign)
    {
        if (campaign != null) {
            campaign.Status = SwrveCampaignState.Status.Seen;
            SaveCampaignData(campaign);
        }
    }

    [System.Obsolete("Use GetBaseMessageForId instead. This will be removed in 8.0")]
    /// <summary>
    /// Obtain an in-app message for the given id.
    /// </summary>
    /// <param name="messageId">
    /// The id of the message you want to retrieve.
    /// </param>
    /// <returns>
    /// In-app message for the given id.
    /// </returns>
    public virtual SwrveMessage GetMessageForId(int messageId)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return null;
        }

        SwrveMessage message = null;
        IEnumerator<SwrveBaseCampaign> itCampaign = campaigns.GetEnumerator();
        while (itCampaign.MoveNext() && message == null) {
            if (!(itCampaign.Current is SwrveInAppCampaign)) {
                continue;
            }

            SwrveInAppCampaign campaign = (SwrveInAppCampaign)itCampaign.Current;
            message = campaign.GetMessageForId(messageId);
            if (message != null) {
                return message;
            }
        }

        SwrveLog.LogWarning("Message with id " + messageId + " not found");
#endif
        return null;
    }

    /// <summary>
    /// Obtain an in-app message for the given id.
    /// </summary>
    /// <param name="messageId">
    /// The id of the message you want to retrieve.
    /// </param>
    /// <returns>
    /// SwrveBaseMessage for the given id.
    /// </returns>
    public virtual SwrveBaseMessage GetBaseMessageForId(int messageId)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return null;
        }
        SwrveBaseMessage message = null;
        SwrveBaseCampaign campaign = null;
        IEnumerator<SwrveBaseCampaign> itCampaign = campaigns.GetEnumerator();
        while (itCampaign.MoveNext() && message == null) {
            if(itCampaign.Current is SwrveInAppCampaign) {
                campaign = (SwrveInAppCampaign)itCampaign.Current;
                message = ((SwrveInAppCampaign)campaign).GetMessageForId(messageId);
            } else if(itCampaign.Current is SwrveEmbeddedCampaign) {
                campaign = (SwrveEmbeddedCampaign)itCampaign.Current;
                if(((SwrveEmbeddedCampaign)campaign).Message.Id == messageId) {
                    message = ((SwrveEmbeddedCampaign)campaign).Message;
                }
            }
        }
        if (message != null) {
            return message;
        } else {
            SwrveLog.LogWarning("Message with id " + messageId + " not found");
        }

#endif
        return null;
    }

    /// <summary>
    /// Display a message available for the given trigger event.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "event" event.
    /// </remarks>
    /// <param name="eventName">
    /// The name of the event that was triggered.
    /// </param>
    /// <param name="message">
    /// Message to show.
    /// </param>
    /// <param name="installButtonListener">
    /// Button listener to recieve install button events. If null, the global button listener will be used.
    /// </param>
    /// <param name="customButtonListener">
    /// Button listener to recieve custom button events. If null, the global button listener will be used.
    /// </param>
    /// <param name="messageListener">
    /// Message listener to recieve message events. If null, the global message listener will be used.
    /// </param>
    /// <param name="clipboardButtonListener">
    /// Button listener to recieve custom button events. If null, the global button listener will be used.
    /// </param>
    public virtual IEnumerator ShowMessageForEvent(string eventName, SwrveBaseMessage message, ISwrveInstallButtonListener installButtonListener = null, ISwrveCustomButtonListener customButtonListener = null, ISwrveMessageListener messageListener = null, ISwrveClipboardButtonListener clipboardButtonListener = null, ISwrveEmbeddedMessageListener embeddedMessageListener = null)
    {
        return ShowMessageForEvent(eventName, null, message, installButtonListener, customButtonListener, messageListener, clipboardButtonListener, embeddedMessageListener);
    }

    /// <summary>
    /// Display a message available for the given trigger event.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "event" event.
    /// </remarks>
    /// <param name="eventName">
    /// The name of the event that was triggered.
    /// </param>
    /// <param name="payload">
    /// The payload of the event that was triggered.
    /// </param>
    /// <param name="message">
    /// Message to show.
    /// </param>
    /// <param name="installButtonListener">
    /// Button listener to recieve install button events. If null, the global button listener will be used.
    /// </param>
    /// <param name="customButtonListener">
    /// Button listener to recieve custom button events. If null, the global button listener will be used.
    /// </param>
    /// <param name="messageListener">
    /// Message listener to recieve message events. If null, the global message listener will be used.
    /// </param>
    /// <param name="clipboardButtonListener">
    /// Button listener to recieve custom button events. If null, the global button listener will be used.
    /// </param>
    public virtual IEnumerator ShowMessageForEvent(string eventName, IDictionary<string, string> payload, SwrveBaseMessage message, ISwrveInstallButtonListener installButtonListener = null, ISwrveCustomButtonListener customButtonListener = null, ISwrveMessageListener messageListener = null, ISwrveClipboardButtonListener clipboardButtonListener = null, ISwrveEmbeddedMessageListener embeddedMessageListener = null)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            yield return null;
        }

        if(message is SwrveMessage) {
            if (config.TriggeredMessageListener != null) {
                // They are using a custom listener
                if (message != null) {
                    config.TriggeredMessageListener.OnMessageTriggered((SwrveMessage)message);
                }
            } else {
                if (currentMessage == null) {
                    Dictionary<string, string> properties = null;
                    if (config.InAppMessageConfig != null && config.InAppMessageConfig.PersonalizationProvider != null) {
                        properties = config.InAppMessageConfig.PersonalizationProvider.Personalize(payload);
                    }
                    yield return Container.StartCoroutine(LaunchMessage(message, installButtonListener, customButtonListener, clipboardButtonListener, messageListener, properties));
                }
            }
        } else if(message is SwrveEmbeddedMessage) {
            if (config.EmbeddedMessageConfig.EmbeddedMessageListener != null && message != null) {
                config.EmbeddedMessageConfig.EmbeddedMessageListener.OnMessage((SwrveEmbeddedMessage)message);
            }
        }

        TaskFinished("ShowMessageForEvent");
#else
        yield return null;
#endif
    }

    /// <summary>
    /// Display a conversation for the given trigger event.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "event" event.
    /// </remarks>
    /// <param name="eventName">
    /// The name of the event that was triggered
    /// </param>
    public virtual IEnumerator ShowConversationForEvent(string eventName, SwrveConversation conversation)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            yield return null;
        }

        yield return Container.StartCoroutine(LaunchConversation(conversation));
        TaskFinished("ShowConversationForEvent");
#else
        yield return null;
#endif
    }

    /// <summary>
    /// Dismisses the current message if any is beign displayed.
    /// </summary>
    public virtual void DismissMessage()
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return;
        }

        if (config.TriggeredMessageListener != null) {
            config.TriggeredMessageListener.DismissCurrentMessage();
        } else {
            try {
                if (currentMessage != null) {
                    SetMessageMinDelayThrottle();
                    currentMessage.Dismiss();
                }
            } catch (Exception e) {
                SwrveLog.LogError("Error while dismissing a message " + e);
            }
        }
#endif
    }

    /// <summary>
    /// Reload the campaigns and user resources from the server.
    /// </summary>
    public virtual void RefreshUserResourcesAndCampaigns()
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady()) {
            return;
        }

        LoadResourcesAndCampaigns();
#endif
    }

    /// <summary>
    /// Identify users such that they can be tracked and targeted safely across multiple devices, platforms and channels.
    /// </summary>
    /// <param name="externalUserId">
    /// An ID that uniquely identifies your user. Personal identifiable information should not be used. An error may be returned if such information is submitted as the userID eg email, phone number etc.
    /// </param>
    /// <param name="onSuccess">
    /// Callback that will handle a success for the Identify call.
    /// </param>
    /// <param name="onError">
    /// Callback that will handle an error for the Identify call.
    /// </param>
    public virtual void Identify(string userId, OnSuccessIdentify onSuccess, OnErrorIdentify onError)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (config.InitMode == SwrveInitMode.MANAGED) {
            SwrveLog.LogInfo("Swrve identify: Cannot call identify api in MANAGED initMode.");
            throw new Exception("Cannot call identify api in MANAGED initMode.");
        }
        if (Initialised) {
            if (string.IsNullOrEmpty(userId)) {
                SwrveLog.LogInfo("Swrve identify: External user id cannot be nil or empty");
                if (onError != null) {
                    onError(-1, "External user id cannot be nil or empty");
                }
                return;
            }

            SwrveLog.LogInfo("Swrve identify: Pausing event queuing and sending prior to Identity API call...");
            this.SendQueuedEvents();
            this.PauseEventSending();

            // Check and try Identify a cached user.
            SwrveUser cachedUser = profileManager.GetSwrveUser(userId);
            if (this.IdentifyCachedUser(cachedUser, onSuccess)) {
                return;
            }

            //internal identify call.
            this.IdentifyUnknownUser(userId, cachedUser, onSuccess, onError);

        } else {
            SwrveLog.LogError("Swrve identify: Failed to call Identify, you did't init the SDK. Please be sure that the SDK is init first");
        }
#endif
    }

    /// <summary>
    /// Start the sdk when in SWRVE_INIT_MODE_MANAGED mode. Can be called multiple times to switch the current userId to something else.
    /// A new session is started if not already started or if is already started with different userId.
    /// </summary>
    /// <param name="userID">
    /// User id to start sdk with.
    /// </param>
    public virtual void Start(String userId = null)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (config.InitMode == SwrveInitMode.AUTO) {
            throw new Exception("You cannot call this method with the InitMode AUTO");
        }

        if (!sdkStarted) {
            sdkStarted = true;
            profileManager.PrepareAndSetUserId(userId);
#if UNITY_ANDROID || UNITY_IPHONE
            UpdateNativeUserId();
#endif
            this.InitUser();
            ProcessInfluenceData();
        } else {
            SwitchUser(userId, false);
        }
#endif
    }

    /// <summary>
    /// @return true when in SWRVE_INIT_MODE_AUTO mode. When in SWRVE_INIT_MODE_MANAGED mode it will return true after one of the 'start' api's has been called.
    /// </summary>
    public virtual bool IsStarted()
    {
#if SWRVE_SUPPORTED_PLATFORM
        return sdkStarted;
#else
        yield return false;
#endif
    }

    /// <summary>
    /// Inform that am embedded message has been served and processed. This function should be called
    /// by your implementation to update the campaign information and send the appropriate data to
    /// Swrve.
    /// </summary>
    /// <param name="message">
    /// Embedded message that has been processed.
    /// </param>
    public virtual void EmbeddedMessageWasShownToUser(SwrveEmbeddedMessage message)
    {
#if SWRVE_SUPPORTED_PLATFORM
        try {
            // The message was shown. Take the current time so that we can throttle messages
            // from being shown too quickly.
            SetMessageMinDelayThrottle();
            this.messagesLeftToShow = this.messagesLeftToShow - 1;

            // Update next for round robin
            SwrveEmbeddedCampaign campaign = (SwrveEmbeddedCampaign)message.Campaign;
            if (campaign != null) {
                campaign.WasShownToUser();
                SaveCampaignData(campaign);
            }
            // Add a custom payload that define that isn't an embedded Message.
            Dictionary<string, string> payload = new Dictionary<string, string>();
            payload.Add("embedded", "true");
            // Send Impression event
            String viewEvent = "Swrve.Messages.Message-" + message.Id + ".impression";
            SwrveLog.Log("Sending view event: " + viewEvent);
            NamedEventInternal(viewEvent, payload, false);
        } catch (Exception e) {
            SwrveLog.LogError("Error while processing message impression " + e);
        }
#endif
    }

    /// <summary>
    /// Process an embedded message engagement event. This function should be called by your
    /// implementation to inform Swrve of a button event.
    ///
    /// </summary>
    /// <param name="message">
    /// Embedded message that has been processed.
    /// </param>
    /// <param name="buttonName">
    /// Button that was pressed.
    /// </param>
    public virtual void EmbeddedMessageButtonWasPressed(SwrveEmbeddedMessage message, string buttonName)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (buttonName != null) {
            try {
                // Button other than dismiss pressed
                String clickEvent = "Swrve.Messages.Message-" + message.Id + ".click";
                SwrveLog.Log("Sending click event: " + clickEvent);
                Dictionary<string, string> clickPayload = new Dictionary<string, string>();
                clickPayload.Add("name", buttonName);
                clickPayload.Add("embedded", "true");
                NamedEventInternal(clickEvent, clickPayload, false);
            } catch (Exception e) {
                SwrveLog.LogError("Error while processing button click " + e);
            }
        }
#endif
    }
}

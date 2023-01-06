#if UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE
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

#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

#if (UNITY_WP8 || UNITY_METRO) && !UNITY_WSA_10_0
#warning "Please note that the Windows build of the Unity SDK is not supported by Swrve and customers use it at their own risk.  It is not covered by any performance warranty otherwise offered by Swrve"
#endif

#if (UNITY_2_6 || UNITY_2_6_1 || UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || !UNITY_2019_4_OR_NEWER)
#error "The Swrve SDK needs to be compiled with Unity 2019.4+"
#endif

/// <summary>
/// Main class implementation of the Swrve SDK.
/// </summary>
public partial class SwrveSDK
{
    public const string SdkVersion = "9.0.1";

    protected int appId;
    /// <summary>
    /// App ID used to initialize the SDK.
    /// </summary>
    public int AppId
    {
        get
        {
            return appId;
        }
    }

    protected string apiKey;
    /// <summary>
    /// Secret API key used to initialize the SDK.
    /// </summary>
    public string ApiKey
    {
        get
        {
            return apiKey;
        }
    }

    /// <summary>
    /// User ID assigned to this device.
    /// </summary>
    public string UserId
    {
        get
        {
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

        if (config == null)
        {
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
        storage.SetSecureFailedListener(delegate ()
        {
            NamedEventInternal("Swrve.signature_invalid", null, false);
        });
        swrveTemporaryPath = GetSwrveTemporaryCachePath();
        this.InitAssetsManager(container, swrveTemporaryPath);

        // Check for migrations.
        SwrveMigrationsManager migrationManager = new SwrveMigrationsManager(storage, profileManager);
        migrationManager.CheckMigrations();

        // Load install date
        string installTimeSecondsFromFile = storage.Load(AppInstallTimeSecondsSave);
        if (string.IsNullOrEmpty(installTimeSecondsFromFile) || string.Equals("0", installTimeSecondsFromFile))
        {
            // check for "0" because of bug between 6.0.0 and 7.3.2
            installTimeSeconds = GetSessionTime();
            storage.Save(AppInstallTimeSecondsSave, installTimeSeconds.ToString());
        }
        else
        {
            long.TryParse(installTimeSecondsFromFile, out installTimeSeconds);
        }
        installTimeSecondsFormatted = SwrveHelper.EpochToFormat(installTimeSeconds, InstallTimeFormat);

        // Check API key
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("The api key has not been specified.");
        }

        // Language
        if (string.IsNullOrEmpty(Language))
        {
            // Get language on the device
            Language = GetDeviceLanguage();
            if (string.IsNullOrEmpty(Language))
            {
                Language = config.DefaultLanguage;
            }
        }

        // End points
        contentServer = string.IsNullOrEmpty(config.ContentServer) ? GetSwrveEndpoint(appId, config.SelectedStack, "content.swrve.com") : config.ContentServer;
        eventsServer = string.IsNullOrEmpty(config.EventsServer) ? GetSwrveEndpoint(appId, config.SelectedStack, "api.swrve.com") : config.EventsServer;
        identityServer = string.IsNullOrEmpty(config.IdentityServer) ? GetSwrveEndpoint(appId, config.SelectedStack, "identity.swrve.com") : config.IdentityServer;

        // Produce Urls
        eventsUrl = eventsServer + "/1/batch";
        identifyUrl = identityServer + "/identify";
        abTestResourcesDiffUrl = contentServer + "/api/1/user_resources_diff";
        resourcesAndCampaignsUrl = contentServer + "/api/1/user_content";

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
        SwrveQaUser.Init(Container, eventsServer, apiKey, appId, UserId, GetAppVersion(), GetDeviceUUID(), storage);

        if (SwrveHelper.IsOnDevice())
        {
            InitNative();
        }

        if (profileManager.GetTrackingState() == SwrveTrackingState.STOPPED)
        {
            SwrveLog.LogInfo("SwrveSDK is currently in stopped state and will not start until an api is called.");
        }
        else
        {
            sdkStarted = ShouldAutoStart();
        }

        Initialised = true;
        if (sdkStarted)
        {
            this.InitUser();
            ProcessInfluenceData();

            // In-app messaging features
            if (config.MessagingEnabled)
            {
                // Check language and link token
                if (string.IsNullOrEmpty(Language))
                {
                    throw new Exception("Language needed to use messaging");
                }
                else if (string.IsNullOrEmpty(config.AppStore))
                {
                    // Default app-store by platform
#if UNITY_ANDROID
                    config.AppStore = SwrveAppStore.Google;
#elif UNITY_IOS
                    config.AppStore = SwrveAppStore.Apple;
#else
                    throw new Exception("App store must be apple, google, amazon or a custom app store");
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
    /// IOS 14.5 requires user permission to get IFDA. Once given, call this method with the IDFA string
    /// </summary>
    /// <param name="idfa">
    /// IDFA string
    /// </param>
    public virtual void SetIDFA(string idfa)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady())
        {
            return;
        }
        idfaString = idfa;
        storage.Save(IDFAString, idfaString);
        SendDeviceInfo();
#endif
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
        if (!IsSDKReady())
        {
            return;
        }

        if (!string.IsNullOrEmpty(name) && !name.ToLower().StartsWith("swrve."))
        {
            NamedEventInternal(name, payload);
        }
        else
        {
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
        if (!IsSDKReady())
        {
            return;
        }

        if (attributes != null && attributes.Count > 0)
        {
            Dictionary<string, object> json = new Dictionary<string, object>();
            json.Add("attributes", attributes);
            AppendEventToBuffer("user", json);
        }
        else
        {
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
        if (!IsSDKReady())
        {
            return;
        }

        if (name != null)
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            var dateUTC = date.Date.ToUniversalTime();
            string dateAttribute = dateUTC.ToString(@"yyyy-MM-ddTHH:mm:ss.fffZ");
            attributes.Add(name, dateAttribute);

            Dictionary<string, object> json = new Dictionary<string, object>();
            json.Add("attributes", attributes);
            AppendEventToBuffer("user", json);
        }
        else
        {
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
        if (!IsSDKReady())
        {
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
        if (!IsSDKReady())
        {
            return;
        }

        if (rewards == null)
        {
            rewards = new IapRewards();
        }

        _Iap(quantity, productId, productPrice, currency, rewards, string.Empty, string.Empty, string.Empty, "unknown");
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
        if (!IsSDKReady())
        {
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
        if (profileManager.GetTrackingState() == SwrveTrackingState.EVENT_SENDING_PAUSED)
        {
            SwrveLog.LogInfo("SDK tracking state is EVENT_SENDING_PAUSED");
            return false;
        }
        bool sentEvents = false;
        if (!eventsConnecting)
        {
            byte[] eventsPostEncodedData = null;
            if (eventsPostString == null || eventsPostString.Length == 0)
            {
                eventsPostString = eventBufferStringBuilder.ToString();
                eventBufferStringBuilder.Length = 0;
            }

            if (eventsPostString.Length > 0)
            {
                long time = GetSessionTime();
                eventsPostEncodedData = PostBodyBuilder.BuildEvent(apiKey, appId, this.UserId, GetDeviceUUID(), GetAppVersion(), time, eventsPostString);
            }

            // eventsConnecting will be true until there is a response or an error from the POST
            if (eventsPostEncodedData != null)
            {
                eventsConnecting = true;
                SwrveLog.Log("Sending events to Swrve");
                Dictionary<string, string> requestHeaders = new Dictionary<string, string> {
                    { @"Content-Type", @"application/json; charset=utf-8" },
                };
                sentEvents = true;
                StartTask("PostEvents_Coroutine", PostEvents_Coroutine(requestHeaders, eventsPostEncodedData));
            }
            else
            {
                eventsPostString = null;
            }
        }
        else
        {
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
        if (!IsSDKReady())
        {
            return;
        }

        if (deeplinkManager == null)
        {
            deeplinkManager = new SwrveDeeplinkManager(Container, this, contentServer);
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
        if (!IsSDKReady())
        {
            return;
        }

        if (deeplinkManager == null)
        {
            deeplinkManager = new SwrveDeeplinkManager(Container, this, contentServer);
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
        if (!IsSDKReady() || profileManager.GetTrackingState() == SwrveTrackingState.EVENT_SENDING_PAUSED)
        {
            return;
        }
        if (Initialised)
        {
            if (userResources != null)
            {
                onResult.Invoke(userResources, userResourcesRaw);
            }
            else
            {
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
        if (!IsSDKReady() || profileManager.GetTrackingState() == SwrveTrackingState.EVENT_SENDING_PAUSED)
        {
            return;
        }
        if (Initialised && !abTestUserResourcesDiffConnecting)
        {
            abTestUserResourcesDiffConnecting = true;
            StringBuilder getRequest = new StringBuilder(abTestResourcesDiffUrl);
            getRequest.AppendFormat("?user={0}&api_key={1}&app_version={2}&joined={3}", SwrveHelper.EscapeURL(this.UserId), apiKey, SwrveHelper.EscapeURL(GetAppVersion()), userInitTimeSeconds);

            SwrveLog.Log("AB Test User Resources Diff request: " + getRequest.ToString());

            StartTask("GetUserResourcesDiff_Coroutine", GetUserResourcesDiff_Coroutine(getRequest.ToString(), onResult, onError, AbTestUserResourcesDiffSave));
        }
        else
        {
            string errorStr = "Failed to initiate A/B test Diff GET request";
            SwrveLog.LogError(errorStr);
            if (onError != null)
            {
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
        if (!IsSDKReady() || profileManager.GetTrackingState() == SwrveTrackingState.EVENT_SENDING_PAUSED)
        {
            return;
        }

        if (Initialised)
        {
            if (realtimeUserProperties != null)
            {
                onResult.Invoke(realtimeUserProperties, realtimeUserPropertiesRaw);
            }
            else
            {
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
        if (profileManager.GetTrackingState() == SwrveTrackingState.EVENT_SENDING_PAUSED)
        {
            SwrveLog.LogInfo("SDK tracking state is EVENT_SENDING_PAUSED");
            return;
        }
        if (Initialised)
        {
            // Concatenate existing events and buffer
            if (eventBufferStringBuilder != null)
            {
                StringBuilder savedEventStringBuilder = new StringBuilder();
                string bufferedEvents = eventBufferStringBuilder.ToString();
                // Clean event buffer to avoid sending twice
                eventBufferStringBuilder.Length = 0;
                // Save events that could be trying to be sent
                if (saveEventsBeingSent)
                {
                    savedEventStringBuilder.Append(eventsPostString);
                    eventsPostString = null;
                    if (bufferedEvents.Length > 0)
                    {
                        if (savedEventStringBuilder.Length != 0)
                        {
                            savedEventStringBuilder.Append(",");
                        }
                        savedEventStringBuilder.Append(bufferedEvents);
                    }
                }
                else
                {
                    savedEventStringBuilder.Append(bufferedEvents);
                }

                // Load old events saved in file
                try
                {
                    string loadedEvents = storage.Load(EventsSave, this.UserId);
                    if (!string.IsNullOrEmpty(loadedEvents))
                    {
                        if (savedEventStringBuilder.Length != 0)
                        {
                            savedEventStringBuilder.Append(",");
                        }
                        savedEventStringBuilder.Append(loadedEvents);
                    }
                }
                catch (Exception e)
                {
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

    protected virtual Dictionary<string, string> GetDeviceInfo()
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
            { "swrve.tracking_state", profileManager.GetTrackingState().ToString() },
            { "swrve.device_type", GetDeviceType() }
        };

        if (string.IsNullOrEmpty(idfaString))
        {
            //check storage
            idfaString = GetIDFA();
        }

        if (!string.IsNullOrEmpty(idfaString))
        {
            deviceInfo["swrve.IDFA"] = idfaString;
        }

        String tzUtcOffsetSeconds = DateTimeOffset.Now.Offset.TotalSeconds.ToString();
        deviceInfo["swrve.utc_offset_seconds"] = tzUtcOffsetSeconds;

        setNativeInfo(deviceInfo);

        // Carrier info
        ICarrierInfo carrierInfo = GetCarrierInfoProvider();
        if (carrierInfo != null)
        {
            string carrierInfoName = carrierInfo.GetName();
            if (!string.IsNullOrEmpty(carrierInfoName))
            {
                deviceInfo["swrve.sim_operator.name"] = carrierInfoName;
            }
            string carrierInfoIsoCountryCode = carrierInfo.GetIsoCountryCode();
            if (!string.IsNullOrEmpty(carrierInfoIsoCountryCode))
            {
                deviceInfo["swrve.sim_operator.iso_country_code"] = carrierInfoIsoCountryCode;
            }
            string carrierInfoCarrierCode = carrierInfo.GetCarrierCode();
            if (!string.IsNullOrEmpty(carrierInfoCarrierCode))
            {
                deviceInfo["swrve.sim_operator.code"] = carrierInfoCarrierCode;
            }
        }

        SwrveInitMode mode = config.InitMode;
        string initModeDeviceInfo = config.InitMode.ToString().ToLower();
        if (config.AutoStartLastUser)
        {
            initModeDeviceInfo += "_auto";
        }
        deviceInfo["swrve.sdk_init_mode"] = initModeDeviceInfo;

        return deviceInfo;
    }

    /// <summary>
    /// Call this function when the app pauses (phone call, move to another app, etc.).
    /// This method is called automatically if using the SwrveComponent.
    /// </summary>
    public virtual void OnSwrvePause()
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady())
        {
            return;
        }

        applicationPaused = true;
        if (Initialised)
        {
            FlushToDisk();
            // Session management
            GenerateNewSessionInterval();

            if (config != null && config.AutoDownloadCampaignsAndResources)
            {
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
        if (Initialised)
        {
            if (!IsSDKReady())
            {
                return;
            }
#if UNITY_IOS || UNITY_ANDROID
            RefreshPushPermissions();
#endif
            LoadFromDisk();
            QueueDeviceInfo();
            // Session management
            long currentTime = GetSessionTime();
            if (currentTime >= lastSessionTick)
            {
                SessionStart();
            }
            else
            {
                SendQueuedEvents();
            }
            GenerateNewSessionInterval();

            StartCampaignsAndResourcesTimer();
            DisableAutoShowAfterDelay();
            ProcessInfluenceData();

            if (IsMessageDisplaying() == false)
            {
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
        if (!Destroyed)
        {
            Destroyed = true;
            if (Initialised)
            {
                FlushToDisk(true);
            }

            if (config != null && config.AutoDownloadCampaignsAndResources)
            {
                StopCheckForCampaignAndResources();
            }
        }
#endif
    }

    protected void QueueMessageClickEvent(SwrveButton button)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (button == null)
        {
            return;
        }

        try
        {
            SwrveLog.Log("Queueing Button Click of type:" + button.ActionType + " Action:" + button.Action + " appId:" + button.AppId);
            String clickEvent = "Swrve.Messages.Message-" + button.Message.Id + ".click";
            SwrveLog.Log("Queueing click event: " + clickEvent);
            Dictionary<string, string> payload = new Dictionary<string, string>();
            payload.Add("name", button.Name);
            payload.Add("embedded", "false");
            if (button.ButtonId > 0)
            {
                payload.Add("buttonId", button.ButtonId.ToString());
            }

            if (currentMessageView != null && currentMessageView.CurrentPageId > 0)
            {
                payload.Add("contextId", currentMessageView.CurrentPageId.ToString());
            }

            if (currentMessageView != null && currentMessageView.Format.Pages.ContainsKey(currentMessageView.CurrentPageId))
            {
                SwrveMessagePage page = currentMessageView.Format.Pages[currentMessageView.CurrentPageId];
                string pageName = page.PageName;
                if (!string.IsNullOrEmpty(pageName))
                {
                    payload.Add("pageName", pageName);
                }
            }

            NamedEventInternal(clickEvent, payload, false);

            string actionType = SwrveActionTypeExtensions.QaActionTypeString(button.ActionType);
            SwrveQaUser.CampaignButtonClicked(button.Message.Campaign.Id, button.Message.Id, button.Name, actionType, button.Action);
        }
        catch (Exception e)
        {
            SwrveLog.LogError("Error while queuing button click event:" + e);
        }
#endif
    }

    private void QueueMessageDismissEvent(SwrveButton button)
    {
        if (button == null || currentMessageView == null)
        {
            return;
        }

        try
        {
            Dictionary<string, object> eventData = new Dictionary<string, object>();
            eventData.Add("campaignType", "iam");
            eventData.Add("actionType", "dismiss");
            eventData.Add("id", button.Message.Id.ToString());
            eventData.Add("contextId", currentMessageView.CurrentPageId.ToString());

            Dictionary<string, string> payload = new Dictionary<string, string>();
            if (button.ButtonId > 0)
            {
                payload.Add("buttonId", button.ButtonId.ToString());
            }

            if (!string.IsNullOrEmpty(button.Name))
            {
                payload.Add("buttonName", button.Name);
            }

            if (currentMessageView != null && currentMessageView.Format.Pages.ContainsKey(currentMessageView.CurrentPageId))
            {
                SwrveMessagePage page = currentMessageView.Format.Pages[currentMessageView.CurrentPageId];
                string pageName = page.PageName;
                if (!string.IsNullOrEmpty(pageName))
                {
                    payload.Add("pageName", pageName);
                }
            }

            eventData.Add("payload", payload);
            AppendEventToBuffer("generic_campaign_event", eventData, false);
            string actionType = SwrveActionTypeExtensions.QaActionTypeString(button.ActionType);
            SwrveQaUser.CampaignButtonClicked(button.Message.Campaign.Id, button.Message.Id, button.Name, actionType, button.Action);
        }
        catch (Exception e)
        {
            SwrveLog.LogError("Error while queuing button dismiss event:" + e);
        }
    }

    private void QueueMessagePageNavEvent(SwrveButton button)
    {
        try
        {
            if (currentMessageView.SentNavigationEvents.Contains(button.ButtonId))
            {
                SwrveLog.Log("Page Nav event already sent for button: " + button.ButtonId);
                return;
            }

            Dictionary<string, object> eventData = new Dictionary<string, object>();
            eventData.Add("campaignType", "iam");
            eventData.Add("actionType", "navigation");
            eventData.Add("id", button.Message.Id.ToString());
            eventData.Add("contextId", currentMessageView.CurrentPageId.ToString());

            // Page nav events will always have buttonId and to, so no need to check for them
            Dictionary<string, string> payload = new Dictionary<string, string>();
            payload.Add("buttonId", button.ButtonId.ToString());
            payload.Add("to", button.Action);

            if (currentMessageView != null && currentMessageView.Format.Pages.ContainsKey(currentMessageView.CurrentPageId))
            {
                SwrveMessagePage page = currentMessageView.Format.Pages[currentMessageView.CurrentPageId];
                string pageName = page.PageName;
                if (!string.IsNullOrEmpty(pageName))
                {
                    payload.Add("pageName", pageName);
                }
            }

            eventData.Add("payload", payload);
            AppendEventToBuffer("generic_campaign_event", eventData, false);
            currentMessageView.SentNavigationEvents.Add(button.ButtonId);
        }
        catch (Exception e)
        {
            SwrveLog.LogError("Error while queuing button page nav event:" + e);
        }
    }

    private void QueueMessagePageViewEvent()
    {
        try
        {
            long currentPageId = currentMessageView.CurrentPageId;
            if (currentPageId == 0 || currentMessageView.SentPageViewEvents.Contains(currentPageId))
            {
                return;
            }

            Dictionary<string, object> eventData = new Dictionary<string, object>();
            eventData.Add("campaignType", "iam");
            eventData.Add("actionType", "page_view");
            eventData.Add("id", currentMessageView.Message.Id.ToString());
            eventData.Add("contextId", currentPageId.ToString());

            Dictionary<string, string> payload = new Dictionary<string, string>();
            if (currentMessageView.Format.Pages.ContainsKey(currentPageId))
            {
                SwrveMessagePage page = currentMessageView.Format.Pages[currentPageId];
                string pageName = page.PageName;
                if (!string.IsNullOrEmpty(pageName))
                {
                    payload.Add("pageName", pageName);
                }
            }

            eventData.Add("payload", payload);
            AppendEventToBuffer("generic_campaign_event", eventData, false);
            currentMessageView.SentPageViewEvents.Add(currentPageId);
        }
        catch (Exception e)
        {
            SwrveLog.LogError("Error while queuing page view event:" + e);
        }
    }

    protected virtual void MessageWasShownToUser(SwrveMessageFormat messageFormat)
    {
#if SWRVE_SUPPORTED_PLATFORM
        try
        {
            // The message was shown. Take the current time so that we can throttle messages
            // from being shown too quickly.
            SetMessageMinDelayThrottle();
            this.messagesLeftToShow = this.messagesLeftToShow - 1;

            // Update next for round robin
            SwrveInAppCampaign campaign = (SwrveInAppCampaign)messageFormat.Message.Campaign;
            if (campaign != null)
            {
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
        }
        catch (Exception e)
        {
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
        return (currentMessageView != null);
#else
        return false;
#endif
    }

    protected virtual SwrveBaseMessage GetBaseMessageForEvent(string eventName, IDictionary<string, string> eventPayload = null)
    {
        if (!IsSDKReady())
        {
            return null;
        }

        if (!checkGlobalRules(eventName, eventPayload, SwrveHelper.GetNow()))
        {
            return null;
        }

        try
        {
            return _getBaseMessageForEvent(eventName, eventPayload);
        }
        catch (Exception e)
        {
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

        // Get Personalization Properties in case they are required for IAMs
        Dictionary<string, string> personalizationProperties = GetPersonalizationProperties(eventPayload);

        List<SwrveQaUserCampaignInfo> qaCampaignInfoList = new List<SwrveQaUserCampaignInfo>();
        while (itCampaign.MoveNext() && result == null)
        {
            if (!(itCampaign.Current is SwrveInAppCampaign || itCampaign.Current is SwrveEmbeddedCampaign))
            {
                continue;
            }

            SwrveBaseCampaign nextCampaign = itCampaign.Current;
            SwrveBaseMessage nextMessage = null;
            if (nextCampaign is SwrveEmbeddedCampaign)
            {
                nextMessage = ((SwrveEmbeddedCampaign)nextCampaign).GetMessageForEvent(eventName, eventPayload, qaCampaignInfoList);
            }
            else if (nextCampaign is SwrveInAppCampaign)
            {
                nextMessage = ((SwrveInAppCampaign)nextCampaign).GetMessageForEvent(eventName, eventPayload, qaCampaignInfoList, personalizationProperties);
            }

            // Check if the message supports the current orientation
            if (nextMessage != null)
            {
                if (nextMessage.SupportsOrientation(deviceOrientation))
                {
                    availableMessages.Add(nextMessage);
                    if (nextMessage.Priority <= minPriority)
                    {
                        if (nextMessage.Priority < minPriority)
                        {
                            // If it is lower than any of the previous ones
                            // remove those from being candidates
                            candidateMessages.Clear();
                        }
                        minPriority = nextMessage.Priority;
                        candidateMessages.Add(nextMessage);
                    }
                }
                else
                {
                    string reason = "Message didn't support the current device orientation: " + deviceOrientation;
                    SwrveQaUserCampaignInfo campaignInfo = new SwrveQaUserCampaignInfo(nextCampaign.Id, nextMessage.Id, nextCampaign.GetCampaignType(), false, reason);
                    qaCampaignInfoList.Add(campaignInfo);
                }
            }
        }

        // Select randomly from the highest messages
        if (candidateMessages.Count > 0)
        {
            candidateMessages.Shuffle();
            result = candidateMessages[0];
            campaign = result.Campaign;
        }

        if (SwrveQaUser.Instance.loggingEnabled && campaign != null && result != null)
        {
            // A message was chosen, check if other campaigns would have returned a message
            IEnumerator<SwrveBaseMessage> itOtherMessage = availableMessages.GetEnumerator();
            while (itOtherMessage.MoveNext())
            {
                SwrveBaseCampaign otherCampaign = itOtherMessage.Current.Campaign;
                if (otherCampaign != result.Campaign)
                {
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

    protected virtual SwrveConversation GetConversationForEvent(string eventName, IDictionary<string, string> eventPayload = null)
    {
        if (!IsSDKReady())
        {
            return null;
        }

        if (!checkGlobalRules(eventName, eventPayload, SwrveHelper.GetNow()))
        {
            return null;
        }

        try
        {
            return _getConversationForEvent(eventName, eventPayload);
        }
        catch (Exception e)
        {
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
        while (itCampaign.MoveNext() && result == null)
        {
            if (!(itCampaign.Current is SwrveConversationCampaign))
            {
                continue;
            }

            SwrveConversationCampaign nextCampaign = (SwrveConversationCampaign)itCampaign.Current;
            SwrveConversation nextConversation = nextCampaign.GetConversationForEvent(eventName, eventPayload, qaCampaignInfoList);
            // Check if the message supports the current orientation
            if (nextConversation != null)
            {
                availableConversations.Add(nextConversation);
                if (nextConversation.Priority <= minPriority)
                {
                    if (nextConversation.Priority < minPriority)
                    {
                        // If it is lower than any of the previous ones
                        // remove those from being candidates
                        candidateConversations.Clear();
                    }
                    minPriority = nextConversation.Priority;
                    candidateConversations.Add(nextConversation);
                }
            }
        }
        if (candidateConversations.Count > 0)
        {
            candidateConversations.Shuffle(); // Select randomly
            result = candidateConversations[0];
            campaign = result.Campaign;
        }

        if (SwrveQaUser.Instance.loggingEnabled && campaign != null && result != null)
        {
            // A message was chosen, check if other campaigns would have returned a message
            IEnumerator<SwrveConversation> itOtherMessage = availableConversations.GetEnumerator();
            while (itOtherMessage.MoveNext())
            {
                SwrveBaseCampaign otherCampaign = itOtherMessage.Current.Campaign;
                if (otherCampaign != result.Campaign)
                {
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
        if ((campaigns == null) || (campaigns.Count == 0))
        {
            NoMessagesWereShown(eventName, eventPayload, "No campaigns available");
            return false;
        }

        if (!string.Equals(eventName, DefaultAutoShowMessagesTrigger, StringComparison.OrdinalIgnoreCase) && IsTooSoonToShowMessageAfterLaunch(now))
        {
            NoMessagesWereShown(eventName, eventPayload, "{App throttle limit} Too soon after launch. Wait until " + showMessagesAfterLaunch.ToString(WaitTimeFormat));
            return false;
        }

        if (IsTooSoonToShowMessageAfterDelay(now))
        {
            NoMessagesWereShown(eventName, eventPayload, "{App throttle limit} Too soon after last base message. Wait until " + showMessagesAfterDelay.ToString(WaitTimeFormat));
            return false;
        }

        if (HasShowTooManyMessagesAlready())
        {
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
        if (!IsSDKReady() || campaign == null)
        {
            return;
        }

        if (!orientation.HasValue)
        {
            orientation = GetDeviceOrientation();
        }
        Dictionary<string, string> personalizationProperties = IncludeRealTimeUserProperties(properties);
        ShowCampaign(campaign, false, orientation.Value, personalizationProperties);
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
        if (!IsSDKReady())
        {
            return null;
        }

        if (!orientation.HasValue)
        {
            orientation = GetDeviceOrientation();
        }

        List<SwrveBaseCampaign> result = new List<SwrveBaseCampaign>();
        IEnumerator<SwrveBaseCampaign> itCampaign = campaigns.GetEnumerator();
        Dictionary<string, string> personalizationProperties = IncludeRealTimeUserProperties(properties);
        while (itCampaign.MoveNext())
        {
            SwrveBaseCampaign campaign = itCampaign.Current;
            if (IsValidMessageCenter(campaign, orientation.Value, personalizationProperties))
            {
                bool add = true;
                SwrveMessageTextTemplatingResolver resolver = new SwrveMessageTextTemplatingResolver();
                if (properties != null && (campaign is SwrveInAppCampaign))
                {
                    add = resolver.ResolveTemplating((SwrveInAppCampaign)campaign, personalizationProperties);
                }
                if (add)
                {
                    if (campaign is SwrveInAppCampaign)
                    {
                        SwrveInAppCampaign swrveInAppCampaign = (SwrveInAppCampaign)campaign;
                        campaign.MessageCenterDetails = resolver.ResolveMessageCenterDetails(swrveInAppCampaign.Message.MessageCenterDetails, personalizationProperties);
                        if (campaign.MessageCenterDetails != null)
                        {
                            LoadMessageCenterAssetsFromCache(campaign.MessageCenterDetails);
                        }
                    }

                    result.Add(campaign);
                }
            }
        }
        return result;
    }

    private void LoadMessageCenterAssetsFromCache(SwrveMessageCenterDetails messageCenterDetails)
    {
        if (messageCenterDetails == null)
        {
            return;
        }
        string imageUrl = messageCenterDetails.ImageUrl;
        string imageSha = messageCenterDetails.ImageSha;
        if (imageUrl != null)
        {
            //create sha afer personalizeText
            byte[] externalAssetBytes = System.Text.Encoding.UTF8.GetBytes(imageUrl);
            string externalAssetSha1 = SwrveHelper.sha1(externalAssetBytes);

            string filePath = Path.Combine(swrveTemporaryPath, externalAssetSha1);
            if (CrossPlatformFile.Exists(filePath))
            {
                messageCenterDetails.Image = LoadMessageCenterTextureFromPath(filePath);
            }
        }

        if (messageCenterDetails.Image == null && imageSha != null)
        {
            // try load the cdn image asset, this is also used as a fallback, when the imageUrl above failed to download and wasn't in cache.
            string filePath = Path.Combine(swrveTemporaryPath, imageSha);
            if (CrossPlatformFile.Exists(filePath))
            {
                byte[] byteArray = CrossPlatformFile.ReadAllBytes(filePath);
                messageCenterDetails.Image = LoadMessageCenterTextureFromPath(filePath);
            }
        }
    }

    private Texture2D LoadMessageCenterTextureFromPath(String filePath)
    {
        byte[] byteArray = CrossPlatformFile.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(byteArray))
        {
            return texture;
        }
        else
        {
            SwrveLog.LogWarning("Could not load message center asset from cache: " + filePath);
            return null;
        }
    }

    /// <summary>
    /// Remove this campaign. It won't be returned anymore by the 'GetMessageCenterCampaigns' methods.
    /// </summary>
    /// <param name="campaign">
    /// The campaign to remove.
    /// </param>
    public virtual void RemoveMessageCenterCampaign(SwrveBaseCampaign campaign)
    {
        if (campaign != null)
        {
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
        if (campaign != null)
        {
            campaign.Status = SwrveCampaignState.Status.Seen;
            SaveCampaignData(campaign);
        }
    }

    protected virtual IEnumerator ShowMessageForEvent(string eventName, SwrveBaseMessage message, ISwrveCustomButtonListener customButtonListener = null, ISwrveMessageListener messageListener = null, ISwrveClipboardButtonListener clipboardButtonListener = null, ISwrveEmbeddedMessageListener embeddedMessageListener = null)
    {
        return ShowMessageForEvent(eventName, null, message, customButtonListener, messageListener, clipboardButtonListener, embeddedMessageListener);
    }

    protected virtual IEnumerator ShowMessageForEvent(string eventName, IDictionary<string, string> payload, SwrveBaseMessage message, ISwrveCustomButtonListener customButtonListener = null, ISwrveMessageListener messageListener = null, ISwrveClipboardButtonListener clipboardButtonListener = null, ISwrveEmbeddedMessageListener embeddedMessageListener = null)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady())
        {
            yield return null;
        }

        if (message != null)
        {
            Dictionary<string, string> properties = GetPersonalizationProperties(payload);
            if (message is SwrveMessage)
            {
                if (currentMessageView == null)
                {
                    yield return Container.StartCoroutine(LaunchMessage(message, properties));
                }
            }
            else if (message is SwrveEmbeddedMessage)
            {
                if (embeddedMessageListener != null)
                {
                    embeddedMessageListener.OnMessage((SwrveEmbeddedMessage)message, properties);
                }
            }
        }

        TaskFinished("ShowMessageForEvent");
#else
        yield return null;
#endif
    }

    protected virtual IEnumerator ShowConversationForEvent(string eventName, SwrveConversation conversation)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady())
        {
            yield return null;
        }

        yield return Container.StartCoroutine(LaunchConversation(conversation));
        TaskFinished("ShowConversationForEvent");
#else
        yield return null;
#endif
    }

    /// <summary>
    /// Dismisses the current message if any is being displayed.
    /// </summary>
    public virtual void DismissMessage()
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady())
        {
            return;
        }

        try
        {
            if (currentMessageView != null)
            {
                SetMessageMinDelayThrottle();
                currentMessageView.Dismiss();
            }
        }
        catch (Exception e)
        {
            SwrveLog.LogError("Error while dismissing a message " + e);
        }
#endif
    }

    /// <summary>
    /// Reload the campaigns and user resources from the server.
    /// </summary>
    public virtual void RefreshUserResourcesAndCampaigns()
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady())
        {
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
        if (config.InitMode == SwrveInitMode.MANAGED)
        {
            SwrveLog.LogInfo("Swrve identify: Cannot call identify api in MANAGED initMode.");
            throw new Exception("Cannot call identify api in MANAGED initMode.");
        }
        if (Initialised)
        {
            if (string.IsNullOrEmpty(userId))
            {
                SwrveLog.LogInfo("Swrve identify: External user id cannot be nil or empty");
                if (onError != null)
                {
                    onError(-1, "External user id cannot be nil or empty");
                }
                return;
            }

            SwrveLog.LogInfo("Swrve identify: Pausing event queuing and sending prior to Identity API call...");
            this.SendQueuedEvents();
            this.PauseEventSending();

            // Check and try Identify a cached user.
            SwrveUser cachedUser = profileManager.GetSwrveUser(userId);
            if (this.IdentifyCachedUser(cachedUser, onSuccess))
            {
                return;
            }

            //internal identify call.
            this.IdentifyUnknownUser(userId, cachedUser, onSuccess, onError);

        }
        else
        {
            SwrveLog.LogError("Swrve identify: Failed to call Identify, you did't init the SDK. Please be sure that the SDK is init first");
        }
#endif
    }

    /// <summary>
    /// Start the sdk if stopped or autostartlastuser is false.
    /// Tracking will begin using the last user or an auto generated userId if the first time the sdk is started.
    /// </summary>
    public virtual void Start()
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!sdkStarted)
        {
            profileManager.InitUserId();
#if UNITY_ANDROID || UNITY_IOS
            UpdateNativeUserId();
#endif
            this.InitUser();
            ProcessInfluenceData();
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
    public virtual void Start(String userId)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (config.InitMode == SwrveInitMode.AUTO)
        {
            throw new Exception("You cannot call this method with the InitMode AUTO");
        }

        if (!sdkStarted)
        {
            profileManager.InitUserId(userId);
#if UNITY_ANDROID || UNITY_IOS
            UpdateNativeUserId();
#endif
            this.InitUser();
            ProcessInfluenceData();
        }
        else
        {
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
    /// Stop the SDK from tracking. The sdk will remain stopped until a start api is called.
    /// </summary>
    public virtual void StopTracking()
    {
        sdkStarted = false;
        profileManager.SetTrackingState(SwrveTrackingState.STOPPED);
#if UNITY_ANDROID || UNITY_IOS
        UpdateTrackingStateStopped(true);
#endif

        QueueDeviceInfo();
        SendQueuedEvents();

        StopCheckForCampaignAndResources();

#if UNITY_ANDROID || UNITY_IOS
        ClearAllAuthenticatedNotifications();
#endif

        try
        {
            if (currentMessageView != null)
            {
                currentMessageView.Dismiss();
            }
        }
        catch (Exception e)
        {
            SwrveLog.LogError("Error while dismissing a message after StopTracking" + e);
        }
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
        try
        {
            // The message was shown. Take the current time so that we can throttle messages
            // from being shown too quickly.
            SetMessageMinDelayThrottle();
            this.messagesLeftToShow = this.messagesLeftToShow - 1;

            // Update next for round robin
            SwrveEmbeddedCampaign campaign = (SwrveEmbeddedCampaign)message.Campaign;
            if (campaign != null)
            {
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
        }
        catch (Exception e)
        {
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
        if (buttonName != null)
        {
            try
            {
                // Button other than dismiss pressed
                String clickEvent = "Swrve.Messages.Message-" + message.Id + ".click";
                SwrveLog.Log("Sending click event: " + clickEvent);
                Dictionary<string, string> clickPayload = new Dictionary<string, string>();
                clickPayload.Add("name", buttonName);
                clickPayload.Add("embedded", "true");
                NamedEventInternal(clickEvent, clickPayload, false);
            }
            catch (Exception e)
            {
                SwrveLog.LogError("Error while processing button click " + e);
            }
        }
#endif
    }

    /// <summary>
    /// Resolved the personalization for an embedded message's data. This function will make the best attempt
    /// to resolve personalization for your campaign and return the data resolved.
    /// </summary>
    /// <param name="message">
    /// The string that Swrve's text templating will be applied to resulting the keys being resolve in string.
    /// </param>
    /// <param name="personalizationProperties">
    /// Personalization Properties that are required to resolve the Message.data
    /// </param>
    /// <returns>
    /// Return a string value if it could resolve personalization on Message.data. returns null if it could not.
    /// </returns>
    public virtual string GetPersonalizedEmbeddedMessageData(SwrveEmbeddedMessage message, Dictionary<string, string> personalizationProperties)
    {
#if SWRVE_SUPPORTED_PLATFORM
        if (!IsSDKReady())
        {
            return null;
        }

        try
        {
            if (message.type == SwrveEmbeddedMessage.SwrveEmbeddedCampaignType.JSON)
            {
                return SwrveTextTemplating.ApplyToJSON(message.data, personalizationProperties);
            }
            else
            {
                return SwrveTextTemplating.Apply(message.data, personalizationProperties);
            }

        }
        catch (SwrveSDKTextTemplatingException exception)
        {
            SwrveEmbeddedCampaign campaign = (SwrveEmbeddedCampaign)message.Campaign;
            SwrveQaUser.EmbeddedPersonalizationFailed(campaign.Id, message.Id, message.data, "Failed to resolve personalization");
            SwrveLog.LogWarning("Could not resolve personalization for campaign: " + campaign.Id + ".  Exception:" + exception.ToString());
        }
#endif
        return null;
    }

    /// <summary>
    /// Make the best attempt at applying personalization to a string value
    /// </summary>
    /// <param name="text">
    /// The string that Swrve's text templating will be applied to.
    /// </param>
    /// <param name="personalizationProperties">
    /// The properties used for text templating.
    /// </param>
    /// <returns>
    /// Return a string value if it could resolve personalization, null if it could not.
    /// </returns>
    public virtual string GetPersonalizedText(string text, Dictionary<string, string> personalizationProperties)
    {
#if SWRVE_SUPPORTED_PLATFORM
        try
        {
            return SwrveTextTemplating.Apply(text, personalizationProperties);
        }
        catch (SwrveSDKTextTemplatingException exception)
        {
            SwrveLog.LogWarning("Could not resolve personalization: " + exception.ToString());
        }
#endif
        return null;
    }
}

using UnityEngine;
using System;
using System.Collections;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using Swrve;
using SwrveMiniJSON;
using Swrve.Messaging;
using Swrve.Helpers;
using Swrve.ResourceManager;
using Swrve.Device;

#if UNITY_IPHONE
using System.Runtime.InteropServices;
#endif

#if UNITY_WP8 || UNITY_METRO
#error "Please note that the Windows build of the Unity SDK is not supported by Swrve and customers use it at their own risk."
+ "It is not covered by any performance warranty otherwise offered by Swrve"
#endif

#if (UNITY_2_6 || UNITY_2_6_1 || UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5)
#error "The Swrve SDK needs to be compiled with Unity 4.0.0+"
#endif

/// <summary>
/// Main class implementation of the Swrve SDK.
/// </summary>
/// <remarks>
/// </remarks>
public partial class SwrveSDK
{
    public const String SdkVersion = "3.3.1";

#if UNITY_IPHONE
    [DllImport ("__Internal")]
    private static extern string _swrveiOSGetLanguage();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSGetTimeZone();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSGetAppVersion();

    [DllImport ("__Internal")]
    private static extern void _swrveRegisterForPushNotifications();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSUUID();
#endif

    private int gameId;
    /// <summary>
    /// Game ID used to initialize the SDK.
    /// </summary>
    public int GameId
    {
        get {
            return gameId;
        }
    }

    private string apiKey;
    /// <summary>
    /// Secret API key used to initialize the SDK.
    /// </summary>
    public string ApiKey
    {
        get {
            return apiKey;
        }
    }

    protected string userId;
    /// <summary>
    /// User ID assigned to this device.
    /// </summary>
    public string UserId
    {
        get {
            return userId;
        }
    }

    protected SwrveConfig config;

    /// <summary>
    /// Current language of the game or device.
    /// </summary>
    public string Language;

    /// <summary>
    /// Use this object to obtain the latest resources
    /// and values for this user.
    /// </summary>
    public SwrveResourceManager ResourceManager;

    /// <summary>
    /// Container MonoBehaviour object in the scene. Used
    /// to call coroutines and other Unity3D specific
    /// methods.
    /// </summary>
    public MonoBehaviour Container;

    /// <summary>
    /// Install button listener for all in-app messages.
    /// </summary>
    public ISwrveInstallButtonListener GlobalInstallButtonListener = null;

    /// <summary>
    /// Custom button listener for all in-app messages.
    /// </summary>
    public ISwrveCustomButtonListener GlobalCustomButtonListener = null;

    /// <summary>
    /// Global in-app message listener.
    /// </summary>
    public ISwrveMessageListener GlobalMessageListener = null;

    /// <summary>
    /// Listener for push notifications received in the app.
    /// </summary>
    public ISwrvePushNotificationListener PushNotificationListener = null;

    /// <summary>
    /// Disable default renderer and manage messages manually.
    /// </summary>
    public ISwrveTriggeredMessageListener TriggeredMessageListener = null;

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

    /// <summary>
    /// Flag to indicate that the SDK was initialised.
    /// </summary>
    public bool Initialised = false;

    /// <summary>
    /// Flag to indicate that the SDK was destroyed.
    /// </summary>
    public bool Destroyed = false;

    /// <summary>
    /// Initialise the SDK with the given game id, api key.
    /// </summary>
    /// <param name="container">
    /// MonoBehaviour objecto to act as a container.
    /// </param>
    /// <param name="gameId">
    /// Game ID for your game, as provided by Swrve.
    /// </param>
    /// <param name="apiKey">
    /// API key for your game, as provided by Swrve.
    /// </param>
    public void Init (MonoBehaviour container, int gameId, string apiKey)
    {
        Init (container, gameId, apiKey, new SwrveConfig());
    }

    /// <summary>
    /// Initialise the SDK with the given game id, api key and user id.
    /// </summary>
    /// <param name="container">
    /// MonoBehaviour objecto to act as a container.
    /// </param>
    /// <param name="gameId">
    /// Game ID for your game, as provided by Swrve.
    /// </param>
    /// <param name="apiKey">
    /// API key for your game, as provided by Swrve.
    /// </param>
    /// <param name="userId">
    /// Custom unique identifier for this user.
    /// </param>
    public void Init (MonoBehaviour container, int gameId, string apiKey, string userId)
    {
        SwrveConfig config = new SwrveConfig();
        config.UserId = userId;
        Init (container, gameId, apiKey, config);
    }

    /// <summary>
    /// Initialise the SDK with the given game id, api key, user id and config.
    /// </summary>
    /// <param name="container">
    /// MonoBehaviour objecto to act as a container.
    /// </param>
    /// <param name="gameId">
    /// Game ID for your game, as provided by Swrve.
    /// </param>
    /// <param name="apiKey">
    /// API key for your game, as provided by Swrve.
    /// </param>
    /// <param name="userId">
    /// Custom unique identifier for this user.
    /// </param>
    /// <param name="config">
    /// Configuration object with advanced settings.
    /// </param>
    public virtual void Init (MonoBehaviour container, int gameId, string apiKey, string userId, SwrveConfig config)
    {
        config.UserId = userId;
        Init (container, gameId, apiKey, config);
    }

    /// <summary>
    /// Initialise the SDK with the given game id, api key and config.
    /// </summary>
    /// <param name="container">
    /// MonoBehaviour objecto to act as a container.
    /// </param>
    /// <param name="gameId">
    /// Game ID for your game, as provided by Swrve.
    /// </param>
    /// <param name="apiKey">
    /// API key for your game, as provided by Swrve.
    /// </param>
    /// <param name="config">
    /// Configuration object with advanced settings.
    /// </param>
    public virtual void Init (MonoBehaviour container, int gameId, string apiKey, SwrveConfig config)
    {
        this.lastSessionTick = GetSessionTime ();
        // Save init time
        initialisedTime = SwrveHelper.GetNow();
        this.config = config;
        this.Container = container;
        this.gameId = gameId;
        this.apiKey = apiKey;
        this.userId = config.UserId;
        this.Language = config.Language;
        this.campaignsAndResourcesInitialized = false;
        this.autoShowMessagesEnabled = true;
        this.assetsOnDisk = new HashSet<string> ();
        this.assetsCurrentlyDownloading = false;

        // Check API key
        if (string.IsNullOrEmpty(apiKey)) {
            throw new Exception("The api key has not been specified.");
        }

        // Check user id
        if (string.IsNullOrEmpty(userId)) {
            // Use the Swrve user id when empty
            this.userId = GetDeviceUniqueId();
        }
        // Save to disk if we have a valid unique identifier
        if (!string.IsNullOrEmpty (this.userId)) {
            // Save to preferences
            PlayerPrefs.SetString (DeviceIdKey, this.userId);
            PlayerPrefs.Save ();
        }
        SwrveLog.Log("Your user id is: " + this.userId);
        this.escapedUserId = WWW.EscapeURL (this.userId);

        // Language
        if (string.IsNullOrEmpty(Language)) {
            // Get language on the device
            Language = GetDeviceLanguage();
            if (string.IsNullOrEmpty(Language)) {
                Language = config.DefaultLanguage;
            }
        }

        // End points
        config.CalculateEndpoints(gameId);
        string abTestServer = config.ContentServer;
        eventsUrl = config.EventsServer + "/1/batch";
        abTestResourcesDiffUrl = abTestServer + "/api/1/user_resources_diff";
        resourcesAndCampaignsUrl = abTestServer + "/api/1/user_resources_and_campaigns";

        // Storage path
        swrvePath = GetSwrvePath();

        // Storage
        if (storage == null) {
            storage = CreateStorage ();
        }
        storage.SetSecureFailedListener(delegate() {
            NamedEventInternal ("Swrve.signature_invalid", null, false);
        });

        // REST Client
        restClient = CreateRestClient ();

        // Events buffers
        eventBufferStringBuilder = new StringBuilder (config.MaxBufferChars);
        string installTimeEpoch = GetSavedInstallTimeEpoch();

        // Load stored data
        LoadData ();
        InitUserResources();

        // Get device info
        deviceCarrierInfo = new DeviceCarrierInfo();
        GetDeviceScreenInfo ();

        Initialised = true;

        if (config.AutomaticSessionManagement) {
            // Start a new session (will send events)
            QueueSessionStart ();
            GenerateNewSessionInterval ();
        }

        if (string.IsNullOrEmpty(installTimeEpoch)) {
            // Its a new user
            NamedEvent("Swrve.first_session");
        }

#if UNITY_ANDROID
        // Ask for Android registration id
        if (config.PushNotificationEnabled && !string.IsNullOrEmpty(config.GCMSenderId)) {
            GooglePlayRegisterForPushNotification(Container, config.GCMSenderId);
        }
#endif
        QueueDeviceInfo ();

        // Send initial events
        SendQueuedEvents();

        // In-app messaging features
        if (config.TalkEnabled) {
            // Check language and link token
            if (string.IsNullOrEmpty(Language)) {
                throw new Exception ("Language needed to use Talk");
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

            try {
                swrveTemporaryPath = GetSwrveTemporaryCachePath();
                LoadTalkData ();

#if UNITY_IPHONE
                // If we had a device token, keep asking for a new one
                if (config.PushNotificationEnabled && !string.IsNullOrEmpty(iOSdeviceToken)) {
                    RegisterForPushNotificationsIOS();
                }
#endif
            } catch (Exception e) {
                SwrveLog.LogError ("Error while initializing " + e);
            }
        }

        StartCampaignsAndResourcesTimer();
        DisableAutoShowAfterDelay();
    }

    #region Public Methods
    /// <summary>
    /// Buffer the event of the start of a play session.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "session_start" event.
    /// </remarks>
    public void SessionStart ()
    {
        QueueSessionStart();
        SendQueuedEvents ();
    }

    /// <summary>
    /// Buffer the event of the end of a play session.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "session_end" event.
    /// </remarks>
    public void SessionEnd ()
    {
        Dictionary<string,object> json = new Dictionary<string, object> ();
        AppendEventToBuffer ("session_end", json);
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
    public virtual void NamedEvent (string name, Dictionary<string, string> payload = null)
    {
        NamedEventInternal (name, payload);
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
    public void UserUpdate (Dictionary<string, string> attributes)
    {
        if (attributes != null && attributes.Count > 0) {
            Dictionary<string,object> json = new Dictionary<string, object> ();
            json.Add ("attributes", attributes);
            AppendEventToBuffer ("user", json);
        } else {
            SwrveLog.LogError ("Invoked user update with no update attributes");
        }
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
    public void Purchase (string item, string currency, int cost, int quantity)
    {
        Dictionary<string,object> json = new Dictionary<string, object> ();
        json.Add ("item", item);
        json.Add ("currency", currency);
        json.Add ("cost", cost);
        json.Add ("quantity", quantity);
        AppendEventToBuffer ("purchase", json);
    }

    /// <summary>
    /// Buffer the event of a purchase using real currency, where a single item
    /// (that isn't an in-app currency) was purchased.
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
    public void Iap (int quantity, string productId, double productPrice, string currency)
    {
        IapRewards no_rewards = new IapRewards();
        Iap (quantity, productId, productPrice, currency, no_rewards);
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
    public void Iap (int quantity, string productId, double productPrice, string currency, IapRewards rewards)
    {
        _Iap (quantity, productId, productPrice, currency, rewards, string.Empty, string.Empty, string.Empty, "unknown_store");
    }

#if UNITY_IPHONE
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
    /// </param>
    public void IapApple (int quantity, string productId, double productPrice, string currency, string receipt)
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
    /// </param>
    /// <param name="transactionId">
    /// The transaction id identifying the purchase iOS7+ (see SKPaymentTransaction::transactionIdentifier).
    /// </param>
    public void IapApple (int quantity, string productId, double productPrice, string currency, string receipt, string transactionId)
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
    /// </param>
    public void IapApple (int quantity, string productId, double productPrice, string currency, IapRewards rewards, string receipt)
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
    /// </param>
    /// <param name="transactionId">
    /// The transaction id identifying the purchase iOS7+ (see SKPaymentTransaction::transactionIdentifier).
    /// </param>
    public void IapApple (int quantity, string productId, double productPrice, string currency, IapRewards rewards, string receipt, string transactionId)
    {
        if (config.AppStore != "apple") {
            throw new Exception("This function can only be called to validate IAP events from Apple");
        } else {
            if (String.IsNullOrEmpty(receipt)) {
                SwrveLog.LogError("IAP event not sent: receipt cannot be empty for Apple Store verification");
                return;
            }
            _Iap (quantity, productId, productPrice, currency, rewards, receipt, string.Empty, transactionId, config.AppStore);
        }
    }
#endif

#if UNITY_ANDROID
    /// <summary>
    /// Buffer the event of a purchase using real currency, where a single item
    /// (that isn't an in-app currency) was purchased.
    /// The receipt provided will be validated against the Google Play Store.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "iap" event.
    /// Note that this method is currently only supported for the Google Play Store,
    /// and a valid receipt and signature need to be provided for verification.
    /// </remarks>
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
    /// <param name="purchaseData">
    /// The receipt sent back from the Google Play Store upon successful purchase - this receipt will be verified by Swrve
    /// </param>
    /// <param name="dataSignature">
    /// The receipt signature sent back from the Google Play Store upon successful purchase
    /// </param>
    public void IapGooglePlay (string productId, double productPrice, string currency, string purchaseData, string dataSignature)
    {
        IapRewards no_rewards = new IapRewards();
        IapGooglePlay (productId, productPrice, currency, no_rewards, purchaseData, dataSignature);
    }

    /// <summary>
    /// Buffer the event of a purchase using real currency, where any in-app
    /// currencies were purchased, or where multiple items were purchased as part of a bundle.
    /// The receipt provided will be validated against the Google Play Store.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "iap" event.
    /// Note that this method is currently only supported for the Google Play Store,
    /// and a valid receipt and signature need to be provided for verification.
    /// </remarks>
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
    /// <param name="purchaseData">
    /// The receipt sent back from the Google Play Store upon successful purchase - this receipt will be verified by Swrve
    /// </param>
    /// <param name="dataSignature">
    /// The receipt signature sent back from the Google Play Store upon successful purchase
    /// </param>
    public void IapGooglePlay (string productId, double productPrice, string currency, IapRewards rewards, string purchaseData, string dataSignature)
    {
        if (config.AppStore != "google") {
            throw new Exception("This function can only be called to validate IAP events from Google");
        } else {
            if (String.IsNullOrEmpty(purchaseData)) {
                SwrveLog.LogError("IAP event not sent: purchase data cannot be empty for Google Play Store verification");
                return;
            }
            if (String.IsNullOrEmpty(dataSignature)) {
                SwrveLog.LogError("IAP event not sent: data signature cannot be empty for Google Play Store verification");
                return;
            }
            // Google IAP is always of quantity 1
            _Iap (1, productId, productPrice, currency, rewards, purchaseData, dataSignature, string.Empty, config.AppStore);
        }
    }
#endif

    /// <summary>
    /// Buffer the event of a gift of in-game currency.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "currency_given" event.
    /// </remarks>
    /// <param name="givenCurrency">
    /// The name of the in-game currency that the player was rewarded with.
    /// </param>
    /// <param name="amount">
    /// The amount of in-game currency that the player was rewarded with.
    /// </param>
    public void CurrencyGiven (string givenCurrency, double amount)
    {
        Dictionary<string, object> json = new Dictionary<string, object> ();
        json.Add ("given_currency", givenCurrency);
        json.Add ("given_amount", amount);
        AppendEventToBuffer ("currency_given", json);
    }

    /// <summary>
    /// Send buffered events to the server.
    /// </summary>
    public bool SendQueuedEvents ()
    {
        bool sentEvents = false;
        if (Initialised) {
            if (!eventsConnecting) {
                byte[] eventsPostEncodedData = null;
                if (eventsPostString == null || eventsPostString.Length == 0) {
                    eventsPostString = eventBufferStringBuilder.ToString ();
                    eventBufferStringBuilder.Length = 0;
                }

                if (eventsPostString.Length > 0) {
                    long time = SwrveHelper.GetSeconds ();
                    eventsPostEncodedData = PostBodyBuilder.Build (apiKey, gameId, userId, GetDeviceId(),
                                            GetAppVersion(), time, eventsPostString);
                }

                // eventsConnecting will be true until there is a response or an error from the POST
                if (eventsPostEncodedData != null) {
                    eventsConnecting = true;
                    SwrveLog.Log("Sending events to Swrve");
                    Dictionary<string, string> requestHeaders = new Dictionary<string, string> {
                        { @"Content-Type", @"application/json; charset=utf-8" },
                        { @"Content-Length", eventsPostEncodedData.Length.ToString () }
                    };
                    sentEvents = true;
                    StartTask ("PostEvents_Coroutine", PostEvents_Coroutine (requestHeaders, eventsPostEncodedData));
                } else {
                    eventsPostString = null;
                }
            } else {
                SwrveLog.LogWarning ("Sending events already in progress");
            }
        }

        return sentEvents;
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
    public void GetUserResources (Action<Dictionary<string, Dictionary<string, string>>, string> onResult, Action<Exception> onError)
    {
        if (Initialised) {
            if (userResources != null) {
                onResult.Invoke(userResources, userResourcesRaw);
            } else {
                onResult.Invoke(new Dictionary<string, Dictionary<string, string>>(), "[]");
            }
        }
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
    public void GetUserResourcesDiff (Action<Dictionary<string, Dictionary<string, string>>, Dictionary<string, Dictionary<string, string>>, string> onResult, Action<Exception> onError)
    {
        if (Initialised && !abTestUserResourcesDiffConnecting) {
            abTestUserResourcesDiffConnecting = true;
            StringBuilder getRequest = new StringBuilder (abTestResourcesDiffUrl);
            getRequest.AppendFormat ("?user={0}&api_key={1}&app_version={2}&joined={3}", escapedUserId, apiKey, WWW.EscapeURL (GetAppVersion()), installTimeEpoch);

            SwrveLog.Log("AB Test User Resources Diff request: " + getRequest.ToString());

            StartTask ("GetUserResourcesDiff_Coroutine", GetUserResourcesDiff_Coroutine (getRequest.ToString (), onResult, onError, AbTestUserResourcesDiffSave));
        } else {
            string errorStr = "Failed to initiate A/B test Diff GET request";
            SwrveLog.LogError(errorStr);
            if (onError != null) {
                onError.Invoke(new Exception(errorStr));
            }
        }
    }

    /// <summary>
    /// Loads unsent events and A/B test data from disk.
    /// </summary>
    public void LoadFromDisk ()
    {
        LoadEventsFromDisk ();
    }

    /// <summary>
    /// Save unsent events to disk.
    /// </summary>
    /// <param name="saveEventsBeingSent">
    /// Also save events that are in the outgoing buffer, trying to be sent.
    /// </param>
    public void FlushToDisk (bool saveEventsBeingSent = false)
    {
        if (Initialised) {
            // Concatenate existing events and buffer
            if (eventBufferStringBuilder != null) {
                StringBuilder savedEventStringBuilder = new StringBuilder ();
                string bufferedEvents = eventBufferStringBuilder.ToString ();
                // Clean event buffer to avoid sending twice
                eventBufferStringBuilder.Length = 0;
                // Save events that could be trying to be sent
                if(saveEventsBeingSent) {
                    savedEventStringBuilder.Append (eventsPostString);
                    eventsPostString = null;
                    if(bufferedEvents.Length > 0) {
                        if (savedEventStringBuilder.Length != 0) {
                            savedEventStringBuilder.Append (",");
                        }
                        savedEventStringBuilder.Append (bufferedEvents);
                    }
                } else {
                    savedEventStringBuilder.Append (bufferedEvents);
                }

                // Load old events saved in file
                try {
                    string loadedEvents = storage.Load (EventsSave, userId);
                    if(!string.IsNullOrEmpty(loadedEvents)) {
                        if (savedEventStringBuilder.Length != 0) {
                            savedEventStringBuilder.Append (",");
                        }
                        savedEventStringBuilder.Append (loadedEvents);
                    }
                } catch (Exception e) {
                    SwrveLog.LogWarning("Could not read events from cache (" + e.ToString() + ")");
                }

                // Save all the events in the storage
                string savedEventString = savedEventStringBuilder.ToString ();
                storage.Save (EventsSave, savedEventString, userId);
            }
        }
    }

    /// <summary>
    /// Returns the base path for the file storage.
    /// </summary>
    public string BasePath ()
    {
        return swrvePath;
    }
    #endregion

    /// <summary>
    /// Returns a map of Swrve device properties.
    /// </summary>
    public Dictionary<string, string> GetDeviceInfo ()
    {
        string deviceModel = GetDeviceModel();
        string osVersion = SystemInfo.operatingSystem;
#if UNITY_ANDROID
        string os = "Android";
#elif UNITY_IPHONE
        string os = "iOS";
#elif UNITY_STANDALONE_WIN
        string os = "PC";
#elif UNITY_STANDALONE_LINUX
        string os = "Linux";
#elif UNITY_STANDALONE_OSX
        string os = "Mac";
#elif UNITY_WEBPLAYER
        string os = "Browser";
#elif UNITY_WII
        string os = "Wii";
#elif UNITY_PS3
        string os = "PS3";
#elif UNITY_XBOX360
        string os = "XBOX360";
#elif UNITY_NACL
        string os = "NaCl";
#elif UNITY_FLASH
        string os = "Flash";
#elif UNITY_WP8
        string os = "WindowsPhone8";
#elif UNITY_METRO
        string os = "WindowsStore";
#else
#error
        string os = Application.platform.ToString();
#endif

        float dpi = (Screen.dpi == 0) ? DefaultDPI : Screen.dpi;

        Dictionary<string, string> deviceInfo = new Dictionary<string, string> () {
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
            { "swrve.install_date", installTimeFormatted }
        };

        String tzUtcOffsetSeconds = DateTimeOffset.Now.Offset.TotalSeconds.ToString();
        deviceInfo ["swrve.utc_offset_seconds"] = tzUtcOffsetSeconds;

#if UNITY_IPHONE
        if (!string.IsNullOrEmpty(iOSdeviceToken)) {
            deviceInfo["swrve.ios_token"] = iOSdeviceToken;
        }

        try {
            deviceInfo ["swrve.timezone_name"] = _swrveiOSGetTimeZone();
        } catch (Exception e) {
            SwrveLog.LogWarning("Couldn't get device timezone on iOS, make sure you have the plugin inside your project and you are running on a device: " + e.ToString());
        }
#elif UNITY_ANDROID
        if (!string.IsNullOrEmpty(gcmDeviceToken)) {
            deviceInfo["swrve.gcm_token"] = gcmDeviceToken;
        }

        string timezone = AndroidGetTimezone();
        if (!string.IsNullOrEmpty(timezone)) {
            deviceInfo ["swrve.timezone_name"] = timezone;
        }
#endif

        // Carrier info
        ICarrierInfo carrierInfo = GetCarrierInfoProvider();
        if (carrierInfo != null) {
            deviceInfo ["swrve.sim_operator.name"] = carrierInfo.GetName();
            deviceInfo ["swrve.sim_operator.iso_country_code"] = carrierInfo.GetIsoCountryCode();
            deviceInfo ["swrve.sim_operator.code"] = carrierInfo.GetCarrierCode();
        }

        return deviceInfo;
    }

    /// <summary>
    /// Call this function when the app pauses (phone call, move to another app, etc.).
    /// This method is called automatically if using the SwrveComponent.
    /// </summary>
    public void OnSwrvePause ()
    {
        if (Initialised) {
            FlushToDisk ();
            // Session management
            GenerateNewSessionInterval ();

            if (config != null && config.AutoDownloadCampaignsAndResources) {
                Container.StopCoroutine ("CheckForCampaignsAndResourcesUpdates_Coroutine");
            }
        }
    }

    /// <summary>
    /// Call this function when the app resumes.
    /// This method is called automatically if using the SwrveComponent.
    /// </summary>
    public void OnSwrveResume ()
    {
        if (Initialised) {
            LoadFromDisk ();
            QueueDeviceInfo ();
            // Session management
            long currentTime = GetSessionTime ();
            if (currentTime >= lastSessionTick) {
                SessionStart ();
            } else {
                SendQueuedEvents ();
            }
            GenerateNewSessionInterval ();

            StartCampaignsAndResourcesTimer();
            DisableAutoShowAfterDelay();
        }
    }

    /// <summary>
    /// Call this function when the app is being shutdown.
    /// This method is called automatically if using the SwrveComponent.
    /// </summary>
    public void OnSwrveDestroy ()
    {
        if (!Destroyed) {
            Destroyed = true;
            if (Initialised) {
                FlushToDisk (true);
            }

            if (config != null && config.AutoDownloadCampaignsAndResources) {
                Container.StopCoroutine ("CheckForCampaignsAndResourcesUpdates_Coroutine");
            }
        }
    }

    /// <summary>
    /// Get the latest available in-app campaigns.
    /// </summary>
    /// <returns>
    /// Latest in-app campagins available to the user.
    /// </returns>
    public List<SwrveCampaign> GetCampaigns ()
    {
        return campaigns;
    }

    /// <summary>
    /// Notify that a in-app message button was pressed.
    /// This method is automatically called by the SDK unless
    /// you are implementing your own rendering code.
    /// </summary>
    /// <param name="button">
    /// Button pressed by the user.
    /// </param>
    public void ButtonWasPressedByUser (SwrveButton button)
    {
        if (button != null) {
            try {
                SwrveLog.Log("Button " + button.ActionType + ": " + button.Action + " game id: " + button.GameId);

                if (button.ActionType != SwrveActionType.Dismiss) {
                    // Button other than dismiss pressed
                    String clickEvent = "Swrve.Messages.Message-" + button.Message.Id + ".click";
                    SwrveLog.Log("Sending click event: " + clickEvent);
                    Dictionary<string, string> clickPayload = new Dictionary<string, string> ();
                    clickPayload.Add ("name", button.Name);
                    NamedEventInternal (clickEvent, clickPayload);
                }
            } catch (Exception e) {
                SwrveLog.LogError("Error while processing button click " + e);
            }
        }
    }

    /// <summary>
    /// Notify that a message was displayed to the user.
    /// This method is automatically called by the SDK unless
    /// you are implementing your own rendering code.
    /// </summary>
    /// <param name="messageFormat">
    /// Message format displayed to the user
    /// </param>
    public void MessageWasShownToUser (SwrveMessageFormat messageFormat)
    {
        try {
            // The message was shown. Take the current time so that we can throttle messages
            // from being shown too quickly.
            SetMessageMinDelayThrottle();
            this.messagesLeftToShow = this.messagesLeftToShow - 1;

            // Update next for round robin
            SwrveCampaign campaign = messageFormat.Message.Campaign;
            if (campaign != null) {
                campaign.MessageWasShownToUser (messageFormat);
                SaveCampaignData (campaign);
            }

            // Send impression event
            String viewEvent = "Swrve.Messages.Message-" + messageFormat.Message.Id + ".impression";
            SwrveLog.Log("Sending view event: " + viewEvent);
            Dictionary<string, string> payload = new Dictionary<string, string> ();
            payload.Add ("format", messageFormat.Name);
            payload.Add ("orientation", messageFormat.Orientation.ToString ());
            payload.Add ("size", messageFormat.Size.X + "x" + messageFormat.Size.Y);
            NamedEventInternal (viewEvent, payload);
        } catch (Exception e) {
            SwrveLog.LogError("Error while processing message impression " + e);
        }
    }

    /// <summary>
    /// Determines if an in-app message is being displayed.
    /// </summary>
    /// <returns>
    /// True if there are any in-app messages currently on screen.
    /// </returns>
    public bool IsMessageDispaying ()
    {
        return (currentMessage != null);
    }

    /// <summary>
    /// Gets the app store link of a given game, that was
    /// setup in the dashboard for the current app store.
    /// </summary>
    /// <param name="gameId">
    /// Game identifier.
    /// </param>
    /// <returns>
    /// The app store link for a given game.
    /// </returns>
    public string GetAppStoreLink (int gameId)
    {
        string appStoreLink = null;
        if (gameStoreLinks != null) {
            gameStoreLinks.TryGetValue (gameId.ToString (), out appStoreLink);
        }
        return appStoreLink;
    }

    /// <summary>
    /// Obtain an in-app message for the given event.
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
    public SwrveMessage GetMessageForEvent (string eventName)
    {
        SwrveMessage result = null;
        SwrveCampaign campaign = null;
        DateTime now = SwrveHelper.GetNow();
        Dictionary<int, string> campaignReasons = null;
        Dictionary<int, int> campaignMessages = null;

        SwrveLog.Log("Trying to get message for: " + eventName);

        if (campaigns != null) {
            if (campaigns.Count == 0) {
                NoMessagesWereShown (eventName, "No campaigns available");
                return null;
            }

            if (!string.Equals(eventName, DefaultAutoShowMessagesTrigger, StringComparison.OrdinalIgnoreCase) && IsTooSoonToShowMessageAfterLaunch (now)) {
                NoMessagesWereShown(eventName, "{Game throttle limit} Too soon after launch. Wait until " + showMessagesAfterLaunch.ToString (WaitTimeFormat));
                return null;
            }

            if (IsTooSoonToShowMessageAfterDelay (now)) {
                NoMessagesWereShown(eventName, "{Game throttle limit} Too soon after last message. Wait until " + showMessagesAfterDelay.ToString (WaitTimeFormat));
                return null;
            }

            if (HasShowTooManyMessagesAlready ()) {
                NoMessagesWereShown(eventName, "{Game throttle limit} Too many messages shown");
                return null;
            }

            if (qaUser != null) {
                campaignReasons = new Dictionary<int, string> ();
                campaignMessages = new Dictionary<int, int> ();
            }

            List<SwrveMessage> availableMessages = new List<SwrveMessage>();
            // Select messages with higher priority
            int minPriority = int.MaxValue;
            List<SwrveMessage> candidateMessages = new List<SwrveMessage>();
            IEnumerator<SwrveCampaign> itCampaign = campaigns.GetEnumerator ();
            SwrveOrientation deviceOrientation = GetDeviceOrientation();
            while (itCampaign.MoveNext() && result == null) {
                SwrveCampaign nextCampaign = itCampaign.Current;
                SwrveMessage nextMessage = nextCampaign.GetMessageForEvent (eventName, campaignReasons);
                // Check if the message supports the current orientation
                if (nextMessage != null) {
                    if (nextMessage.SupportsOrientation(deviceOrientation)) {
                        availableMessages.Add(nextMessage);
                        if (nextMessage.Priority <= minPriority) {
                            minPriority = nextMessage.Priority;
                            if (nextMessage.Priority < minPriority) {
                                candidateMessages.Clear();
                            }
                            candidateMessages.Add(nextMessage);
                        }
                    } else {
                        if (qaUser != null) {
                            campaignMessages.Add (nextCampaign.Id, nextMessage.Id);
                            campaignReasons.Add (nextCampaign.Id, "Message didn't support the current device orientation: " + deviceOrientation);
                        }
                    }
                }
            }

            // Select randomly from the highest messages
            candidateMessages.Shuffle();
            if (candidateMessages.Count > 0) {
                result = candidateMessages[0];
                campaign = result.Campaign;
            }

            if (qaUser != null && campaign != null && result != null) {
                // A message was chosen, check if other campaigns would have returned a message
                IEnumerator<SwrveMessage> itOtherMessage = availableMessages.GetEnumerator ();
                while (itOtherMessage.MoveNext()) {
                    SwrveMessage otherMessage = itOtherMessage.Current;
                    if (otherMessage != result) {
                        int otherCampaignId = otherMessage.Campaign.Id;
                        if (!campaignMessages.ContainsKey(otherCampaignId)) {
                            campaignMessages.Add (otherCampaignId, otherMessage.Id);
                            campaignReasons.Add (otherCampaignId, "Campaign " + campaign.Id + " was selected for display ahead of this campaign");
                        }
                    }
                }
            }
        }

        if (qaUser != null) {
            qaUser.Trigger (eventName, result, campaignReasons, campaignMessages);
        }

        if (result == null) {
            SwrveLog.LogWarning ("Not showing message: no candidate messages for " + eventName);
        } else {
            // Notify message has been returned
            Dictionary<string, string> payload = new Dictionary<string, string> ();
            payload.Add ("id", result.Id.ToString ());
            NamedEventInternal ("Swrve.Messages.message_returned", payload, false);
        }

        return result;
    }

    /// <summary>
    /// Obtain an in-app message for the given id.
    /// </summary>
    /// <param name="messageId">
    /// The id of the message you want to retrieve.
    /// </param>
    /// <returns>
    /// In-app message for the given id.
    /// </returns>
    public SwrveMessage GetMessageForId (int messageId)
    {
        SwrveMessage message = null;
        IEnumerator<SwrveCampaign> itCampaign = campaigns.GetEnumerator ();
        while (itCampaign.MoveNext() && message == null) {
            SwrveCampaign campaign = itCampaign.Current;
            message = campaign.GetMessageForId (messageId);
            if (message != null) {
                return message;
            }
        }

        SwrveLog.LogWarning("Message with id " + messageId + " not found");
        return null;
    }

    /// <summary>
    /// Display a message available for the given trigger event.
    /// </summary>
    /// <remarks>
    /// See the REST API documentation for the "event" event.
    /// </remarks>
    /// <param name="eventName">
    /// The name of the event that was triggered
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
    public IEnumerator ShowMessageForEvent (string eventName, ISwrveInstallButtonListener installButtonListener = null, ISwrveCustomButtonListener customButtonListener = null, ISwrveMessageListener messageListener = null)
    {
        if (TriggeredMessageListener != null) {
            // They are using a custom listener
            SwrveMessage message = GetMessageForEvent (eventName);
            if (message != null) {
                TriggeredMessageListener.OnMessageTriggered (message);
            }
        } else {
            if (currentMessage == null) {
                SwrveMessage message = GetMessageForEvent (eventName);
                yield return Container.StartCoroutine(LaunchMessage(message, installButtonListener, customButtonListener, messageListener));
            }
        }
        TaskFinished ("ShowMessageForEvent");
    }

    /// <summary>
    /// Dismisses the current message if any is beign displayed.
    /// </summary>
    public void DismissMessage ()
    {
        if (TriggeredMessageListener != null) {
            TriggeredMessageListener.DismissCurrentMessage ();
        } else {
            try {
                if (currentMessage != null) {
                    SetMessageMinDelayThrottle();
                    currentMessage.Dismiss ();
                }
            } catch (Exception e) {
                SwrveLog.LogError("Error while dismissing a message " + e);
            }
        }
    }

    /// <summary>
    /// Reload the campaigns and user resources from the server.
    /// </summary>
    public void RefreshUserResourcesAndCampaigns ()
    {
        LoadResourcesAndCampaigns ();
    }

#if UNITY_IPHONE

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
#endif
#if UNITY_ANDROID
    /// <summary>
    /// Used internally by the Google Cloud Messaging plugin to notify
    /// of a device registration id.
    /// </summary>
    /// <param name="registrationId">
    /// The new device registration id.
    /// </param>
    public void RegistrationIdReceived(string registrationId)
    {
        if (!string.IsNullOrEmpty(registrationId)) {
            bool sendDeviceInfo = (this.gcmDeviceToken != registrationId);

            if (sendDeviceInfo) {
                this.gcmDeviceToken = registrationId;
                storage.Save (GcmdeviceTokenSave, gcmDeviceToken);
                if (qaUser != null) {
                    qaUser.UpdateDeviceInfo();
                }
                SendDeviceInfo();
            }
        }
    }

    /// <summary>
    /// Used internally by the Google Cloud Messaging plugin to notify
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
            PushNotificationListener.OnNotificationReceived(notification);
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
        if  (notification != null && notification.ContainsKey("_p")) {
            return notification["_p"].ToString();
        } else {
            SwrveLog.Log("Got unidentified notification");
        }

        return null;
    }

    /// <summary>
    /// Used internally by the Google Cloud Messaging plugin to notify
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

        if (PushNotificationListener != null) {
            PushNotificationListener.OnOpenedFromPushNotification(notification);
        }
    }
#endif
}

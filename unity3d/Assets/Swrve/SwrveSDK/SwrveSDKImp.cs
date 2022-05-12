#if UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE
#define SWRVE_SUPPORTED_PLATFORM
#endif
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SwrveUnity;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using SwrveUnityMiniJSON;
using System.IO;
using SwrveUnity.REST;
using SwrveUnity.Messaging;
using SwrveUnity.Input;
using SwrveUnity.Helpers;
using SwrveUnity.Storage;
using System.Reflection;
using System.Globalization;
using SwrveUnity.Device;
using SwrveUnity.SwrveUsers;

/// <summary>
/// Internal base class implementation of the Swrve SDK.
/// </summary>
public partial class SwrveSDK
{
    private const String Platform = "Unity ";
    private const float DefaultDPI = 160.0f;
    protected const string EventsSave = "Swrve_Events";
    protected const string AppInstallTimeSecondsSave = "Swrve_JoinedDate";
    protected const string UserJoinedTimeSecondsSave = "Swrve_InitTimeDate";
    protected const string iOSdeviceTokenSave = "Swrve_iOSDeviceToken";
    protected const string FirebaseDeviceTokenSave = "Swrve_gcmDeviceToken";
    protected const string AdmDeviceTokenSave = "Swrve_admDeviceToken";
    protected const string WindowsDeviceTokenSave = "Swrve_windowsDeviceToken";
    protected const string GoogleAdvertisingIdSave = "Swrve_googleAdvertisingId";
    protected const string AbTestUserResourcesSave = "srcngt2"; // Saved securely
    protected const string AbTestUserResourcesDiffSave = "rsdfngt2"; // Saved securely
    protected const string RealtimeUserPropertiesSave = "rupp2"; // Saved securely
    protected const string DeviceUUID = "Swrve_Device_UUID";
    protected const string IDFAString = "Swrve_IDFA";
    protected const string SeqNumSave = "Swrve_SeqNum";
    protected const string ResourcesCampaignTagSave = "cmpg_etag";
    protected const string ResourcesCampaignFlushFrequencySave = "swrve_cr_flush_frequency";
    protected const string ResourcesCampaignFlushDelaySave = "swrve_cr_flush_delay";

    private const string EmptyJSONObject = "{}";
    private const float DefaultCampaignResourcesFlushFrenquency = 60;
    private const float DefaultCampaignResourcesFlushRefreshDelay = 5;
    public const string DefaultAutoShowMessagesTrigger = "Swrve.Messages.showAtSessionStart";

    private const string PushTrackingKey = "_p";
    private const string SilentPushTrackingKey = "_sp";
    private const string PushDeeplinkKey = "_sd";
    private const string PushContentKey = "_sw";
    private const int PushContentVersion = 1;
    private const string PushNestedJsonKey = "_s.JsonPayload";
    private const string PushButtonToCampaignIdKey = "PUSH_BUTTON_TO_CAMPAIGN_ID";
    private const string PushUnityDoNotProcessKey = "SWRVE_UNITY_DO_NOT_PROCESS";
    private long installTimeSeconds;
    private string installTimeSecondsFormatted;
    private long userInitTimeSeconds;
    private string lastPushEngagedId;
    private int deviceWidth;
    private int deviceHeight;
    private long lastSessionTick;
    private ICarrierInfo deviceCarrierInfo;
    private string idfaString;

    // Events buffer
    protected StringBuilder eventBufferStringBuilder;
    protected string eventsPostString;

    // Storage
    protected string swrvePath;
    protected ISwrveStorage storage;

    // Swrve UserManager
    protected SwrveProfileManager profileManager;

    // WWW connections
    internal IRESTClient restClient;
    protected string contentServer;
    protected string eventsServer;
    protected string identityServer;
    private string eventsUrl;
    private string identifyUrl;
    private string abTestResourcesDiffUrl;
    protected bool eventsConnecting;
    protected bool abTestUserResourcesDiffConnecting;

    // AB tests and campaigns
    protected string userResourcesRaw;
    protected Dictionary<string, Dictionary<string, string>> userResources;
    protected string realtimeUserPropertiesRaw;
    protected Dictionary<string, string> realtimeUserProperties;
    protected float campaignsAndResourcesFlushFrequency;
    protected float campaignsAndResourcesFlushRefreshDelay;
    protected string lastETag;
    protected long campaignsAndResourcesLastRefreshed;
    protected bool campaignsAndResourcesInitialized;

    // Messaging related
    protected static readonly int CampaignEndpointVersion = 9;
    protected static readonly int InAppMessageCampaignVersion = 7;
    protected static readonly int EmbeddedCampaignVersion = 1;
    private static readonly int CampaignResponseVersion = 2;
    protected static readonly string CampaignsSave = "cmcc2"; // Saved securely
    /** Last campaign to come from /ad_journey_campaign endpoint */
    protected static readonly string LastExternalCampaignSave = "cmcc3"; // Saved securely
    protected static readonly string CampaignsSettingsSave = "Swrve_CampaignsData";
    private static readonly string WaitTimeFormat = @"HH\:mm\:ss zzz";
    protected static readonly string InstallTimeFormat = "yyyyMMdd";
    private string resourcesAndCampaignsUrl;
    protected string swrveTemporaryPath;
    protected bool campaignsConnecting;
    protected bool autoShowMessagesEnabled;
    protected Dictionary<int, SwrveCampaignState> campaignsState = new Dictionary<int, SwrveCampaignState>();
    protected List<SwrveBaseCampaign> campaigns = new List<SwrveBaseCampaign>();
    protected Dictionary<string, object> campaignSettings = new Dictionary<string, object>();
    protected Dictionary<string, string> appStoreLinks = new Dictionary<string, string>();
    protected SwrveInAppMessageView currentMessageView = null;
    protected SwrveOrientation currentOrientation;
    protected IInputManager inputManager = NativeInputManager.Instance;
    protected string prefabName;
    protected bool sdkStarted;
    private bool applicationPaused = false;

    // Messaging rules
    private const int DefaultDelayFirstMessage = 150;
    private const long DefaultMaxShows = 99999;
    private const int DefaultMinDelay = 55;
    private DateTime initialisedTime;
    private DateTime showMessagesAfterLaunch;
    private DateTime showMessagesAfterDelay;
    private long messagesLeftToShow;
    private int minDelayBetweenMessage;

    internal List<SwrveBaseCampaign> campaignDisplayQueue = new List<SwrveBaseCampaign>(); // Conversation / Message queue

    // Deeplink Manager
    protected SwrveDeeplinkManager deeplinkManager;

    protected bool campaignAndResourcesCoroutineEnabled = true;
    private IEnumerator campaignAndResourcesCoroutineInstance;

    private int conversationVersion;

    #region SDK internal States methods.

    internal DateTime GetInitialisedTime()
    {
        return this.initialisedTime;
    }

    internal protected bool IsSDKReady()
    {
        bool isSdkReady = true;
        if (profileManager.GetTrackingState() == SwrveTrackingState.STOPPED)
        {
            SwrveLog.LogWarning("Warning: SwrveSDK is stopped and needs to be started before calling this api.");
            isSdkReady = false;
        }
        else if (!IsStarted())
        {
            SwrveLog.LogWarning("Warning: SwrveSDK needs to be started before calling this api.");
            isSdkReady = false;
        }
        return isSdkReady;
    }

    private void EnableEventSending()
    {
        profileManager.SetTrackingState(SwrveTrackingState.STARTED);
        StartCampaignsAndResourcesTimer();
    }
    private void PauseEventSending()
    {
        profileManager.SetTrackingState(SwrveTrackingState.EVENT_SENDING_PAUSED);
        StopCheckForCampaignAndResources();
    }

    #endregion

    private void QueueSessionStart()
    {
        Dictionary<string, object> json = new Dictionary<string, object>();
        AppendEventToBuffer("session_start", json);
    }

    protected void NamedEventInternal(string name, Dictionary<string, string> payload = null, bool allowShowMessage = true)
    {
        if (payload == null)
        {
            payload = new Dictionary<string, string>();
        }

        Dictionary<string, object> json = new Dictionary<string, object>();
        json.Add("name", name);
        json.Add("payload", payload);

        AppendEventToBuffer("event", json, allowShowMessage);
    }

    protected static string GetSwrvePath()
    {
        string path = Application.persistentDataPath;
        if (string.IsNullOrEmpty(path))
        {
            path = Application.temporaryCachePath;
            SwrveLog.Log("Swrve path (tried again): " + path);
        }
        return path;
    }

    protected void SetConversationVersion(int conversationVersion)
    {
        this.conversationVersion = conversationVersion;
    }

    protected static string GetSwrveTemporaryCachePath()
    {
        string path = Application.temporaryCachePath;
        if (path == null || path.Length == 0)
        {
            path = Application.persistentDataPath;
        }
#if UNITY_IOS
        path = path + "/com.ngt.msgs";
#endif
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    private void _Iap(int quantity, string productId, double productPrice, string currency, IapRewards rewards, string receipt, string receiptSignature, string transactionId, string appStore)
    {
        if (!_Iap_check_arguments(quantity, productId, productPrice, currency, appStore))
        {
            SwrveLog.LogError("ERROR: IAP event not sent because it received an illegal argument");
            return;
        }

        Dictionary<string, object> json = new Dictionary<string, object>();
        json.Add("app_store", appStore);
        json.Add("local_currency", currency);
        json.Add("cost", productPrice);
        json.Add("product_id", productId);
        json.Add("quantity", quantity);
        json.Add("rewards", rewards.getRewards());

        if (!string.IsNullOrEmpty(GetAppVersion()))
        {
            json.Add("app_version", GetAppVersion());
        }

        if (appStore == "apple")
        {
            // receipt comes from the new wrapper and should be base64 encoded here
            json.Add("receipt", receipt);
            if (!string.IsNullOrEmpty(transactionId))
            {
                json.Add("transaction_id", transactionId);
            }
        }
        else if (appStore == "google")
        {
            json.Add("receipt", receipt);
            json.Add("receipt_signature", receiptSignature);
        }
        else
        {
            json.Add("receipt", receipt);
        }

        AppendEventToBuffer("iap", json);

        if (config.AutoDownloadCampaignsAndResources)
        {
            // Send events automatically and check for changes
            CheckForCampaignsAndResourcesUpdates(false);
        }
    }

    protected virtual SwrveOrientation GetDeviceOrientation()
    {
        ScreenOrientation orientation = Screen.orientation;
        switch (orientation)
        {
            case ScreenOrientation.LandscapeLeft:
            case ScreenOrientation.LandscapeRight:
                return SwrveOrientation.Landscape;
            case ScreenOrientation.Portrait:
            case ScreenOrientation.PortraitUpsideDown:
                return SwrveOrientation.Portrait;
            default:
                // Unknown orientation, calculate by the size of the screen
                if (Screen.height >= Screen.width)
                {
                    return SwrveOrientation.Portrait;
                }
                return SwrveOrientation.Landscape;
        }
    }

    private bool _Iap_check_arguments(int quantity, string productId, double productPrice, string currency, string appStore)
    {
        if (String.IsNullOrEmpty(productId))
        {
            SwrveLog.LogError("IAP event illegal argument: productId cannot be empty");
            return false;
        }
        if (String.IsNullOrEmpty(currency))
        {
            SwrveLog.LogError("IAP event illegal argument: currency cannot be empty");
            return false;
        }
        if (String.IsNullOrEmpty(appStore))
        {
            SwrveLog.LogError("IAP event illegal argument: appStore cannot be empty");
            return false;
        }

        if (quantity <= 0)
        {
            SwrveLog.LogError("IAP event illegal argument: quantity must be greater than zero");
            return false;
        }
        if (productPrice < 0)
        {
            SwrveLog.LogError("IAP event illegal argument: productPrice must be greater than or equal to zero");
            return false;
        }

        return true;
    }

    private Dictionary<string, Dictionary<string, string>> ProcessUserResources(IList<object> userResources)
    {
        Dictionary<string, Dictionary<string, string>> result = new Dictionary<string, Dictionary<string, string>>();
        if (userResources != null)
        {
            IEnumerator<object> userResourcesIt = userResources.GetEnumerator();
            while (userResourcesIt.MoveNext())
            {
                Dictionary<string, object> userResource = (Dictionary<string, object>)userResourcesIt.Current;
                string uid = (string)userResource["uid"];
                result.Add(uid, NormalizeJson(userResource));
            }
        }

        return result;
    }

    private Dictionary<string, string> NormalizeJson(Dictionary<string, object> json)
    {
        Dictionary<string, string> normalized = new Dictionary<string, string>();
        Dictionary<string, object>.Enumerator enumerator = json.GetEnumerator();
        while (enumerator.MoveNext())
        {
            KeyValuePair<string, object> item = enumerator.Current;
            if (item.Value != null)
            {
                normalized.Add(item.Key, item.Value.ToString());
            }
        }

        return normalized;
    }

    private IEnumerator GetUserResourcesDiff_Coroutine(string getRequest, Action<Dictionary<string, Dictionary<string, string>>, Dictionary<string, Dictionary<string, string>>, string> onResult, Action<Exception> onError, string saveCategory)
    {
        Exception wwwException = null;
        string abTestCandidate = null;
        yield return Container.StartCoroutine(restClient.Get(getRequest, delegate (RESTResponse response)
        {
            if (response.Error == WwwDeducedError.NoError)
            {
                abTestCandidate = response.Body;
                SwrveLog.Log("AB Test result: " + abTestCandidate);
                storage.SaveSecure(saveCategory, abTestCandidate, this.UserId);
                TaskFinished("GetUserResourcesDiff_Coroutine");
            }
            else
            {
                // WWW connection error
                wwwException = new Exception(response.Error.ToString());
                SwrveLog.LogError("AB Test request failed: " + response.Error.ToString());
                TaskFinished("GetUserResourcesDiff_Coroutine");
            }
        }));

        abTestUserResourcesDiffConnecting = false;

        if (wwwException != null || string.IsNullOrEmpty(abTestCandidate))
        {
            // Try to load from cache
            try
            {
                string loadedData = storage.LoadSecure(saveCategory, this.UserId);
                if (string.IsNullOrEmpty(loadedData))
                {
                    onError.Invoke(wwwException);
                }
                else
                {
                    if (ResponseBodyTester.TestUTF8(loadedData, out abTestCandidate))
                    {
                        Dictionary<string, Dictionary<string, string>> userResourcesDiffNew = new Dictionary<string, Dictionary<string, string>>();
                        Dictionary<string, Dictionary<string, string>> userResourcesDiffOld = new Dictionary<string, Dictionary<string, string>>();
                        ProcessUserResourcesDiff(abTestCandidate, userResourcesDiffNew, userResourcesDiffOld);
                        onResult.Invoke(userResourcesDiffNew, userResourcesDiffOld, abTestCandidate);
                    }
                    else
                    {
                        // Launch error
                        onError.Invoke(wwwException);
                    }
                }
            }
            catch (Exception e)
            {
                SwrveLog.LogWarning("Could not read user resources diff from cache (" + e.ToString() + ")");
                onError.Invoke(wwwException);
            }
        }
        else
        {
            // Launch listener
            if (!string.IsNullOrEmpty(abTestCandidate))
            {
                Dictionary<string, Dictionary<string, string>> userResourcesDiffNew = new Dictionary<string, Dictionary<string, string>>();
                Dictionary<string, Dictionary<string, string>> userResourcesDiffOld = new Dictionary<string, Dictionary<string, string>>();
                ProcessUserResourcesDiff(abTestCandidate, userResourcesDiffNew, userResourcesDiffOld);
                onResult.Invoke(userResourcesDiffNew, userResourcesDiffOld, abTestCandidate);
            }
        }
    }

    private void ProcessUserResourcesDiff(string abTestJson, Dictionary<string, Dictionary<string, string>> newResources, Dictionary<string, Dictionary<string, string>> oldResources)
    {
        IList<object> userResourcesDiffJson = (List<object>)Json.Deserialize(abTestJson);
        if (userResourcesDiffJson != null)
        {
            IEnumerator<object> userResourcesIt = userResourcesDiffJson.GetEnumerator();
            while (userResourcesIt.MoveNext())
            {
                Dictionary<string, object> userResource = (Dictionary<string, object>)userResourcesIt.Current;
                string uid = (string)userResource["uid"];
                Dictionary<string, object> item = (Dictionary<string, object>)userResource["diff"];
                IEnumerator<string> itemKey = item.Keys.GetEnumerator();

                Dictionary<string, string> newItemData = new Dictionary<string, string>();
                Dictionary<string, string> oldItemData = new Dictionary<string, string>();
                while (itemKey.MoveNext())
                {
                    Dictionary<string, string> currentKey = NormalizeJson((Dictionary<string, object>)item[itemKey.Current]);
                    newItemData.Add(itemKey.Current, currentKey["new"]);
                    oldItemData.Add(itemKey.Current, currentKey["old"]);
                }

                newResources.Add(uid, newItemData);
                oldResources.Add(uid, oldItemData);
            }
        }
    }

    private string GetDeviceUUID()
    {
        string deviceUUID = storage.Load(DeviceUUID);
        if (string.IsNullOrEmpty(deviceUUID))
        {
            // Generate a unique device id and save it on our storage.
            deviceUUID = SwrveHelper.GetRandomUUID();
            storage.Save(DeviceUUID, deviceUUID);
        }
        return deviceUUID;
    }

    private string GetIDFA()
    {
        idfaString = storage.Load(IDFAString);
        if (string.IsNullOrEmpty(idfaString))
        {
            return null;
        }
        return idfaString;
    }

    private void HandleCampaignFromNotification(string campaignID)
    {
        if (IsStarted())
        {
            if (deeplinkManager == null)
            {
                deeplinkManager = new SwrveDeeplinkManager(Container, this, contentServer);
            }
            deeplinkManager.HandleNotificationToCampaign(campaignID);
        }
    }

    /// <summary>
    /// Method used to begin a session during the "Identify" or during the SDKInit.
    /// </summary>
    private void BeginSession()
    {
        this.EnableEventSending();
        this.DisableAutoShowAfterDelay();

        if (config.AutomaticSessionManagement)
        {
            // Start a new session (will send events)
            QueueSessionStart();
            GenerateNewSessionInterval();
        }
        if (profileManager.isNewUser)
        {
            NamedEventInternal("Swrve.first_session", null, false);
        }

#if UNITY_IOS
        RefreshPushPermissions();
        SaveConfigForPushDelivery();
#endif
        this.QueueDeviceInfo();
        this.StartCampaignsAndResourcesTimer();
        this.SendQueuedEvents();
    }

    public void SwitchUser(string userId, bool identifiedOnAnotherDevice)
    {
        // Dont do anything if the current user is the same as the new one
        if (IsStarted())
        {
            if (userId == null || userId == profileManager.userId)
            {
                EnableEventSending();
                return;
            }
        }
        // Remove notifications from the current authenticated user.
#if UNITY_ANDROID || UNITY_IOS
        ClearAllAuthenticatedNotifications();
#endif
        profileManager.userId = userId;
        profileManager.SaveSwrveUserId(userId); // Update local storage
        // Update the native layer with the new userId.

#if UNITY_ANDROID || UNITY_IOS
        UpdateNativeUserId();
#endif
        // This isNewUser flag is used to trigger the "Swrve.first_session" event on BeginSession.
        if (!identifiedOnAnotherDevice)
        {
            profileManager.isNewUser = false;
        }

        // The userId changed so we need to create a storage for the current user.
        storage = CreateStorage();
        storage.SetSecureFailedListener(delegate ()
        {
            NamedEventInternal("Swrve.signature_invalid", null, false);
        });

        // This will recreate our SwrveQaUser class and also flush any possible event available in memory from the previous user.
        string eventServer = string.IsNullOrEmpty(config.EventsServer) ? GetSwrveEndpoint(appId, config.SelectedStack, "api.swrve.com") : config.EventsServer;
        SwrveQaUser.Init(Container, eventServer, apiKey, appId, userId, GetAppVersion(), GetDeviceUUID(), storage);

        // Re initialize Event Buffer
        eventBufferStringBuilder = new StringBuilder(config.MaxBufferChars);

        this.InitUser();
    }

    private bool ShouldAutoStart()
    {
        bool shouldAutostart = false;
        if (config.InitMode == SwrveInitMode.AUTO && config.AutoStartLastUser)
        {
            shouldAutostart = true;
        }
        else if (config.InitMode == SwrveInitMode.MANAGED && config.AutoStartLastUser)
        {
            string savedUserIdFromPrefs = profileManager.userId;
            if (string.IsNullOrEmpty(savedUserIdFromPrefs) == false)
            {
                shouldAutostart = true;
            }
        }
        return shouldAutostart;
    }

    private void InitUser()
    {
        sdkStarted = true;
#if UNITY_ANDROID || UNITY_IOS
        UpdateTrackingStateStopped(false);
#endif

        // some variables and config that are for the specific user that is running.
        this.lastSessionTick = SwrveHelper.GetMilliseconds();

        // Save init time
        initialisedTime = SwrveHelper.GetNow();
        showMessagesAfterDelay = initialisedTime;
        autoShowMessagesEnabled = true;
        profileManager.SetTrackingState(SwrveTrackingState.STARTED);

        CheckUserTimes();

        // Load stored data
        LoadData();

        if (config.ABTestDetailsEnabled)
        {
            try
            {
                LoadABTestDetails();
            }
            catch (Exception e)
            {
                SwrveLog.LogError("Error while initializing " + e);
            }
        }
        InitUserResources();
        InitRealtimeUserProperties();
        // Get device info
        deviceCarrierInfo = new DeviceCarrierInfo();
        GetDeviceScreenInfo();

        // In-app messaging features
        if (config.MessagingEnabled)
        {
            LoadTalkData();
        }
        this.BeginSession();
    }

    /// <summary>
    /// Check and update the current user IntallTime and UserInitTime on storage. This method update as well the profileManager.isNewUser bool.
    /// </summary>
    private void CheckUserTimes()
    {
        // save the first time a user gets initiailised for signature key and setting isNewUser in profile manager
        string userInitTimeSecondsFromFile = storage.Load(UserJoinedTimeSecondsSave, this.UserId);
        if (string.IsNullOrEmpty(userInitTimeSecondsFromFile))
        {
            profileManager.isNewUser = true;
            userInitTimeSeconds = GetSessionTime();
            storage.Save(UserJoinedTimeSecondsSave, userInitTimeSeconds.ToString(), this.UserId);
        }
        else
        {
            profileManager.isNewUser = false;
            long.TryParse(userInitTimeSecondsFromFile, out userInitTimeSeconds);
        }
    }

    private string GetNextSeqNum()
    {
        string seqNum = storage.Load(SeqNumSave, this.UserId);
        // increment value
        int value;
        seqNum = int.TryParse(seqNum, out value) ? (++value).ToString() : "1";
        storage.Save(SeqNumSave, seqNum, this.UserId);
        return seqNum;
    }

    protected string GetDeviceLanguage()
    {
        string language = getNativeLanguage();

        if (string.IsNullOrEmpty(language))
        {
            CultureInfo info = CultureInfo.CurrentUICulture;
            string cultureLang = info.TwoLetterISOLanguageName.ToLower();
            if (cultureLang != "iv")
            {
                language = cultureLang;
            }
        }

        return language;
    }

    protected void InvalidateETag()
    {
        lastETag = string.Empty;
        storage.Remove(ResourcesCampaignTagSave, this.UserId);
    }

    private void InitUserResources()
    {
        userResourcesRaw = storage.LoadSecure(AbTestUserResourcesSave, this.UserId);
        if (!string.IsNullOrEmpty(userResourcesRaw))
        {
            IList<object> userResourcesJson = (IList<object>)Json.Deserialize(userResourcesRaw);
            userResources = ProcessUserResources(userResourcesJson);
            NotifyUpdateUserResources();
        }
        else
        {
            InvalidateETag();
        }
    }

    private void InitRealtimeUserProperties()
    {
        realtimeUserPropertiesRaw = storage.LoadSecure(RealtimeUserPropertiesSave, this.UserId);
        if (!string.IsNullOrEmpty(realtimeUserPropertiesRaw))
        {
            Dictionary<string, object> realtimeUserPropertiesJson = (Dictionary<string, object>)Json.Deserialize(realtimeUserPropertiesRaw);
            realtimeUserProperties = NormalizeJson(realtimeUserPropertiesJson);
        }
        else
        {
            InvalidateETag();
        }
    }

    private void NotifyUpdateUserResources()
    {
        if (userResources != null)
        {
            ResourceManager.SetResourcesFromJSON(userResources);
            if (config.ResourcesUpdatedCallback != null)
            {
                config.ResourcesUpdatedCallback.Invoke();
            }
        }
    }

    private void LoadEventsFromDisk()
    {
        try
        {
            // Load cached events
            string loadedEvents = storage.Load(EventsSave, this.UserId);
            storage.Remove(EventsSave, this.UserId);
            // Add loaded events to buffer
            if (!string.IsNullOrEmpty(loadedEvents))
            {
                if (eventBufferStringBuilder.Length != 0)
                {
                    eventBufferStringBuilder.Insert(0, ",");
                }
                eventBufferStringBuilder.Insert(0, loadedEvents);
            }
        }
        catch (Exception e)
        {
            SwrveLog.LogWarning("Could not read events from cache (" + e.ToString() + ")");
        }
    }

    private void LoadData()
    {
        // Load events
        LoadEventsFromDisk();

        // Load latest etag
        lastETag = storage.Load(ResourcesCampaignTagSave, this.UserId);

        // Load user resources and campaign flush settings
        string strFlushFrequency = storage.Load(ResourcesCampaignFlushFrequencySave, this.UserId);
        if (!string.IsNullOrEmpty(strFlushFrequency))
        {
            if (float.TryParse(strFlushFrequency, out campaignsAndResourcesFlushFrequency))
            {
                campaignsAndResourcesFlushFrequency /= 1000;
            }
        }
        if (campaignsAndResourcesFlushFrequency == 0)
        {
            campaignsAndResourcesFlushFrequency = DefaultCampaignResourcesFlushFrenquency;
        }
        string strFlushDelay = storage.Load(ResourcesCampaignFlushDelaySave, this.UserId);
        if (!string.IsNullOrEmpty(strFlushDelay))
        {
            if (float.TryParse(strFlushDelay, out campaignsAndResourcesFlushRefreshDelay))
            {
                campaignsAndResourcesFlushRefreshDelay /= 1000;
            }
        }
        if (campaignsAndResourcesFlushRefreshDelay == 0)
        {
            campaignsAndResourcesFlushRefreshDelay = DefaultCampaignResourcesFlushRefreshDelay;
        }

#if UNITY_IOS
        // Load device token
        iOSdeviceToken = GetSavediOSDeviceToken();
#endif
    }

    // Create a unique key that can be used to create HMAC signatures
    protected string GetUniqueKey()
    {
        return apiKey + this.UserId;
    }

    protected virtual IRESTClient CreateRestClient()
    {
        return new RESTClient();
    }

    protected virtual ISwrveStorage CreateStorage()
    {
        if (config.StoreDataInPlayerPrefs)
        {
            return new SwrvePlayerPrefsStorage();
        }
        else
        {
            return new SwrveFileStorage(swrvePath, GetUniqueKey());
        }
    }

    #region User Identify
    private string GetUnidentifiedUserId(string externalUserId, SwrveUser cachedUser)
    {
        string userId = null;
        if (cachedUser == null)
        {
            // if the current swrve user id hasn't already been used, we can use it
            SwrveUser existingUser = profileManager.GetSwrveUser(this.UserId);
            userId = (existingUser == null) ? this.UserId : SwrveHelper.GetRandomUUID();

            // save unverified user
            SwrveUser unverifiedUser = new SwrveUser(userId, externalUserId, false);
            profileManager.SaveSwrveUser(unverifiedUser);
        }
        else
        {
            userId = cachedUser.swrveId; // a previous identify call didn't complete so user has been cached and is unverified
        }
        return userId;
    }

    private bool IdentifyCachedUser(SwrveUser cachedSwrveUser, OnSuccessIdentify onSuccessCallback)
    {
        bool isVerified = false;
        if (cachedSwrveUser != null && cachedSwrveUser.verified)
        {
            SwrveLog.LogInfo("Swrve identify: Identity API call skipped, user loaded from cache. Event sending reenabled.");
            this.SwitchUser(cachedSwrveUser.swrveId, false);

            if (onSuccessCallback != null)
            {
                onSuccessCallback("Swrve identify: Identity API call skipped, user loaded from cache", cachedSwrveUser.swrveId);
            }
            isVerified = true;
        }
        return isVerified;
    }

    private void IdentifyUnknownUser(string externalUserId, SwrveUser cachedUser, OnSuccessIdentify onSuccessCallback, OnErrorIdentify onErrorCallback)
    {
        string unidentifiedSwrveId = this.GetUnidentifiedUserId(externalUserId, cachedUser);

        // Didn't find this user in cache, so execute the identify API.
        // SDK Internal - Identify SuccessCallback
        SwrveSDK.OnSuccessIdentify swrveOnSuccessIdentify = delegate (string status, string swrveUserId)
        {
            SwrveLog.LogInfo("Swrve identify: Identity service success " + status);
            // update the swrve user in cache
            profileManager.UpdateSwrveUser(swrveUserId, externalUserId);
            bool isFirstSession = (unidentifiedSwrveId == swrveUserId) ? true : false;
            this.SwitchUser(swrveUserId, isFirstSession);

            // Check and return user call back.
            if (onSuccessCallback != null)
            {
                onSuccessCallback(status, swrveUserId);
            }
        };

        // SDK Internal - Identify ErrorCallback
        SwrveSDK.OnErrorIdentify swrveOnErrorIdentify = delegate (long httpCode, string errorMessage)
        {
            SwrveLog.LogError("Swrve identify: Identity service error code is " + httpCode.ToString() + " error message: " + errorMessage);
            this.SwitchUser(unidentifiedSwrveId, true);

            if (httpCode == 403)
            {
                this.profileManager.RemoveSwrveUser(externalUserId);
            }

            // Check and return user call back.
            if (onErrorCallback != null)
            {
                onErrorCallback(httpCode, errorMessage);
            }
        };

        // Create identity post and start task.
        byte[] userIdentityPostEncodedData = PostBodyBuilder.BuildIdentify(apiKey, unidentifiedSwrveId, externalUserId, GetDeviceUUID());
        if (userIdentityPostEncodedData != null)
        {
            SwrveLog.LogInfo("Will send identify API call for user: " + externalUserId);
            Dictionary<string, string> requestHeaders = new Dictionary<string, string> {
                { @"Content-Type", @"application/json; charset=utf-8" },
                { @"Cache-Control", @"no-cache; charset=utf-8" }
            };
            StartTask("PostIdentity_Coroutine", PostIdentity_Coroutine(requestHeaders, userIdentityPostEncodedData, swrveOnSuccessIdentify, swrveOnErrorIdentify));
        }
    }
    #endregion

    #region WWW coroutines

    private IEnumerator PostEvents_Coroutine(Dictionary<string, string> requestHeaders, byte[] eventsPostEncodedData)
    {
        yield return Container.StartCoroutine(restClient.Post(eventsUrl, eventsPostEncodedData, requestHeaders, delegate (RESTResponse response)
        {
            if (response.Error != WwwDeducedError.NetworkError)
            {
                // - made it there and it was ok
                // - made it there and it was rejected
                // either way don't send again
                ClearEventBuffer();
                eventsPostEncodedData = null;
            }
            eventsConnecting = false;
            TaskFinished("PostEvents_Coroutine");
        }));
    }

    private IEnumerator PostIdentity_Coroutine(Dictionary<string, string> requestHeaders, byte[] eventsPostEncodedData, OnSuccessIdentify onSuccessCallback, OnErrorIdentify onErrorCallback)
    {
        yield return Container.StartCoroutine(restClient.Post(identifyUrl, eventsPostEncodedData, requestHeaders, delegate (RESTResponse response)
        {
            string status = null;
            string verifiedSwrveUserId = null;
            string errorMessage = null;
            long statusCode = response.ResponseCode;
            if (response.Error == WwwDeducedError.NoError || response.Error == WwwDeducedError.ApplicationErrorHeader)
            {
                if (!string.IsNullOrEmpty(response.Body))
                {
                    Dictionary<string, object> root = (Dictionary<string, object>)Json.Deserialize(response.Body);
                    if (root != null)
                    {
                        if (root.ContainsKey("status"))
                        {
                            status = (string)root["status"];
                        }
                        if (root.ContainsKey("swrve_id"))
                        {
                            verifiedSwrveUserId = (string)root["swrve_id"];
                        }
                        if (root.ContainsKey("message"))
                        {
                            errorMessage = (string)root["message"];
                        }
                        if (root.ContainsKey("code"))
                        {
                            statusCode = Convert.ToInt64(root["code"]);
                        }
                    }
                }
            }
            else
            {
                // Just return a generic error if something went wrong.
                errorMessage = response.Error.ToString();
            }

            // Check for ResponseCode to return the callback.
            if (statusCode >= 200 && statusCode < 300)
            {
                onSuccessCallback.Invoke(status, verifiedSwrveUserId);
            }
            else
            {
                onErrorCallback.Invoke(statusCode, errorMessage);
            }
            TaskFinished("PostIdentity_Coroutine");
        }));
    }

    protected virtual void ClearEventBuffer()
    {
        eventsPostString = null;
    }
    #endregion

    #region Event buffer
    private void AppendEventToBuffer(string eventType, Dictionary<string, object> eventParameters, bool allowShowMessage = true)
    {
        eventParameters.Add("type", eventType);
        eventParameters.Add("seqnum", GetNextSeqNum());
        eventParameters.Add("time", GetSessionTime());

        // Discard the event if it would cause the buffer to overflow
        String eventJson = Json.Serialize(eventParameters);
        string eventName = SwrveHelper.GetEventName(eventParameters);
        bool insideMaxBufferLength = eventBufferStringBuilder.Length + eventJson.Length <= config.MaxBufferChars;
        if (insideMaxBufferLength || config.SendEventsIfBufferTooLarge)
        {
            // Send buffer if too large
            if (!insideMaxBufferLength && config.SendEventsIfBufferTooLarge)
            {
                SendQueuedEvents();
            }

            if (eventBufferStringBuilder.Length > 0)
            {
                eventBufferStringBuilder.Append(',');
            }

            AppendEventToBuffer(eventJson);
            SwrveQaUser.WrappedEvent(eventParameters);

#if UNITY_IOS
            if (config.PushNotificationEnabled)
            {
                // Ask for push notification permission dialog
                if (config.PushNotificationEvents != null && config.PushNotificationEvents.Contains(eventName))
                {
                    RegisterForPushNotificationsIOS(false);
                }
                else if (config.ProvisionalPushNotificationEvents != null && config.ProvisionalPushNotificationEvents.Contains(eventName))
                {
                    RegisterForPushNotificationsIOS(true);
                }
            }
#endif
        }
        else
        {
            SwrveLog.LogError("Could not append the event to the buffer. Please consider enabling SendEventsIfBufferTooLarge");
        }

        if (allowShowMessage)
        {
            object payload;
            eventParameters.TryGetValue("payload", out payload);
            ShowBaseMessage(eventName, (IDictionary<string, string>)payload);
        }
    }

    protected virtual void AppendEventToBuffer(string eventJson)
    {
        eventBufferStringBuilder.Append(eventJson);
    }
    #endregion

    protected virtual Coroutine StartTask(string tag, IEnumerator task)
    {

        return Container.StartCoroutine(task);
    }

    protected virtual void TaskFinished(string tag)
    {
    }

    protected void ShowBaseMessage(string eventName, IDictionary<string, string> payload)
    {
        SwrveBaseMessage baseMessage = GetBaseMessage(eventName, payload);
        if (null != baseMessage)
        {
            if (baseMessage is SwrveConversation)
            {
                StartTask("ShowConversationForEvent", ShowConversationForEvent(eventName, (SwrveConversation)baseMessage));
            }
            else
            {
                //Handle SwrveEmbeddedCampaign and SwrveInAppCampaign
                StartTask("ShowMessageForEvent", ShowMessageForEvent(eventName, payload, baseMessage, config.InAppMessageConfig.CustomButtonListener, config.InAppMessageConfig.MessageListener, config.InAppMessageConfig.ClipboardButtonListener, config.EmbeddedMessageConfig.EmbeddedMessageListener));
            }
        }
    }

    public SwrveBaseMessage GetBaseMessage(string eventName, IDictionary<string, string> eventPayload = null)
    {
        if (!checkGlobalRules(eventName, eventPayload, SwrveHelper.GetNow()))
        {
            return null;
        }

        SwrveBaseMessage baseMessage = null;
        if (config.MessagingEnabled)
        {
            baseMessage = GetBaseMessageForEvent(eventName, eventPayload);
        }

        if ((baseMessage == null) && config.ConversationsEnabled)
        {
            baseMessage = GetConversationForEvent(eventName, eventPayload);
        }
        else if ((baseMessage != null) && config.ConversationsEnabled)
        {
            SwrveQaUser.CampaignTriggeredConversationNoDisplay(eventName, eventPayload); // Message was selected so we just log it here as a QA user.
        }

        if (baseMessage == null)
        {
            SwrveLog.Log("Not showing message: no candidate for " + eventName);
        }
        else
        {
            SwrveLog.Log(string.Format(
                             "[{0}] {1} has been chosen for {2}\nstate: {3}",
                             baseMessage, baseMessage.Campaign.Id, eventName, baseMessage.Campaign.State));
        }

        return baseMessage;
    }

    private bool IsAlive()
    {
        return (Container != null && !Destroyed);
    }

    protected virtual void GetDeviceScreenInfo()
    {
        deviceWidth = Screen.width;
        deviceHeight = Screen.height;

        if (deviceWidth > deviceHeight)
        {
            int tmp = deviceWidth;
            deviceWidth = deviceHeight;
            deviceHeight = tmp;
        }
    }

    private void QueueDeviceInfo()
    {
#if SWRVE_SUPPORTED_PLATFORM
        Dictionary<string, string> deviceInfo = GetDeviceInfo();
        if (deviceInfo != null && deviceInfo.Count > 0)
        {
            Dictionary<string, object> json = new Dictionary<string, object>();
            json.Add("attributes", deviceInfo);
            AppendEventToBuffer("device_update", json, false);
        }
        else
        {
            SwrveLog.LogError("Invoked user update with no update attributes");
        }
#endif
    }

    private void SendDeviceInfo()
    {
        QueueDeviceInfo();
        SendQueuedEvents();
    }

    private IEnumerator WaitAndRefreshResourcesAndCampaigns_Coroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        RefreshUserResourcesAndCampaigns();
    }

    private void CheckForCampaignsAndResourcesUpdates(bool invokedByTimer)
    {
        if (!IsAlive())
        {
            // The container was destroyed and we should stop
            return;
        }

        bool sentEvents = SendQueuedEvents();
        if (sentEvents)
        {
            // Wait for events to be processed and then ask for campaigns and resources
            Container.StartCoroutine(WaitAndRefreshResourcesAndCampaigns_Coroutine(campaignsAndResourcesFlushRefreshDelay));
        }

        if (!invokedByTimer)
        {
            // Restart flush timer
            StopCheckForCampaignAndResources();
            StartCheckForCampaignsAndResources();
        }
    }

    private void StartCheckForCampaignsAndResources()
    {
        if (campaignAndResourcesCoroutineInstance == null)
        {
            campaignAndResourcesCoroutineInstance = CheckForCampaignsAndResourcesUpdates_Coroutine();
            Container.StartCoroutine(campaignAndResourcesCoroutineInstance);
        }
        campaignAndResourcesCoroutineEnabled = true;
    }

    private void StopCheckForCampaignAndResources()
    {
        if (campaignAndResourcesCoroutineInstance != null)
        {
            Container.StopCoroutine("campaignAndResourcesCoroutineInstance");
            campaignAndResourcesCoroutineInstance = null;
        }
        campaignAndResourcesCoroutineEnabled = false;
    }

    private IEnumerator CheckForCampaignsAndResourcesUpdates_Coroutine()
    {
        yield return new WaitForSeconds(campaignsAndResourcesFlushFrequency);
        CheckForCampaignsAndResourcesUpdates(true);
        if (campaignAndResourcesCoroutineEnabled)
        {
            campaignAndResourcesCoroutineInstance = null;
            StartCheckForCampaignsAndResources();
        }
    }

    protected virtual long GetSessionTime()
    {
        return SwrveHelper.GetMilliseconds();
    }

    private void GenerateNewSessionInterval()
    {
        lastSessionTick = GetSessionTime() + (config.NewSessionInterval * 1000);
    }

    public void Update()
    {
        if (currentMessageView != null)
        {
            SwrveButtonClickResult clickedResult = currentMessageView.Update(inputManager, NativeIsBackPressed());
            ProcessButtonUp(clickedResult);

            if (currentMessageView.Dismissed)
            {
                currentMessageView = null;
                HandleNextCampaign();
            }
        }
    }

    public void OnGUI()
    {
        if (currentMessageView != null)
        {
            SwrveOrientation newOrientation = GetDeviceOrientation();
            currentMessageView.Render(newOrientation);
        }
    }

    private void ProcessButtonUp(SwrveButtonClickResult clickedResult)
    {
        if (clickedResult == null)
        {
            QueueMessagePageViewEvent();
        }
        else
        {
            SwrveButton button = clickedResult.Button;
            SwrveLog.Log("Clicked button " + button.ActionType);
            try
            {
                if (button.ActionType == SwrveActionType.Install)
                {
                    ProcessButtonUpInstall(clickedResult);
                }
                else if (button.ActionType == SwrveActionType.Custom)
                {
                    ProcessButtonUpCustom(clickedResult);
                }
                else if (button.ActionType == SwrveActionType.CopyToClipboard)
                {
                    ProcessButtonUpCopyToClipboard(clickedResult);
                }
                else if (button.ActionType == SwrveActionType.PageLink)
                {
                    ProcessButtonUpPageNav(clickedResult);
                }
                else if (button.ActionType == SwrveActionType.Dismiss)
                {
                    ProcessButtonUpDismiss(clickedResult);
                }
                else // else default to dismissing the message
                {
                    DismissMessage();
                }
            }
            catch (Exception exp)
            {
                SwrveLog.LogError("Error processing the clicked button: " + exp.Message);
                DismissMessage();
            }
        }
    }

    private void ProcessButtonUpInstall(SwrveButtonClickResult clickedResult)
    {
        QueueMessageClickEvent(clickedResult.Button);
        string appId = clickedResult.Button.AppId.ToString();
        if (appStoreLinks.ContainsKey(appId))
        {
            string appStoreUrl = appStoreLinks[appId];
            if (!string.IsNullOrEmpty(appStoreUrl))
            {
                OpenURL(appStoreUrl); // Open app store
            }
            else
            {
                SwrveLog.LogError("No app store url for app " + appId);
            }
        }
        else
        {
            SwrveLog.LogError("Install button app store url empty!");
        }
        DismissMessage();
    }

    private void ProcessButtonUpCustom(SwrveButtonClickResult clickedResult)
    {
        QueueMessageClickEvent(clickedResult.Button);
        string buttonAction = clickedResult.ResolvedAction;
        if (config.InAppMessageConfig.CustomButtonListener != null)
        {
            config.InAppMessageConfig.CustomButtonListener.OnAction(buttonAction); // Launch custom button listener
        }
        else
        {
            SwrveLog.Log("No custom button listener, treating action as URL");
            if (!string.IsNullOrEmpty(buttonAction))
            {
                OpenURL(buttonAction);
            }
        }
        DismissMessage();
    }

    private void ProcessButtonUpCopyToClipboard(SwrveButtonClickResult clickedResult)
    {
        QueueMessageClickEvent(clickedResult.Button);
        string buttonAction = clickedResult.ResolvedAction;
        SwrveLog.Log("Copying text to clipboard");
        if (!string.IsNullOrEmpty(buttonAction))
        {
#if UNITY_ANDROID || UNITY_IOS
            CopyToClipboard(buttonAction);
#else
            SwrveLog.Log("Copy to clipboard is only implemented for Android and iOS");
#endif
        }

        if (config.InAppMessageConfig.ClipboardButtonListener != null)
        {
            config.InAppMessageConfig.ClipboardButtonListener.OnAction(buttonAction); // Launch custom button listener
        }
        DismissMessage();
    }

    private void ProcessButtonUpPageNav(SwrveButtonClickResult clickedResult)
    {
        if (clickedResult.Button == null || currentMessageView == null)
        {
            return;
        }

        QueueMessagePageNavEvent(clickedResult.Button);
        long pageId = long.Parse(clickedResult.Button.Action);
        currentMessageView.PageNavigation(pageId);
    }

    private void ProcessButtonUpDismiss(SwrveButtonClickResult clickedResult)
    {
        QueueMessageDismissEvent(clickedResult.Button);
        DismissMessage();
    }

    protected virtual void OpenURL(string url)
    {
        Application.OpenURL(url);
    }

    protected void SetMessageMinDelayThrottle()
    {
        this.showMessagesAfterDelay = SwrveHelper.GetNow() + TimeSpan.FromSeconds(this.minDelayBetweenMessage);
    }

    public void ConversationClosed()
    {
        // Callback method from the native layer to let us know that the campaigns are closed
        if (currentMessageView == null)
        {
            // There's also no IAM being displayed so go ahead and cycle to next campaign
            HandleNextCampaign();
        }
    }

    internal protected void ShowCampaign(SwrveBaseCampaign campaign, bool isQueued, Dictionary<string, string> properties)
    {
        ShowCampaign(campaign, isQueued, GetDeviceOrientation(), properties);
    }

    private void ShowCampaign(SwrveBaseCampaign campaign, bool isQueued, SwrveOrientation orientation, Dictionary<string, string> properties)
    {
        if (IsMessageDisplaying() == false && IsConversationDisplaying() == false && applicationPaused == false)
        {
            if (campaign is SwrveInAppCampaign)
            {
                SwrveMessage inAppMessage = ((SwrveInAppCampaign)campaign).Message;
                if (inAppMessage.SupportsOrientation(orientation))
                {
                    Container.StartCoroutine(LaunchMessage(inAppMessage, properties));
                }
            }
            else if (campaign is SwrveConversationCampaign)
            {
                Container.StartCoroutine(LaunchConversation(
                                             ((SwrveConversationCampaign)campaign).Conversation
                                         ));
            }
            else if (campaign is SwrveEmbeddedCampaign)
            {
                SwrveEmbeddedMessage embeddedMessage = ((SwrveEmbeddedCampaign)campaign).Message;
                if (config.EmbeddedMessageConfig.EmbeddedMessageListener != null)
                {
                    config.EmbeddedMessageConfig.EmbeddedMessageListener.OnMessage(embeddedMessage, properties);
                }
                else
                {
                    SwrveLog.LogError("Could not find a valid EmbeddedMessageListener defined as part of the EmbeddedMessageConfig, be sure that you did set it as parf of the SDK initialisation");
                }
            }
        }
        else if (isQueued)
        {
            this.campaignDisplayQueue.Add(campaign);
        }
    }

    private void HandleNextCampaign()
    {
        if (this.campaignDisplayQueue.Count > 0)
        {
            SwrveBaseCampaign campaign = this.campaignDisplayQueue[0];
            this.campaignDisplayQueue.RemoveAt(0); // remove from queue before processing
            var properties = GetPersonalizationProperties(null);
            ShowCampaign(campaign, false, properties);
        }
    }

    private void AutoShowMessages()
    {
        // Don't do anything if we've already shown a message or if its too long after session start
        if (!autoShowMessagesEnabled)
        {
            return;
        }

        // Only execute if at least 1 call to the /user_content api endpoint has been completed
        if (!campaignsAndResourcesInitialized || campaigns == null || campaigns.Count == 0)
        {
            return;
        }

        SwrveBaseMessage baseMessage = null;
        // Process only Conversation campaign types first
        for (int ci = 0; ci < campaigns.Count; ci++)
        {
            if (!(campaigns[ci] is SwrveConversationCampaign))
            {
                continue;
            }

            SwrveConversationCampaign campaign = (SwrveConversationCampaign)campaigns[ci];
            if (campaign.CanTrigger(DefaultAutoShowMessagesTrigger) && campaign.CheckImpressions())
            {
                SwrveConversation conversation = GetConversationForEvent(DefaultAutoShowMessagesTrigger);
                if (campaign.AreAssetsReady(null))
                {
                    autoShowMessagesEnabled = false;
                    Container.StartCoroutine(LaunchConversation(conversation));
                    baseMessage = conversation;
                    break;
                }
            }
        }

        if (baseMessage == null)
        {
            // Process SwrveInAppCampaign and SwrveEmbeddedCampaign.
            for (int ci = 0; ci < campaigns.Count; ci++)
            {
                if (!(campaigns[ci] is SwrveInAppCampaign || campaigns[ci] is SwrveEmbeddedCampaign))
                {
                    continue;
                }

                SwrveBaseCampaign campaign = campaigns[ci];
                if (campaign.CanTrigger(DefaultAutoShowMessagesTrigger) && campaign.CheckImpressions())
                {
                    SwrveBaseMessage message = GetBaseMessageForEvent(DefaultAutoShowMessagesTrigger);
                    if (message != null)
                    {
                        if (message is SwrveMessage)
                        {
                            // Handle the respective message type "SwrveMessage"
                            if (config.InAppMessageConfig.TriggeredMessageListener != null)
                            {
                                // They are using a custom listener
                                if (message != null && message is SwrveMessage)
                                {
                                    autoShowMessagesEnabled = false;
                                    config.InAppMessageConfig.TriggeredMessageListener.OnMessageTriggered((SwrveMessage)message);
                                    baseMessage = message;
                                }
                            }
                            else
                            {
                                if (currentMessageView == null)
                                {
                                    autoShowMessagesEnabled = false;
                                    Dictionary<string, string> properties = GetPersonalizationProperties(null);
                                    Container.StartCoroutine(LaunchMessage(message, properties));
                                    baseMessage = message;
                                }
                            }
                            break;
                        }
                        else if (message is SwrveEmbeddedMessage)
                        {
                            // Handle the respective message type "SwrveEmbeddedMessage"
                            if (currentMessageView == null)
                            {
                                if (config.EmbeddedMessageConfig.EmbeddedMessageListener != null)
                                {
                                    autoShowMessagesEnabled = false;
                                    Dictionary<string, string> properties = GetPersonalizationProperties(null);
                                    config.EmbeddedMessageConfig.EmbeddedMessageListener.OnMessage((SwrveEmbeddedMessage)message, properties);
                                    baseMessage = message;
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    private IEnumerator LaunchMessage(SwrveBaseMessage message, Dictionary<string, string> personalizationProperties)
    {
        if (message != null && message is SwrveMessage)
        {
            SwrveOrientation currentOrientation = GetDeviceOrientation();
            SwrveMessageFormat selectedFormat = ((SwrveMessage)message).GetFormat(currentOrientation);
            if (selectedFormat != null)
            {
                // Check if the templating on the message can be resolved
                SwrveMessageTextTemplatingResolver resolver = new SwrveMessageTextTemplatingResolver();
                if (resolver.ResolveTemplating((SwrveMessage)message, personalizationProperties))
                {
                    // Temporarily set this as the message that will be shown if everything goes well
                    currentMessageView = CreateSwrveInAppMessageView(swrveTemporaryPath, Container, selectedFormat, config.InAppMessageConfig, resolver);
                    CoroutineReference<bool> wereAllLoaded = new CoroutineReference<bool>(false);
                    yield return Container.StartCoroutine(currentMessageView.PreloadAndDisplay(wereAllLoaded));

                    if (wereAllLoaded.Value())
                    {
                        MessageWasShownToUser(selectedFormat);
                    }
                    else
                    {
                        SwrveLog.LogError("Could not preload all the assets for message " + message.Id);
                        currentMessageView = null;
                    }
                }
            }
            else
            {
                SwrveLog.LogError("Could not get a format for the current orientation: " + currentOrientation.ToString());
            }
        }
    }

    protected virtual SwrveInAppMessageView CreateSwrveInAppMessageView(string swrveTemporaryPath, MonoBehaviour container, SwrveMessageFormat format, SwrveInAppMessageConfig inAppConfig,
            SwrveMessageTextTemplatingResolver templatingResolver)
    {
        return new SwrveInAppMessageView(swrveTemporaryPath, container, format, inAppConfig, templatingResolver);
    }

    private bool IsValidMessageCenter(SwrveBaseCampaign campaign, SwrveOrientation orientation, Dictionary<string, string> personalizationProperties)
    {
        return campaign.MessageCenter
               && campaign.Status != SwrveCampaignState.Status.Deleted
               && campaign.IsActive()
               && campaign.SupportsOrientation(orientation)
               && campaign.AreAssetsReady(personalizationProperties);
    }

    private IEnumerator LaunchConversation(SwrveConversation conversation)
    {
        if (null != conversation)
        {
            yield return null;
            ShowConversation(conversation.Conversation);
            ConversationWasShownToUser(conversation);
        }
    }

    public void ConversationWasShownToUser(SwrveConversation conversation)
    {
        SetMessageMinDelayThrottle();

        if (null != conversation.Campaign)
        {
            conversation.Campaign.WasShownToUser();
            SaveCampaignData(conversation.Campaign);
        }
    }

    private void NoMessagesWereShown(string eventName, IDictionary<string, string> eventPayload, string reason)
    {
        SwrveLog.Log("Not showing message for " + eventName + ": " + reason);
        //SwrveQaUser.Instance.CampaignTriggered(eventName, eventPayload, false, reason, null);
    }

    /* Returns the path to the downloaded asset */
    /*public string AssetPath(string fileName)
    {
        return GetTemporaryPathFileName(fileName);
    }*/

    /*private IEnumerator PreloadFormatAssets(SwrveInAppMessageView view, CoroutineReference<bool> wereAllLoaded)
    {
        SwrveLog.Log("Preloading format");
        bool allLoaded = true;

        for (int ii = 0; ii < format.Images.Count; ii++) {
            SwrveImage image = format.Images[ii];
            if (image.Texture == null && !string.IsNullOrEmpty(image.File)) {
                SwrveLog.Log("Preloading image file " + image.File);
                CoroutineReference<Texture2D> result = new CoroutineReference<Texture2D>();
                yield return StartTask("LoadAsset", LoadAsset(image.File, result));
                if (result.Value() != null) {
                    image.Texture = result.Value();
                } else {
                    allLoaded = false;
                }
            }
        }

        for (int bi = 0; bi < format.Buttons.Count; bi++) {
            SwrveButton button = format.Buttons[bi];
            if (button.Texture == null && !string.IsNullOrEmpty(button.Image)) {
                SwrveLog.Log("Preloading button image " + button.Image);
                CoroutineReference<Texture2D> result = new CoroutineReference<Texture2D>();
                yield return StartTask("LoadAsset", LoadAsset(button.Image, result));
                if (result.Value() != null) {
                    button.Texture = result.Value();
                } else {
                    allLoaded = false;
                }
            }
        }

        wereAllLoaded.Value(allLoaded);
        TaskFinished("PreloadFormatAssets");
    }*/

    private bool HasShowTooManyMessagesAlready()
    {
        return (messagesLeftToShow <= 0);
    }

    private bool IsTooSoonToShowMessageAfterLaunch(DateTime now)
    {
        return now < showMessagesAfterLaunch;
    }

    private bool IsTooSoonToShowMessageAfterDelay(DateTime now)
    {
        return now < showMessagesAfterDelay;
    }

    protected virtual void ProcessCampaigns(Dictionary<string, object> root, bool loadingPreviousCampaignState)
    {
        List<SwrveBaseCampaign> newCampaigns = new List<SwrveBaseCampaign>();
        HashSet<SwrveAssetsQueueItem> assetsQueue = new HashSet<SwrveAssetsQueueItem>();
        // this queue will be merged to above to ensure it's at the front for downloading
        HashSet<SwrveAssetsQueueItem> priorityAssetsQueue = new HashSet<SwrveAssetsQueueItem>();

        try
        {
            // Stop if we got an empty json
            if (root != null && root.ContainsKey("version"))
            {
                int version = MiniJsonHelper.GetInt(root, "version");
                if (version == CampaignResponseVersion)
                {
                    UpdateCdnPaths(root);

                    // App data
                    Dictionary<string, object> appData = (Dictionary<string, object>)root["game_data"];
                    Dictionary<string, object>.Enumerator appDataEnumerator = appData.GetEnumerator();
                    while (appDataEnumerator.MoveNext())
                    {
                        string appId = appDataEnumerator.Current.Key;
                        if (appStoreLinks.ContainsKey(appId))
                        {
                            appStoreLinks.Remove(appId);
                        }
                        Dictionary<string, object> appAppStore = (Dictionary<string, object>)appData[appId];
                        if (appAppStore != null && appAppStore.ContainsKey("app_store_url"))
                        {
                            object appStoreLink = appAppStore["app_store_url"];
                            if (appStoreLink != null && appStoreLink is string)
                            {
                                appStoreLinks.Add(appId, (string)appStoreLink);
                            }
                        }
                    }

                    // Rules
                    Dictionary<string, object> rules = (Dictionary<string, object>)root["rules"];
                    int delayFirstMessage = (rules.ContainsKey("delay_first_message")) ? MiniJsonHelper.GetInt(rules, "delay_first_message") : DefaultDelayFirstMessage;
                    long maxShows = (rules.ContainsKey("max_messages_per_session")) ? MiniJsonHelper.GetLong(rules, "max_messages_per_session") : DefaultMaxShows;
                    int minDelay = (rules.ContainsKey("min_delay_between_messages")) ? MiniJsonHelper.GetInt(rules, "min_delay_between_messages") : DefaultMinDelay;

                    DateTime now = SwrveHelper.GetNow();
                    this.minDelayBetweenMessage = minDelay;
                    this.messagesLeftToShow = maxShows;
                    this.showMessagesAfterLaunch = initialisedTime + TimeSpan.FromSeconds(delayFirstMessage);

                    SwrveLog.Log("App rules OK: Delay Seconds: " + delayFirstMessage + " Max shows: " + maxShows);
                    SwrveLog.Log("Time is " + now.ToString() + " show messages after " + this.showMessagesAfterLaunch.ToString());

                    // Campaigns
                    IList<object> jsonCampaigns = (List<object>)root["campaigns"];
                    List<SwrveQaUserCampaignInfo> qaUserCampaignInfoList = new List<SwrveQaUserCampaignInfo>();

                    // Call Personalization Once to get back properties for resolving dynamic images for campaigns.
                    Dictionary<string, string> personalizationProperties = GetPersonalizationProperties(null);

                    for (int i = 0, j = jsonCampaigns.Count; i < j; i++)
                    {
                        Dictionary<string, object> campaignData = (Dictionary<string, object>)jsonCampaigns[i];
                        SwrveBaseCampaign campaign = SwrveBaseCampaign.LoadFromJSON(SwrveAssetsManager, campaignData, initialisedTime, config.InAppMessageConfig.DefaultBackgroundColor, qaUserCampaignInfoList);
                        if (campaign == null)
                        {
                            continue;
                        }

                        // if the Campaign triggers include the DefaultAutoShowMessagesTrigger
                        bool isAnAutoShowCampaign = false;
                        List<SwrveTrigger> triggers = campaign.GetTriggers();

                        for (int t = 0; t < triggers.Count; t++)
                        {
                            if (string.Equals(triggers[t].GetEventName(), DefaultAutoShowMessagesTrigger))
                            {
                                isAnAutoShowCampaign = true;
                                break;
                            }
                        }

                        if (campaign is SwrveConversationCampaign)
                        {
                            SwrveConversationCampaign conversationCampaign = (SwrveConversationCampaign)campaign;
                            if (isAnAutoShowCampaign)
                            {
                                priorityAssetsQueue.UnionWith(conversationCampaign.Conversation.ConversationAssets);
                            }
                            else
                            {
                                assetsQueue.UnionWith(conversationCampaign.Conversation.ConversationAssets);
                            }
                            qaUserCampaignInfoList.Add(new SwrveQaUserCampaignInfo(campaign.Id, conversationCampaign.Conversation.Id, conversationCampaign.GetCampaignType(), false));
                        }
                        else if (campaign is SwrveInAppCampaign)
                        {

                            SwrveInAppCampaign messageCampaign = (SwrveInAppCampaign)campaign;
                            if (isAnAutoShowCampaign)
                            {
                                priorityAssetsQueue.UnionWith(messageCampaign.GetImageAssets(personalizationProperties));
                            }
                            else
                            {
                                assetsQueue.UnionWith(messageCampaign.GetImageAssets(personalizationProperties));
                            }
                            qaUserCampaignInfoList.Add(new SwrveQaUserCampaignInfo(campaign.Id, messageCampaign.Message.Id, messageCampaign.GetCampaignType(), false));
                        }
                        else if (campaign is SwrveEmbeddedCampaign)
                        {
                            SwrveEmbeddedCampaign embeddedCampaign = (SwrveEmbeddedCampaign)campaign;
                            qaUserCampaignInfoList.Add(new SwrveQaUserCampaignInfo(campaign.Id, embeddedCampaign.Message.Id, embeddedCampaign.GetCampaignType(), false));
                        }

                        if (loadingPreviousCampaignState)
                        {
                            SwrveCampaignState campaignState = null;
                            campaignsState.TryGetValue(campaign.Id, out campaignState);
                            if (campaignState != null)
                            {
                                campaign.State = campaignState;
                            }
                            else
                            {
                                if (campaignSettings != null)
                                {
                                    campaign.State = new SwrveCampaignState(campaign.Id, campaignSettings);
                                }
                            }
                        }
                        campaignsState[campaign.Id] = campaign.State;
                        newCampaigns.Add(campaign);
                    }

                    SwrveQaUser.CampaignsDownloaded(qaUserCampaignInfoList);
                }
            }
        }
        catch (Exception exp)
        {
            SwrveLog.LogError("Could not process campaigns: " + exp.ToString());
        }

        StartTask("SwrveAssetsManager.DownloadAssets", this.SwrveAssetsManager.DownloadAssets(priorityAssetsQueue, assetsQueue, AutoShowMessages));
        campaigns = new List<SwrveBaseCampaign>(newCampaigns);
    }

    internal void UpdateCdnPaths(Dictionary<string, object> root)
    {
        if (root.ContainsKey("cdn_root"))
        {
            string cdnRoot = (string)root["cdn_root"];
            this.SwrveAssetsManager.CdnImages = cdnRoot;
            SwrveLog.Log("CDN URL " + cdnRoot);
        }
        else if (root.ContainsKey("cdn_paths"))
        {
            Dictionary<string, object> cdnPaths = (Dictionary<string, object>)root["cdn_paths"];
            string cdnImages = (string)cdnPaths["message_images"];
            string cdnFonts = (string)cdnPaths["message_fonts"];
            this.SwrveAssetsManager.CdnImages = cdnImages;
            this.SwrveAssetsManager.CdnFonts = cdnFonts;
            SwrveLog.Log("CDN URL images: " + cdnImages + " fonts: " + cdnFonts);
        }
    }

    internal ISwrveAssetsManager GetSwrveAssetsManager()
    {
        return this.SwrveAssetsManager;
    }

    internal void DownloadAnyMissingAssets()
    {
        StartTask("SwrveAssetsManager.DownloadMissingAssets", this.SwrveAssetsManager.DownloadAnyMissingAssets(AutoShowMessages));
    }

    private void LoadResourcesAndCampaigns()
    {
        if (!IsAlive())
        {
            return;
        }
        try
        {
            if (!campaignsConnecting)
            {
                if (!config.AutoDownloadCampaignsAndResources)
                {

                    if (campaignsAndResourcesLastRefreshed != 0)
                    {
                        long currentTime = GetSessionTime();
                        if (currentTime < campaignsAndResourcesLastRefreshed)
                        {
                            SwrveLog.Log("Request to retrieve campaign and user resource data was rate-limited.");
                            return;
                        }
                    }

                    // Set next time gate
                    campaignsAndResourcesLastRefreshed = GetSessionTime() + (long)(campaignsAndResourcesFlushFrequency * 1000);
                }

                campaignsConnecting = true;
                var getRequest = GetCampaignsAndResourcesUrl(resourcesAndCampaignsUrl);
                StartTask("GetCampaignsAndResources_Coroutine", GetCampaignsAndResources_Coroutine(getRequest.ToString()));
            }
        }
        catch (Exception e)
        {
            SwrveLog.LogError("Error while trying to get user resources and campaign data: " + e);
        }
    }

    internal protected string GetCampaignsAndResourcesUrl(string endPoint)
    {
        float dpi = (Screen.dpi == 0) ? DefaultDPI : Screen.dpi;
        string deviceName = GetDeviceModel();
        string os = GetPlatformOS();
        string deviceType = GetDeviceType();
        string osVersion = SystemInfo.operatingSystem;
        StringBuilder campaignUrl = new StringBuilder(endPoint)
        .AppendFormat("?user={0}&api_key={1}&app_version={2}&joined={3}", SwrveHelper.EscapeURL(this.UserId), ApiKey, SwrveHelper.EscapeURL(GetAppVersion()), userInitTimeSeconds);

        if (config.MessagingEnabled)
        {
            campaignUrl.AppendFormat("&version={0}&orientation={1}&language={2}&app_store={3}&embedded_campaign_version={4}&in_app_version={5}&device_width={6}&device_height={7}&device_dpi={8}&os_version={9}&device_name={10}&os={11}&device_type={12}",
                                     CampaignEndpointVersion, config.Orientation.ToString().ToLower(), Language, config.AppStore, EmbeddedCampaignVersion,
                                     InAppMessageCampaignVersion, deviceWidth, deviceHeight, dpi, SwrveHelper.EscapeURL(osVersion), SwrveHelper.EscapeURL(deviceName),
                                     os, deviceType);
        }
        if (config.ConversationsEnabled)
        {
            campaignUrl.AppendFormat("&conversation_version={0}", this.conversationVersion);
        }

        if (config.ABTestDetailsEnabled)
        {
            campaignUrl.AppendFormat("&ab_test_details=1");
        }

        if (!string.IsNullOrEmpty(lastETag))
        {
            campaignUrl.AppendFormat("&etag={0}", lastETag);
        }

        return campaignUrl.ToString();
    }

    private string GetDeviceModel()
    {
        string deviceModel = SystemInfo.deviceModel;
        if (string.IsNullOrEmpty(deviceModel))
        {
            deviceModel = "ModelUnknown";
        }
        return deviceModel;
    }

    private string GetPlatformOS()
    {
#if UNITY_ANDROID
        return SwrveSDK.GetAndroidPlatformOS();
#elif UNITY_IOS
        return SwrveSDK.GetiOSPlatformOS();
#elif UNITY_STANDALONE_WIN
        return "pc";
#elif UNITY_STANDALONE_LINUX
        return "linux";
#elif UNITY_STANDALONE_OSX
        return "osx";
#else
        return Application.platform.ToString().ToLower();
#endif
    }

    private string GetDeviceType()
    {
#if UNITY_ANDROID
        return SwrveSDK.GetAndroidDeviceType();
#elif UNITY_IOS
        return SwrveSDK.GetOSDeviceType();
#elif UNITY_STANDALONE_WIN
        return "desktop";
#elif UNITY_STANDALONE_LINUX
        return "desktop";
#elif UNITY_STANDALONE_OSX
        return "desktop";
#else
        return "mobile";
#endif
    }

    protected virtual IEnumerator GetCampaignsAndResources_Coroutine(string getRequest)
    {
        SwrveLog.Log("Campaigns and resources request: " + getRequest);
        yield return Container.StartCoroutine(restClient.Get(getRequest, delegate (RESTResponse response)
        {
            if (response.Error == WwwDeducedError.NoError)
            {
                // Save etag for future requests
                string etag = null;
                if (response.Headers != null)
                {
                    response.Headers.TryGetValue("ETAG", out etag);
                    if (!string.IsNullOrEmpty(etag))
                    {
                        lastETag = etag;
                        storage.Save(ResourcesCampaignTagSave, etag, this.UserId);
                    }
                }

                if (!string.IsNullOrEmpty(response.Body))
                {
                    Dictionary<string, object> root = (Dictionary<string, object>)Json.Deserialize(response.Body);

                    if (root != null && root.Count > 0)
                    {
                        // Check for qa must be first.
                        Dictionary<string, object> qaUserDictionary = null;
                        bool loadPreviousCampaignState = true;
                        if (root.ContainsKey("qa"))
                        {
                            // qa moved to root in v7 of endpoint.
                            qaUserDictionary = (Dictionary<string, object>)root["qa"];
                            bool wasPreviouslyResetDevice = SwrveQaUser.Instance.resetDevice;
                            bool resetDevice = (bool)qaUserDictionary["reset_device_state"];
                            if (!wasPreviouslyResetDevice && resetDevice)
                            {
                                loadPreviousCampaignState = false;
                            }
                        }

                        UpdateQaUser(qaUserDictionary);

                        // Save flush settings
                        if (root.ContainsKey("flush_frequency"))
                        {
                            string strFlushFrequency = MiniJsonHelper.GetString(root, "flush_frequency");
                            if (!string.IsNullOrEmpty(strFlushFrequency))
                            {
                                if (float.TryParse(strFlushFrequency, out campaignsAndResourcesFlushFrequency))
                                {
                                    campaignsAndResourcesFlushFrequency /= 1000;
                                    storage.Save(ResourcesCampaignFlushFrequencySave, strFlushFrequency, this.UserId);
                                }
                            }
                        }
                        if (root.ContainsKey("flush_refresh_delay"))
                        {
                            string strFlushRefreshDelay = MiniJsonHelper.GetString(root, "flush_refresh_delay");
                            if (!string.IsNullOrEmpty(strFlushRefreshDelay))
                            {
                                if (float.TryParse(strFlushRefreshDelay, out campaignsAndResourcesFlushRefreshDelay))
                                {
                                    campaignsAndResourcesFlushRefreshDelay /= 1000;
                                    storage.Save(ResourcesCampaignFlushDelaySave, strFlushRefreshDelay, this.UserId);
                                }
                            }
                        }

                        if (root.ContainsKey("real_time_user_properties"))
                        {
                            // Process realtime user properties
                            Dictionary<string, object> realtimeUserPropsDate = (Dictionary<string, object>)root["real_time_user_properties"];
                            string realtimeUserPropsJson = SwrveUnityMiniJSON.Json.Serialize(realtimeUserPropsDate);
                            storage.SaveSecure(RealtimeUserPropertiesSave, realtimeUserPropsJson, this.UserId);
                            realtimeUserProperties = NormalizeJson(realtimeUserPropsDate);
                            realtimeUserPropertiesRaw = realtimeUserPropsJson;
                        }

                        if (root.ContainsKey("user_resources"))
                        {
                            // Process user resources
                            IList<object> userResourcesData = (IList<object>)root["user_resources"];
                            string userResourcesJson = SwrveUnityMiniJSON.Json.Serialize(userResourcesData);
                            storage.SaveSecure(AbTestUserResourcesSave, userResourcesJson, this.UserId);
                            userResources = ProcessUserResources(userResourcesData);
                            userResourcesRaw = userResourcesJson;

                            if (campaignsAndResourcesInitialized)
                            {
                                NotifyUpdateUserResources();
                            }
                        }

                        if (root.ContainsKey("campaigns"))
                        {
                            Dictionary<string, object> campaignsData = (Dictionary<string, object>)root["campaigns"];
                            if (config.MessagingEnabled)
                            {
                                string campaignsJson = SwrveUnityMiniJSON.Json.Serialize(campaignsData);
                                SaveCampaignsCache(campaignsJson);

                                AutoShowMessages();

                                ProcessCampaigns(campaignsData, loadPreviousCampaignState);
                            }

                            if (config.ABTestDetailsEnabled && campaignsData.ContainsKey("ab_test_details"))
                            {
                                Dictionary<string, object> abTestDetailsData = (Dictionary<string, object>)campaignsData["ab_test_details"];
                                ResourceManager.SetABTestDetailsFromJSON(abTestDetailsData);
                            }
                        }
                    }
                }
            }
            else
            {
                SwrveLog.LogError("Resources and campaigns request error: " + response.Error.ToString() + ":" + response.Body);
            }

            if (!campaignsAndResourcesInitialized)
            {
                campaignsAndResourcesInitialized = true;

                // Only called first time API call returns - whether failed or successful, whether new campaigns were returned or not;
                // this ensures that if API call fails or there are no changes, we call autoShowMessages with cached campaigns
                AutoShowMessages();

                // Invoke listeners once to denote that the first attempt at downloading has finished
                // independent of whether the resources or campaigns have changed from cached values
                NotifyUpdateUserResources();
            }

            campaignsConnecting = false;
            TaskFinished("GetCampaignsAndResources_Coroutine");
        }));
    }

    private void SaveCampaignsCache(string cacheContent)
    {
        try
        {
            if (cacheContent == null)
            {
                cacheContent = string.Empty;
            }
            storage.SaveSecure(CampaignsSave, cacheContent, this.UserId);
        }
        catch (Exception e)
        {
            SwrveLog.LogError("Error while saving campaigns to the cache " + e);
        }
    }

    internal void SaveExternalCampaignCache(string cacheContent)
    {
        try
        {
            if (cacheContent == null)
            {
                cacheContent = string.Empty;
            }
            storage.SaveSecure(LastExternalCampaignSave, cacheContent, this.UserId);
        }
        catch (Exception e)
        {
            SwrveLog.LogError("Error while saving last external campaign to the cache " + e);
        }
    }

    private void SaveCampaignData(SwrveBaseCampaign campaign)
    {
        try
        {
            // Move from SwrveCampaignState to the dictionary
            campaignSettings["Impressions" + campaign.Id] = campaign.Impressions;
            campaignSettings["Status" + campaign.Id] = campaign.Status.ToString();

            string serializedCampaignSettings = Json.Serialize(campaignSettings);
            storage.Save(CampaignsSettingsSave, serializedCampaignSettings, this.UserId);
        }
        catch (Exception e)
        {
            SwrveLog.LogError("Error while trying to save campaign settings " + e);
        }
    }

    private void LoadTalkData()
    {
        // Load campaign settings
        try
        {
            string campaignSettingsStr;
            string loadedData = storage.Load(CampaignsSettingsSave, this.UserId);
            if (loadedData != null && loadedData.Length != 0)
            {
                if (ResponseBodyTester.TestUTF8(loadedData, out campaignSettingsStr))
                {
                    campaignSettings = (Dictionary<string, object>)Json.Deserialize(campaignSettingsStr);
                }
            }
        }
        catch (Exception e)
        {
            SwrveLog.LogWarning("Could not read default campaign settings." + e.ToString());
        }

        // Load campaigns
        try
        {
            string loadedData = storage.LoadSecure(CampaignsSave, this.UserId);
            if (!string.IsNullOrEmpty(loadedData))
            {
                string campaignsCandidate = null;
                if (ResponseBodyTester.TestUTF8(loadedData, out campaignsCandidate))
                {
                    Dictionary<string, object> campaignsData = (Dictionary<string, object>)Json.Deserialize(campaignsCandidate);
                    bool loadingPreviousCampaignState = !SwrveQaUser.Instance.resetDevice;
                    ProcessCampaigns(campaignsData, loadingPreviousCampaignState);
                }
                else
                {
                    SwrveLog.Log("Failed to parse campaigns cache");
                    InvalidateETag();
                }
            }
            else
            {
                InvalidateETag();
            }
        }
        catch (Exception e)
        {
            SwrveLog.LogWarning("Could not read campaigns from cache, using default (" + e.ToString() + ")");
            InvalidateETag();
        }
    }

    private void LoadABTestDetails()
    {
        // Load ABTest details
        try
        {
            string loadedData = storage.LoadSecure(CampaignsSave, this.UserId);
            if (!string.IsNullOrEmpty(loadedData))
            {
                string campaignsCandidate = null;
                if (ResponseBodyTester.TestUTF8(loadedData, out campaignsCandidate))
                {
                    Dictionary<string, object> campaignsData = (Dictionary<string, object>)Json.Deserialize(campaignsCandidate);
                    if (campaignsData.ContainsKey("ab_test_details"))
                    {
                        Dictionary<string, object> abTestDetails = (Dictionary<string, object>)campaignsData["ab_test_details"];
                        ResourceManager.SetABTestDetailsFromJSON(abTestDetails);
                    }
                }
                else
                {
                    SwrveLog.Log("Failed to parse AB test details cache");
                }
            }
        }
        catch (Exception e)
        {
            SwrveLog.LogWarning("Could not read ABTest details from cache, using default (" + e.ToString() + ")");
        }
    }

    internal protected Dictionary<string, object> LoadLastExternalCampaign()
    {
        // Load the last external campaign that occurred on this device with this user
        Dictionary<string, object> campaignData = null;
        try
        {
            string loadedData = storage.LoadSecure(LastExternalCampaignSave, this.UserId);
            if (!string.IsNullOrEmpty(loadedData))
            {
                string campaignsCandidate = null;
                if (ResponseBodyTester.TestUTF8(loadedData, out campaignsCandidate))
                {
                    campaignData = (Dictionary<string, object>)Json.Deserialize(campaignsCandidate);
                }
                else
                {
                    SwrveLog.Log("Failed to parse campaigns cache");
                }
            }
            else
            {
                SwrveLog.Log("No previous external Campaign Found");
            }
        }
        catch (Exception exception)
        {
            SwrveLog.LogWarning("Could not read campaigns from cache: " + exception);
        }

        return campaignData;
    }

    public void SendPushEngagedEvent(string pushId)
    {
        if (("0" == pushId) || (pushId != lastPushEngagedId))
        {
            lastPushEngagedId = pushId;
            NamedEventInternal("Swrve.Messages.Push-" + pushId + ".engaged", null, false);
            SwrveLog.Log("Got Swrve notification with ID " + pushId);
            SendQueuedEvents();
        }
    }

    private IEnumerator WaitASecondAndSendEvents_Coroutine()
    {
        yield return new WaitForSeconds(1);
        SendQueuedEvents();
    }

    protected int ConvertInt64ToInt32Hack(Int64 val)
    {
        // SWRVE-5613
        // Hack to solve Unity issue where the id is an int64
        // with a random high part and the int32 value we
        // need in the lower part.
        return (int)(val & 0xFFFFFFFF);
    }

    protected virtual ICarrierInfo GetCarrierInfoProvider()
    {
        return deviceCarrierInfo;
    }

    public string GetAppVersion()
    {
        if (string.IsNullOrEmpty(config.AppVersion))
        {
            setNativeAppVersion();
        }
        return config.AppVersion;
    }

    private void ShowConversation(string conversation)
    {
#if UNITY_EDITOR
        if (null != ConversationEditorCallback)
        {
            ConversationEditorCallback(conversation);
            return;
        }
#endif
        showNativeConversation(conversation);
    }

    protected void StartCampaignsAndResourcesTimer()
    {
        if (!config.AutoDownloadCampaignsAndResources)
        {
            return;
        }

        RefreshUserResourcesAndCampaigns();

        // Start repeating timer to auto-send events after a specified frequency. eg: 60s
        StartCheckForCampaignsAndResources();

        // Call refresh once after refresh delay (eg: 5s) to ensure campaigns are reloaded after initial events have been sent.
        Container.StartCoroutine(WaitAndRefreshResourcesAndCampaigns_Coroutine(campaignsAndResourcesFlushRefreshDelay));
    }

    protected void DisableAutoShowAfterDelay()
    {
        // Start timer to disable autoshow
        Container.StartCoroutine(DisableAutoShowAfterDelay_Coroutine());
    }

    private IEnumerator DisableAutoShowAfterDelay_Coroutine()
    {
        yield return new WaitForSeconds(config.AutoShowMessagesMaxDelay);
        autoShowMessagesEnabled = false;
    }

    private string GetNativeDetails()
    {
        Dictionary<string, object> currentDetails = new Dictionary<string, object> {
            {"sdkVersion", SwrveSDK.SdkVersion},
            {"apiKey", apiKey},
            {"appId", appId},
            {"userId", this.UserId},
            {"isTrackingStateStopped", this.profileManager.GetTrackingState() == SwrveTrackingState.STOPPED},
            {"deviceId", GetDeviceUUID()},
            {"appVersion", GetAppVersion()},
            {"uniqueKey", GetUniqueKey()},
            {"deviceInfo", GetDeviceInfo()},
            {"batchUrl", "/1/batch"},
            {"eventsServer", eventsServer},
            {"contentServer", contentServer},
            {"httpTimeout", 60000},
            {"maxEventsPerFlush", 50},
            {"swrvePath", swrvePath},
            {"prefabName", prefabName},
            {"swrveTemporaryPath", swrveTemporaryPath},
            {"sigSuffix", SwrveFileStorage.SIGNATURE_SUFFIX}
        };

        string jsonString = Json.Serialize(currentDetails);

        return jsonString;
    }

    private void InitNative()
    {
        initNative();
        setNativeConversationVersion();
    }

    private void ProcessInfluenceData()
    {
        // Obtain influence data from native layer
        string influenceDataJson = GetInfluencedDataJsonPerPlatform();
        if (influenceDataJson != null)
        {
            List<object> influenceData = (List<object>)Json.Deserialize(influenceDataJson);
            if (influenceData != null)
            {
                for (int i = 0; i < influenceData.Count; i++)
                {
                    CheckInfluenceData((Dictionary<string, object>)influenceData[i]);
                }
            }
            else
            {
                SwrveLog.LogError("Could not parse influence data");
            }
        }
    }

    protected virtual string GetInfluencedDataJsonPerPlatform()
    {
        string influenceDataJson = null;
#if !UNITY_EDITOR
#if UNITY_ANDROID || UNITY_IOS
        influenceDataJson = GetInfluencedDataJson();
#endif
#endif
        return influenceDataJson;
    }

    public void QueueGenericCampaignEvent(Dictionary<string, object> eventData)
    {
        AppendEventToBuffer("generic_campaign_event", eventData, false);
    }

    public void CheckInfluenceData(Dictionary<string, object> influenceData)
    {
        if (influenceData != null)
        {
            if (influenceData.ContainsKey("trackingId") && influenceData.ContainsKey("maxInfluencedMillis") && influenceData.ContainsKey("silent"))
            {
                object trackingIdRaw = influenceData["trackingId"];
                object maxInfluencedMillisRaw = influenceData["maxInfluencedMillis"];
                object silentObj = influenceData["silent"];
                long maxInfluencedMillis = 0;
                if (maxInfluencedMillisRaw != null)
                {
                    if (maxInfluencedMillisRaw is long || maxInfluencedMillisRaw is Int32 || maxInfluencedMillisRaw is Int64)
                    {
                        maxInfluencedMillis = (long)maxInfluencedMillisRaw;
                    }
                }

                if (trackingIdRaw != null && trackingIdRaw is string && maxInfluencedMillis > 0)
                {
                    string trackingId = (string)trackingIdRaw;
                    // Check if the user was influenced
                    long now = SwrveHelper.GetMilliseconds();
                    if (now <= maxInfluencedMillis)
                    {
                        Dictionary<string, object> json = new Dictionary<string, object>();
                        json.Add("id", trackingId);
                        json.Add("campaignType", "push");
                        json.Add("actionType", "influenced");
                        Dictionary<string, string> payload = new Dictionary<string, string>();
                        payload.Add("delta", ((maxInfluencedMillis - now) / (100 * 60)).ToString());
                        payload.Add("silent", silentObj.ToString().ToLower());
                        json.Add("payload", payload);
                        AppendEventToBuffer("generic_campaign_event", json, false);
                        SwrveLog.Log("User was influenced by push " + trackingId);
                        Container.StartCoroutine(WaitASecondAndSendEvents_Coroutine());
                    }
                }
            }
            else
            {
                SwrveLog.Log("Couldn't find the influence data keys");
            }
        }
        else
        {
            SwrveLog.Log("Influence data is null");
        }
    }

    // Region with methods to help with the QaUser
    // So we don't need to pass to QA dependencies like "SwrveButton".
    #region SwrveQAHelperArea.

    private void UpdateQaUser(Dictionary<string, object> qaUserDictionary)
    {
        if (qaUserDictionary == null)
        {
            // If we do not receive the QA dic we do save as normal user.
            qaUserDictionary = new Dictionary<string, object>();
            qaUserDictionary.Add("reset_device_state", false);
            qaUserDictionary.Add("logging", false);
        }
        SwrveQaUser.SaveQaUser(qaUserDictionary);
        SwrveQaUser.Update(qaUserDictionary);

        // Update on Native iOS/Android as well.
#if UNITY_IOS
        updateQAUser(qaUserDictionary);
#endif

#if UNITY_ANDROID
        updateQAUser();
#endif

    }

    #endregion

    private string GetStackPrefix(SwrveUnity.Stack stack)
    {
        if (stack == SwrveUnity.Stack.EU)
        {
            return "eu-";
        }
        return "";
    }

    protected virtual string GetSwrveEndpoint(int appId, SwrveUnity.Stack stack, string suffix)
    {
        return "https://" + appId + "." + GetStackPrefix(stack) + suffix;
    }

    private Dictionary<string, string> ProcessRealTimeUserProperties(Dictionary<string, string> rtimeUserProperties)
    {

        if (rtimeUserProperties == null)
        {
            return null;
        }

        var rtupsKeys = rtimeUserProperties.Keys;
        var result = new Dictionary<string, string>();

        foreach (string key in rtupsKeys)
        {
            string modifiedKey = "user." + key;
            result.Add(modifiedKey, rtimeUserProperties[key]);
        }

        return result;
    }

    public Dictionary<string, string> GetPersonalizationProperties(IDictionary<string, string> payload)
    {
        Dictionary<string, string> resultProperties;
        Dictionary<string, string> rtups = ProcessRealTimeUserProperties(realtimeUserProperties);

        if (config.InAppMessageConfig != null && config.InAppMessageConfig.PersonalizationProvider != null)
        {
            Dictionary<string, string> callbackProperties = config.InAppMessageConfig.PersonalizationProvider.Personalize(payload);
            resultProperties = SwrveHelper.CombineTwoStringDictionaries(rtups, callbackProperties);
        }
        else
        {
            resultProperties = rtups;
        }

        return resultProperties;
    }

    protected Dictionary<string, string> IncludeRealTimeUserProperties(Dictionary<string, string> personalizationProperties)
    {
        Dictionary<string, string> rtups = ProcessRealTimeUserProperties(realtimeUserProperties);
        return SwrveHelper.CombineTwoStringDictionaries(rtups, personalizationProperties);
    }
}
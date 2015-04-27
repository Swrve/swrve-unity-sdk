using System;
using System.Collections.Generic;
using System.Text;
using Swrve;
using System.Collections;
using UnityEngine;
using SwrveMiniJSON;
using System.IO;
using Swrve.REST;
using Swrve.Messaging;
using Swrve.Input;
using Swrve.Helpers;
using Swrve.Storage;
using System.Reflection;
using System.Globalization;
using Swrve.Device;

/// <summary>
/// Internal base class implementation of the Swrve SDK.
/// </summary>
public partial class SwrveSDK
{
    private const String Platform = "Unity ";
    private const float DefaultDPI = 160.0f;
    protected const string EventsSave = "Swrve_Events";
    protected const string InstallTimeEpochSave = "Swrve_JoinedDate";
    protected const string iOSdeviceTokenSave = "Swrve_iOSDeviceToken";
    protected const string GcmdeviceTokenSave = "Swrve_gcmDeviceToken";
    protected const string AbTestUserResourcesSave = "srcngt2"; // Saved securely
    protected const string AbTestUserResourcesDiffSave = "rsdfngt2"; // Saved securely
    protected const string DeviceIdSave = "Swrve_DeviceId";
    protected const string SeqNumSave = "Swrve_SeqNum";
    protected const string ResourcesCampaignTagSave = "cmpg_etag";
    protected const string ResourcesCampaignFlushFrequencySave = "swrve_cr_flush_frequency";
    protected const string ResourcesCampaignFlushDelaySave = "swrve_cr_flush_delay";
    private const string DeviceIdKey = "Swrve.deviceUniqueIdentifier";
    private const string EmptyJSONObject = "{}";
    private const float DefaultCampaignResourcesFlushFrenquency = 60;
    private const float DefaultCampaignResourcesFlushRefreshDelay = 5;
    public const string DefaultAutoShowMessagesTrigger = "Swrve.Messages.showAtSessionStart";

    private string escapedUserId;
    private long installTimeEpoch;
    private string installTimeFormatted;
    private string lastPushEngagedId;
#if UNITY_IPHONE
    private string iOSdeviceToken;
#endif
#if UNITY_ANDROID
    private const string SwrveAndroidPushPluginPackageName = "com.swrve.unity.gcm.SwrveGcmDeviceRegistration";
    private string gcmDeviceToken;
    private static AndroidJavaObject androidPlugin;
    private static bool androidPluginInitialized = false;
    private static bool androidPluginInitializedSuccessfully = false;
#endif
    private int deviceWidth;
    private int deviceHeight;
    private long lastSessionTick;
    private ICarrierInfo deviceCarrierInfo;

    // Events buffer
    protected StringBuilder eventBufferStringBuilder;
    protected string eventsPostString;

    // Storage
    protected string swrvePath;
    protected ISwrveStorage storage;

    // WWW connections
    protected IRESTClient restClient;
    private string eventsUrl;
    private string abTestResourcesDiffUrl;
    protected bool eventsConnecting;
    protected bool abTestUserResourcesDiffConnecting;

    // AB tests and campaigns
    protected string userResourcesRaw;
    protected Dictionary<string, Dictionary<string, string>> userResources;
    protected float campaignsAndResourcesFlushFrequency;
    protected float campaignsAndResourcesFlushRefreshDelay;
    protected string lastETag;
    protected long campaignsAndResourcesLastRefreshed;
    protected bool campaignsAndResourcesInitialized;

    // Talk related
    private static readonly int CampaignAPIVersion = 1;
    private static readonly int CampaignEndpointVersion = 4;
    protected static readonly string CampaignsSave = "cmcc2"; // Saved securely
    protected static readonly string CampaignsSettingsSave = "Swrve_CampaignsData";
    private static readonly string WaitTimeFormat = @"HH\:mm\:ss zzz";
    protected static readonly string InstallTimeFormat = "yyyyMMdd";
    private string resourcesAndCampaignsUrl;
    protected string swrveTemporaryPath;
    protected bool campaignsConnecting;
    protected bool autoShowMessagesEnabled;
    protected bool assetsCurrentlyDownloading;
    protected HashSet<string> assetsOnDisk;
    protected List<SwrveCampaign> campaigns = new List<SwrveCampaign> ();
    protected Dictionary<string, object> campaignSettings = new Dictionary<string, object> ();
    protected Dictionary<string, string> gameStoreLinks = new Dictionary<string, string> ();
    protected SwrveMessageFormat currentMessage = null;
    protected SwrveMessageFormat currentDisplayingMessage = null;
    protected SwrveOrientation currentOrientation;
    protected IInputManager inputManager = NativeInputManager.Instance;
    private string cdn = "http://swrve-content.s3.amazonaws.com/messaging/message_image/";

    // Talk rules
    private const int DefaultDelayFirstMessage = 150;
    private const long DefaultMaxShows = 99999;
    private const int DefaultMinDelay = 55;
    private DateTime initialisedTime;
    private DateTime showMessagesAfterLaunch;
    private DateTime showMessagesAfterDelay;
    private long messagesLeftToShow;
    private int minDelayBetweenMessage;

    // QA
    protected SwrveQAUser qaUser;

    private void QueueSessionStart ()
    {
        Dictionary<string,object> json = new Dictionary<string, object> ();
        AppendEventToBuffer ("session_start", json);
    }

    private void NamedEventInternal (string name, Dictionary<string, string> payload = null, bool allowShowMessage = true)
    {
        if (payload == null) {
            payload = new Dictionary<string, string> ();
        }

        Dictionary<string, object> json = new Dictionary<string, object> ();
        json.Add ("name", name);
        json.Add ("payload", payload);

        AppendEventToBuffer ("event", json, allowShowMessage);
    }

    protected static string GetSwrvePath ()
    {
        string path = Application.persistentDataPath;
        if (string.IsNullOrEmpty (path)) {
            path = Application.temporaryCachePath;
            SwrveLog.Log ("Swrve path (tried again): " + path);
        }
        return path;
    }

    protected static string GetSwrveTemporaryCachePath ()
    {
        string path = Application.temporaryCachePath;
        if (path == null || path.Length == 0) {
            path = Application.persistentDataPath;
        }
        return path;
    }

    private void _Iap (int quantity, string productId, double productPrice, string currency, IapRewards rewards, string receipt, string receiptSignature, string transactionId, string appStore)
    {
        if (!_Iap_check_arguments (quantity, productId, productPrice, currency, appStore)) {
            SwrveLog.LogError ("ERROR: IAP event not sent because it received an illegal argument");
            return;
        }

        Dictionary<string, object> json = new Dictionary<string, object> ();
        json.Add ("app_store", appStore);
        json.Add ("local_currency", currency);
        json.Add ("cost", productPrice);
        json.Add ("product_id", productId);
        json.Add ("quantity", quantity);
        json.Add ("rewards", rewards.getRewards ());

        if (!string.IsNullOrEmpty (GetAppVersion ())) {
            json.Add ("app_version", GetAppVersion ());
        }

        if (appStore == "apple") {
            // receipt comes from the new wrapper and should be base64 encoded here
            json.Add ("receipt", receipt);
            if (!string.IsNullOrEmpty(transactionId)) {
                json.Add ("transaction_id", transactionId);
            }
        } else if (appStore == "google") {
            json.Add ("receipt", receipt);
            json.Add ("receipt_signature", receiptSignature);
        } else {
            json.Add ("receipt", receipt);
        }

        AppendEventToBuffer ("iap", json);

        if (config.AutoDownloadCampaignsAndResources) {
            // Send events automatically and check for changes
            CheckForCampaignsAndResourcesUpdates (false);
        }
    }

    protected virtual SwrveOrientation GetDeviceOrientation ()
    {
        ScreenOrientation orientation = Screen.orientation;
        switch (orientation) {
        case ScreenOrientation.LandscapeLeft:
        case ScreenOrientation.LandscapeRight:
            return SwrveOrientation.Landscape;
        case ScreenOrientation.Portrait:
        case ScreenOrientation.PortraitUpsideDown:
            return SwrveOrientation.Portrait;
        default:
            // Unknown orientation, calculate by the size of the screen
            if (Screen.height >= Screen.width) {
                return SwrveOrientation.Portrait;
            }
            return SwrveOrientation.Landscape;
        }
    }

    private bool _Iap_check_arguments (int quantity, string productId, double productPrice, string currency, string appStore)
    {
        if (String.IsNullOrEmpty (productId)) {
            SwrveLog.LogError ("IAP event illegal argument: productId cannot be empty");
            return false;
        }
        if (String.IsNullOrEmpty (currency)) {
            SwrveLog.LogError ("IAP event illegal argument: currency cannot be empty");
            return false;
        }
        if (String.IsNullOrEmpty (appStore)) {
            SwrveLog.LogError ("IAP event illegal argument: appStore cannot be empty");
            return false;
        }

        if (quantity <= 0) {
            SwrveLog.LogError ("IAP event illegal argument: quantity must be greater than zero");
            return false;
        }
        if (productPrice < 0) {
            SwrveLog.LogError ("IAP event illegal argument: productPrice must be greater than or equal to zero");
            return false;
        }

        return true;
    }

    private Dictionary<string, Dictionary<string, string>> ProcessUserResources (IList<object> userResources)
    {
        Dictionary<string, Dictionary<string, string>> result = new Dictionary<string, Dictionary<string, string>> ();
        if (userResources != null) {
            IEnumerator<object> userResourcesIt = userResources.GetEnumerator ();
            while (userResourcesIt.MoveNext()) {
                Dictionary<string, object> userResource = (Dictionary<string, object>)userResourcesIt.Current;
                string uid = (string)userResource ["uid"];
                result.Add (uid, NormalizeJson (userResource));
            }
        }

        return result;
    }

    private Dictionary<string, string> NormalizeJson (Dictionary<string, object> json)
    {
        Dictionary<string, string> normalized = new Dictionary<string, string> ();
        foreach (string key in json.Keys) {
            object val = json [key];
            if (val != null) {
                normalized.Add (key, val.ToString ());
            }
        }

        return normalized;
    }

    private IEnumerator GetUserResourcesDiff_Coroutine (string getRequest, Action<Dictionary<string, Dictionary<string, string>>, Dictionary<string, Dictionary<string, string>>, string> onResult, Action<Exception> onError, string saveCategory)
    {
        Exception wwwException = null;
        string abTestCandidate = null;
        yield return Container.StartCoroutine(restClient.Get(getRequest, delegate(RESTResponse response) {
            if (response.Error == WwwDeducedError.NoError) {
                abTestCandidate = response.Body;
                SwrveLog.Log ("AB Test result: " + abTestCandidate);
                storage.SaveSecure (saveCategory, abTestCandidate, userId);
                TaskFinished("GetUserResourcesDiff_Coroutine");
            } else {
                // WWW connection error
                wwwException = new Exception (response.Error.ToString ());
                SwrveLog.LogError ("AB Test request failed: " + response.Error.ToString ());
                TaskFinished("GetUserResourcesDiff_Coroutine");
            }
        }));

        abTestUserResourcesDiffConnecting = false;

        if (wwwException != null || string.IsNullOrEmpty (abTestCandidate)) {
            // Try to load from cache
            try {
                string loadedData = storage.LoadSecure (saveCategory, userId);
                if (string.IsNullOrEmpty (loadedData)) {
                    onError.Invoke (wwwException);
                } else {
                    if (ResponseBodyTester.TestUTF8 (loadedData, out abTestCandidate)) {
                        Dictionary<string, Dictionary<string, string>> userResourcesDiffNew = new Dictionary<string, Dictionary<string, string>> ();
                        Dictionary<string, Dictionary<string, string>> userResourcesDiffOld = new Dictionary<string, Dictionary<string, string>> ();
                        ProcessUserResourcesDiff (abTestCandidate, userResourcesDiffNew, userResourcesDiffOld);
                        onResult.Invoke (userResourcesDiffNew, userResourcesDiffOld, abTestCandidate);
                    } else {
                        // Launch error
                        onError.Invoke (wwwException);
                    }
                }
            } catch (Exception e) {
                SwrveLog.LogWarning ("Could not read user resources diff from cache (" + e.ToString () + ")");
                onError.Invoke (wwwException);
            }
        }

        // Launch listener
        if (!string.IsNullOrEmpty (abTestCandidate)) {
            Dictionary<string, Dictionary<string, string>> userResourcesDiffNew = new Dictionary<string, Dictionary<string, string>> ();
            Dictionary<string, Dictionary<string, string>> userResourcesDiffOld = new Dictionary<string, Dictionary<string, string>> ();
            ProcessUserResourcesDiff (abTestCandidate, userResourcesDiffNew, userResourcesDiffOld);
            onResult.Invoke (userResourcesDiffNew, userResourcesDiffOld, abTestCandidate);
        }
    }

    private void ProcessUserResourcesDiff (string abTestJson, Dictionary<string, Dictionary<string, string>> newResources, Dictionary<string, Dictionary<string, string>> oldResources)
    {
        IList<object> userResourcesDiffJson = (List<object>)Json.Deserialize (abTestJson);
        if (userResourcesDiffJson != null) {
            IEnumerator<object> userResourcesIt = userResourcesDiffJson.GetEnumerator ();
            while (userResourcesIt.MoveNext()) {
                Dictionary<string, object> userResource = (Dictionary<string, object>)userResourcesIt.Current;
                string uid = (string)userResource ["uid"];
                Dictionary<string, object> item = (Dictionary<string, object>)userResource ["diff"];
                IEnumerator<string> itemKey = item.Keys.GetEnumerator ();

                Dictionary<string, string> newItemData = new Dictionary<string, string> ();
                Dictionary<string, string> oldItemData = new Dictionary<string, string> ();
                while (itemKey.MoveNext()) {
                    Dictionary<string, string> currentKey = NormalizeJson ((Dictionary<string, object>)item [itemKey.Current]);
                    newItemData.Add (itemKey.Current, currentKey ["new"]);
                    oldItemData.Add (itemKey.Current, currentKey ["old"]);
                }

                newResources.Add (uid, newItemData);
                oldResources.Add (uid, oldItemData);
            }
        }
    }

    private long GetInstallTimeEpoch ()
    {
        string savedInstallTimeEpoch = GetSavedInstallTimeEpoch ();
        if (!string.IsNullOrEmpty (savedInstallTimeEpoch)) {
            long installEpoch = 0;
            if (long.TryParse (savedInstallTimeEpoch, out installEpoch)) {
                return installEpoch;
            }
        }
        long newDate = GetSessionTime ();
        storage.Save (InstallTimeEpochSave, newDate.ToString (), userId);
        return newDate;
    }

    private string GetDeviceId ()
    {
        string deviceId = storage.Load (DeviceIdSave, userId);
        if (!string.IsNullOrEmpty (deviceId)) {
            return deviceId;
        }
        short generatedId = (short)(new System.Random ()).Next (short.MaxValue);
        storage.Save (DeviceIdSave, generatedId.ToString (), userId);
        return generatedId.ToString ();
    }

    private string getNextSeqNum ()
    {
        string seqNum = storage.Load (SeqNumSave, userId);
        if (string.IsNullOrEmpty (seqNum)) {
            seqNum = "0";
        }
        // increment value
        seqNum = (int.Parse (seqNum) + 1).ToString ();
        storage.Save (SeqNumSave, seqNum, userId);
        return seqNum;
    }

    protected string GetDeviceLanguage ()
    {
        string language = null;
#if UNITY_IPHONE
        try {
            language = _swrveiOSGetLanguage();
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get the device language, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
        }
#elif UNITY_ANDROID
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
#endif
        if (string.IsNullOrEmpty (language)) {
            CultureInfo info = CultureInfo.CurrentUICulture;
            string cultureLang = info.TwoLetterISOLanguageName.ToLower ();
            if (cultureLang != "iv") {
                language = cultureLang;
            }
        }

        return language;
    }

    protected string GetSavedInstallTimeEpoch ()
    {
        try {
            string val = storage.Load (InstallTimeEpochSave, userId);
            if (!string.IsNullOrEmpty (val)) {
                return val;
            }
        } catch (Exception e) {
            SwrveLog.LogError ("Couldn't obtain saved install time: " + e.Message);
        }

        return null;
    }

    protected void InvalidateETag ()
    {
        lastETag = string.Empty;
        storage.Remove (ResourcesCampaignTagSave, userId);
    }

    private void InitUserResources ()
    {
        userResourcesRaw = storage.LoadSecure (AbTestUserResourcesSave, userId);
        if (!string.IsNullOrEmpty (userResourcesRaw)) {
            IList<object> userResourcesJson = (IList<object>)Json.Deserialize (userResourcesRaw);
            userResources = ProcessUserResources (userResourcesJson);
            NotifyUpdateUserResources ();
        } else {
            InvalidateETag ();
        }
    }

    private void NotifyUpdateUserResources ()
    {
        if (userResources != null) {
            ResourceManager.SetResourcesFromJSON (userResources);
            if (ResourcesUpdatedCallback != null) {
                ResourcesUpdatedCallback.Invoke ();
            }
        }
    }

    private void LoadEventsFromDisk ()
    {
        try {
            // Load cached events
            string loadedEvents = storage.Load (EventsSave, userId);
            storage.Remove (EventsSave, userId);
            // Add loaded events to buffer
            if (!string.IsNullOrEmpty (loadedEvents)) {
                if (eventBufferStringBuilder.Length != 0) {
                    eventBufferStringBuilder.Insert (0, ",");
                }
                eventBufferStringBuilder.Insert (0, loadedEvents);
            }
        } catch (Exception e) {
            SwrveLog.LogWarning ("Could not read events from cache (" + e.ToString () + ")");
        }
    }

    private void LoadData ()
    {
        // Load events
        LoadEventsFromDisk ();

        // Load joined date
        installTimeEpoch = GetInstallTimeEpoch ();
        installTimeFormatted = SwrveHelper.EpochToFormat (installTimeEpoch, InstallTimeFormat);

        // Load latest etag
        lastETag = storage.Load (ResourcesCampaignTagSave, userId);

        // Load user resources and campaign flush settings
        string strFlushFrequency = storage.Load (ResourcesCampaignFlushFrequencySave, userId);
        if (!string.IsNullOrEmpty (strFlushFrequency)) {
            if (float.TryParse (strFlushFrequency, out campaignsAndResourcesFlushFrequency)) {
                campaignsAndResourcesFlushFrequency /= 1000;
            }
        }
        if (campaignsAndResourcesFlushFrequency == 0) {
            campaignsAndResourcesFlushFrequency = DefaultCampaignResourcesFlushFrenquency;
        }
        string strFlushDelay = storage.Load (ResourcesCampaignFlushDelaySave, userId);
        if (!string.IsNullOrEmpty (strFlushDelay)) {
            if (float.TryParse (strFlushDelay, out campaignsAndResourcesFlushRefreshDelay)) {
                campaignsAndResourcesFlushRefreshDelay /= 1000;
            }
        }
        if (campaignsAndResourcesFlushRefreshDelay == 0) {
            campaignsAndResourcesFlushRefreshDelay = DefaultCampaignResourcesFlushRefreshDelay;
        }
#if UNITY_IPHONE
        // Load device token
        iOSdeviceToken = GetSavediOSDeviceToken();
#endif
    }

    // Create a unique key that can be used to create HMAC signatures
    protected string GetUniqueKey ()
    {
        return apiKey + userId;
    }

    private string GetDeviceUniqueId ()
    {
        // Try to obtain valid saved user id
        string deviceUniqueId = PlayerPrefs.GetString (DeviceIdKey, null);
        if (string.IsNullOrEmpty (deviceUniqueId)) {
            deviceUniqueId = GetRandomUUID ();
        }

        return deviceUniqueId;
    }

    private string GetRandomUUID ()
    {
#if UNITY_IPHONE
        try {
            return _swrveiOSUUID();
        } catch (Exception exp) {
            SwrveLog.LogWarning ("Couldn't get random UUID: " + exp.ToString ());
        }
#else
        try {
            Type type = System.Type.GetType ("System.Guid");
            if (type != null) {
                MethodInfo methodInfo = type.GetMethod ("NewGuid");
                if (methodInfo != null) {
                    object result = methodInfo.Invoke (null, null);
                    if (result != null) {
                        string stringResult = result.ToString();
                        if (!string.IsNullOrEmpty(stringResult)) {
                            return stringResult;
                        }
                    }
                }
            }
        } catch (Exception exp) {
            SwrveLog.LogWarning ("Couldn't get random UUID: " + exp.ToString ());
        }
#endif

        // Generate random string if all fails
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string randomString = string.Empty;
        for (int i = 0; i < 128; i++) {
            randomString += chars[UnityEngine.Random.Range (0, chars.Length - 1)];
        }
        return randomString;
    }

    protected virtual IRESTClient CreateRestClient ()
    {
        return new RESTClient ();
    }

    protected virtual ISwrveStorage CreateStorage ()
    {
        if (config.StoreDataInPlayerPrefs) {
            return new SwrvePlayerPrefsStorage ();
        } else {
            return new SwrveFileStorage (swrvePath, GetUniqueKey ());
        }
    }

    #region WWW coroutines

    private IEnumerator PostEvents_Coroutine (Dictionary<string, string> requestHeaders, byte[] eventsPostEncodedData)
    {
        yield return Container.StartCoroutine(restClient.Post(eventsUrl, eventsPostEncodedData, requestHeaders, delegate(RESTResponse response) {
            if (response.Error != WwwDeducedError.NetworkError) {
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

    protected virtual void ClearEventBuffer ()
    {
        eventsPostString = null;
    }
    #endregion

    #region Event buffer
    private void AppendEventToBuffer (string eventType, Dictionary<string, object> eventParameters, bool allowShowMessage = true)
    {
        eventParameters.Add ("type", eventType);
        eventParameters.Add ("seqnum", getNextSeqNum ());
        eventParameters.Add ("time", GetSessionTime ());

        // Discard the event if it would cause the buffer to overflow
        String eventJson = Json.Serialize (eventParameters);
        bool insideMaxBufferLength = eventBufferStringBuilder.Length + eventJson.Length <= config.MaxBufferChars;
        if (insideMaxBufferLength || config.SendEventsIfBufferTooLarge) {
            // Send buffer if too large
            if (!insideMaxBufferLength && config.SendEventsIfBufferTooLarge) {
                SendQueuedEvents();
            }

            if (eventBufferStringBuilder.Length > 0) {
                eventBufferStringBuilder.Append (',');
            }

            AppendEventToBuffer (eventJson);

            string eventName = SwrveHelper.GetEventName (eventParameters);

            if (allowShowMessage && config.TalkEnabled) {
                StartTask ("ShowMessageForEvent", ShowMessageForEvent (eventName, GlobalInstallButtonListener, GlobalCustomButtonListener, GlobalMessageListener));
            }

#if UNITY_IPHONE
            // Ask for push notification permission dialog
            if (config.PushNotificationEnabled && (eventName.Equals("Swrve.push_notification_permission") || (config.PushNotificationEvents != null && config.PushNotificationEvents.Contains(eventName)))) {
                RegisterForPushNotificationsIOS();
            }
#endif
        } else {
            Debug.LogError("Could not append the event to the buffer. Please consider enabling SendEventsIfBufferTooLarge");
        }
    }

    protected virtual void AppendEventToBuffer (string eventJson)
    {
        eventBufferStringBuilder.Append (eventJson);
    }
    #endregion

    protected virtual Coroutine StartTask (string tag, IEnumerator task)
    {
        return Container.StartCoroutine (task);
    }

    protected virtual void TaskFinished (string tag)
    {
    }

    private bool IsAlive ()
    {
        return (Container != null && !Destroyed);
    }

    protected virtual void GetDeviceScreenInfo ()
    {
        deviceWidth = Screen.width;
        deviceHeight = Screen.height;

        if (deviceWidth > deviceHeight) {
            int tmp = deviceWidth;
            deviceWidth = deviceHeight;
            deviceHeight = tmp;
        }
    }

    private void QueueDeviceInfo ()
    {
        Dictionary<string, string> deviceInfo = GetDeviceInfo ();
        UserUpdate (deviceInfo);
    }

    private void SendDeviceInfo ()
    {
        QueueDeviceInfo ();
        SendQueuedEvents ();
    }

    private IEnumerator WaitAndRefreshResourcesAndCampaigns_Coroutine (float delay)
    {
        yield return new WaitForSeconds(delay);
        RefreshUserResourcesAndCampaigns ();
    }

    private void CheckForCampaignsAndResourcesUpdates (bool invokedByTimer)
    {
        if (!IsAlive ()) {
            // The container was destroyed and we should stop
            return;
        }

        bool sentEvents = SendQueuedEvents ();
        if (sentEvents) {
            // Wait for events to be processed and then ask for campaigns and resources
            Container.StartCoroutine (WaitAndRefreshResourcesAndCampaigns_Coroutine (campaignsAndResourcesFlushRefreshDelay));
        }

        if (!invokedByTimer) {
            // Restart flush timer
            Container.StopCoroutine ("CheckForCampaignsAndResourcesUpdates_Coroutine");
            Container.StartCoroutine (CheckForCampaignsAndResourcesUpdates_Coroutine ());
        }
    }

    private IEnumerator CheckForCampaignsAndResourcesUpdates_Coroutine ()
    {
        yield return new WaitForSeconds(campaignsAndResourcesFlushFrequency);
        CheckForCampaignsAndResourcesUpdates (true);
        // Invoke again
        Container.StartCoroutine (CheckForCampaignsAndResourcesUpdates_Coroutine ());
    }

    protected virtual long GetSessionTime ()
    {
        return SwrveHelper.GetMilliseconds ();
    }

    private void GenerateNewSessionInterval ()
    {
        lastSessionTick = GetSessionTime () + (config.NewSessionInterval * 1000);
    }

    public void Update ()
    {
        if (currentDisplayingMessage != null) {
            // Event processing
            if (!currentMessage.Closing) {
                if (inputManager.GetMouseButtonDown (0)) {
                    ProcessButtonDown ();
                } else if (inputManager.GetMouseButtonUp (0)) {
                    ProcessButtonUp ();
                }
            }
        }
    }

    public void OnGUI ()
    {
        if (currentDisplayingMessage != null) {
            SwrveOrientation newOrientation = GetDeviceOrientation ();

            // Orientation changed
            if (newOrientation != currentOrientation) {
                if (currentDisplayingMessage.Orientation != newOrientation) {
                    // Orientation format change or format rotation
                    bool otherFormat = currentDisplayingMessage.Message.SupportsOrientation (newOrientation);
                    if (otherFormat) {
                        StartTask ("SwitchMessageOrienation", SwitchMessageOrienation (newOrientation));
                    } else {
                        currentDisplayingMessage.Rotate = true;
                    }
                } else {
                    currentDisplayingMessage.Rotate = false;
                }
            }

            // Save current GUI state
            int originalGuiDepth = GUI.depth;
            Matrix4x4 originalTransform = GUI.matrix;
            // Draw message
            GUI.depth = 0;
            SwrveMessageRenderer.DrawMessage (currentMessage, (int)(Screen.width / 2) + currentMessage.Message.Position.X, Screen.height / 2 + currentMessage.Message.Position.Y);
            // Revert previous GUI state
            GUI.matrix = originalTransform;
            GUI.depth = originalGuiDepth;
            // Message listener
            if (currentDisplayingMessage.MessageListener != null) {
                currentDisplayingMessage.MessageListener.OnShowing (currentDisplayingMessage);
            }

            // Remove reference when message is dismissed
            if (currentMessage.Dismissed) {
                currentMessage = null;
                currentDisplayingMessage = null;
            }

            // Update the current orientation
            currentOrientation = newOrientation;
        }
    }

    private IEnumerator SwitchMessageOrienation (SwrveOrientation newOrientation)
    {
        SwrveMessageFormat newFormat = currentMessage.Message.GetFormat (newOrientation);
        if (newFormat != null && newFormat != currentMessage) {
            SwrveMessageFormat oldFormat = currentMessage;
            // Try to load the new message assets
            CoroutineReference<bool> wereAllLoaded = new CoroutineReference<bool> (false);
            yield return StartTask("PreloadFormatAssets", PreloadFormatAssets(newFormat, wereAllLoaded));
            if (wereAllLoaded.Value ()) {
                // Choosen orientation
                currentMessage = currentDisplayingMessage = newFormat;
                oldFormat.UnloadAssets ();
            } else {
                SwrveLog.LogError ("Could not switch orientation. Not all assets could be preloaded");
            }
            TaskFinished ("SwitchMessageOrienation");
        }
    }

    private void ProcessButtonDown ()
    {
        Vector3 mousePosition = inputManager.GetMousePosition ();
        foreach (SwrveButton button in currentMessage.Buttons) {
            if (button.Rect.Contains (mousePosition)) {
                button.Pressed = true;
            }
        }
    }

    private void ProcessButtonUp ()
    {
        SwrveButton clickedButton = null;
        // Capture last button clicked (last rendered, rendered on top)
        for (int i = currentMessage.Buttons.Count - 1; i >= 0 && clickedButton == null; i--) {
            SwrveButton button = currentMessage.Buttons [i];
            Vector3 mousePosition = inputManager.GetMousePosition ();
            if (button.Rect.Contains (mousePosition) && button.Pressed) {
                clickedButton = button;
            } else {
                button.Pressed = false;
            }
        }

        if (clickedButton != null) {
            SwrveLog.Log ("Clicked button " + clickedButton.ActionType);
            ButtonWasPressedByUser (clickedButton);

            if (clickedButton.ActionType == SwrveActionType.Install) {
                string gameId = clickedButton.GameId.ToString ();
                if (gameStoreLinks.ContainsKey (gameId)) {
                    string appStoreUrl = gameStoreLinks [gameId];
                    if (!string.IsNullOrEmpty(appStoreUrl)) {
                        bool normalFlow = true;
                        if (currentMessage.InstallButtonListener != null) {
                            // Launch custom button listener
                            normalFlow = currentMessage.InstallButtonListener.OnAction (appStoreUrl);
                        }

                        if (normalFlow) {
                            // Open app store
                            Application.OpenURL (appStoreUrl);
                        }
                    } else {
                        SwrveLog.LogError("No app store url for game " + gameId);
                    }
                } else {
                    SwrveLog.LogError("Install button app store url empty!");
                }
            } else if (clickedButton.ActionType == SwrveActionType.Custom) {
                string buttonAction = clickedButton.Action;
                if (currentMessage.CustomButtonListener != null) {
                    // Launch custom button listener
                    currentMessage.CustomButtonListener.OnAction (buttonAction);
                } else {
                    SwrveLog.Log("No custom button listener, treating action as URL");
                    if (!string.IsNullOrEmpty(buttonAction)) {
                        Application.OpenURL (buttonAction);
                    }
                }
            }
            clickedButton.Pressed = false;
            DismissMessage();
        }
    }

    protected void SetMessageMinDelayThrottle()
    {
        this.showMessagesAfterDelay = SwrveHelper.GetNow() + TimeSpan.FromSeconds (this.minDelayBetweenMessage);
    }

    private void AutoShowMessages ()
    {
        // Don't do anything if we've already shown a message or if its too long after session start
        if (!autoShowMessagesEnabled) {
            return;
        }

        // Only execute if at least 1 call to the /user_resources_and_campaigns api endpoint has been completed
        if (!campaignsAndResourcesInitialized || campaigns == null || campaigns.Count == 0) {
            return;
        }

        foreach (SwrveCampaign campaign in campaigns) {
            if (campaign.HasMessageForEvent (DefaultAutoShowMessagesTrigger)) {
                if (TriggeredMessageListener != null) {
                    // They are using a custom listener
                    SwrveMessage message = GetMessageForEvent (DefaultAutoShowMessagesTrigger);
                    if (message != null) {
                        autoShowMessagesEnabled = false;
                        TriggeredMessageListener.OnMessageTriggered (message);
                    }
                } else {
                    if (currentMessage == null) {
                        SwrveMessage message = GetMessageForEvent (DefaultAutoShowMessagesTrigger);
                        if (message != null) {
                            autoShowMessagesEnabled = false;
                            Container.StartCoroutine (LaunchMessage (message, GlobalInstallButtonListener, GlobalCustomButtonListener, GlobalMessageListener));
                        }
                    }
                }
                break;
            }
        }
    }

    private IEnumerator LaunchMessage (SwrveMessage message, ISwrveInstallButtonListener installButtonListener, ISwrveCustomButtonListener customButtonListener, ISwrveMessageListener messageListener)
    {
        if (message != null) {
            SwrveOrientation currentOrientation = GetDeviceOrientation ();
            SwrveMessageFormat selectedFormat = message.GetFormat (currentOrientation);
            if (selectedFormat != null) {
                CoroutineReference<bool> wereAllLoaded = new CoroutineReference<bool> (false);
                yield return StartTask("PreloadFormatAssets", PreloadFormatAssets(selectedFormat, wereAllLoaded));
                if (wereAllLoaded.Value ()) {
                    // Choosen orientation
                    ShowMessageFormat (selectedFormat, installButtonListener, customButtonListener, messageListener);
                } else {
                    SwrveLog.LogError ("Could not preload all the assets for message " + message.Id);
                }
            } else {
                SwrveLog.LogError ("Could not get a format for the current orientation: " + currentOrientation.ToString ());
            }
        }
    }

    private void NoMessagesWereShown (string eventName, string reason)
    {
        SwrveLog.Log ("Not showing message for " + eventName + ": " + reason);
        if (qaUser != null) {
            qaUser.TriggerFailure (eventName, reason);
        }
    }

    private IEnumerator PreloadFormatAssets (SwrveMessageFormat format, CoroutineReference<bool> wereAllLoaded)
    {
        SwrveLog.Log ("Preloading format");
        bool allLoaded = true;
        foreach (SwrveImage image in format.Images) {
            if (image.Texture == null && !string.IsNullOrEmpty (image.File)) {
                SwrveLog.Log ("Preloading image file " + image.File);
                CoroutineReference<Texture2D> result = new CoroutineReference<Texture2D> ();
                yield return StartTask("LoadAsset", LoadAsset (image.File, result));
                if (result.Value () != null) {
                    image.Texture = result.Value ();
                } else {
                    allLoaded = false;
                }
            }
        }

        foreach (SwrveButton button in format.Buttons) {
            if (button.Texture == null && !string.IsNullOrEmpty (button.Image)) {
                SwrveLog.Log ("Preloading button image " + button.Image);
                CoroutineReference<Texture2D> result = new CoroutineReference<Texture2D> ();
                yield return StartTask("LoadAsset", LoadAsset (button.Image, result));
                if (result.Value () != null) {
                    button.Texture = result.Value ();
                } else {
                    allLoaded = false;
                }
            }
        }

        wereAllLoaded.Value (allLoaded);
        TaskFinished ("PreloadFormatAssets");
    }

    private bool HasShowTooManyMessagesAlready ()
    {
        return (messagesLeftToShow <= 0);
    }

    private bool IsTooSoonToShowMessageAfterLaunch (DateTime now)
    {
        return now < showMessagesAfterLaunch;
    }

    private bool IsTooSoonToShowMessageAfterDelay (DateTime now)
    {
        return now < showMessagesAfterDelay;
    }

    private SwrveMessageFormat ShowMessageFormat (SwrveMessageFormat format, ISwrveInstallButtonListener installButtonListener, ISwrveCustomButtonListener customButtonListener, ISwrveMessageListener messageListener)
    {
        currentMessage = format;
        format.MessageListener = messageListener;
        format.CustomButtonListener = customButtonListener;
        format.InstallButtonListener = installButtonListener;

        currentDisplayingMessage = currentMessage;
        currentOrientation = GetDeviceOrientation ();
        SwrveMessageRenderer.InitMessage (currentDisplayingMessage);
        // Message was shown to user
        MessageWasShownToUser (currentDisplayingMessage);

        return format;
    }

    private IEnumerator LoadAsset (string fileName, CoroutineReference<Texture2D> texture)
    {
        string filePath = swrveTemporaryPath + "/" + fileName;

        WWW www = new WWW ("file://" + filePath);
        yield return www;
        if (www != null && www.error == null) {
            Texture2D loadedTexture = www.texture;
            texture.Value (loadedTexture);
        } else {
            SwrveLog.LogError ("Could not load asset with WWW " + filePath + ": " + www.error);

            // Try to load from file system
            if (CrossPlatformFile.Exists (filePath)) {
                byte[] byteArray = CrossPlatformFile.ReadAllBytes (filePath);
                Texture2D loadedTexture = new Texture2D (4, 4);
                if (loadedTexture.LoadImage (byteArray)) {
                    texture.Value (loadedTexture);
                } else {
                    SwrveLog.LogWarning ("Could not load asset from I/O" + filePath);
                }
            } else {
                SwrveLog.LogError ("The file " + filePath + " does not exist.");
            }
        }

        TaskFinished ("LoadAsset");
    }

    protected virtual bool CheckAsset (string fileName)
    {
        if (CrossPlatformFile.Exists (swrveTemporaryPath + "/" + fileName)) {
            return true;
        }
        return false;
    }

    protected virtual IEnumerator DownloadAsset (string fileName, CoroutineReference<Texture2D> texture)
    {
        string url = cdn + fileName;
        SwrveLog.Log ("Downloading asset: " + url);
        WWW www = new WWW (url);
        yield return www;
        WwwDeducedError err = UnityWwwHelper.DeduceWwwError (www);
        if (www != null && WwwDeducedError.NoError == err && www.isDone) {
            Texture2D loadedTexture = www.texture;
            if (loadedTexture != null) {
                string filePath = swrveTemporaryPath + "/" + fileName;
                SwrveLog.Log ("Saving to " + filePath);
                byte[] bytes = loadedTexture.EncodeToPNG ();
                CrossPlatformFile.SaveBytes (filePath, bytes);
                bytes = null;

                // Assign texture
                texture.Value (loadedTexture);
            }
        }
        TaskFinished ("DownloadAsset");
    }

    protected IEnumerator DownloadAssets (List<string> assetsQueue)
    {
        assetsCurrentlyDownloading = true;

        foreach (string asset in assetsQueue) {
            if (!CheckAsset (asset)) {
                CoroutineReference<Texture2D> resultTexture = new CoroutineReference<Texture2D> ();
                yield return StartTask ("DownloadAsset", DownloadAsset (asset, resultTexture));
                Texture2D texture = resultTexture.Value ();
                if (texture != null) {
                    assetsOnDisk.Add (asset);
                    Texture2D.Destroy (texture);
                }
            } else {
                // Already downloaded
                assetsOnDisk.Add (asset);
            }
        }

        assetsCurrentlyDownloading = false;
        AutoShowMessages ();

        TaskFinished ("DownloadAssets");
    }

    protected virtual void ProcessCampaigns (Dictionary<string, object> root)
    {
        List<SwrveCampaign> newCampaigns = new List<SwrveCampaign> ();
        List<string> assetsQueue = new List<string> ();

        try {
            // Stop if we got an empty json
            if (root != null && root.ContainsKey ("version")) {
                int version = MiniJsonHelper.GetInt (root, "version");
                if (version == CampaignAPIVersion) {
                    cdn = (string)root ["cdn_root"];

                    // Game data
                    Dictionary<string, object> gameData = (Dictionary<string, object>)root ["game_data"];
                    foreach (String game_id in gameData.Keys) {
                        if (gameStoreLinks.ContainsKey (game_id)) {
                            gameStoreLinks.Remove (game_id);
                        }
                        Dictionary<string, object> gameAppStore = (Dictionary<string, object>)gameData [game_id];
                        if (gameAppStore != null && gameAppStore.ContainsKey ("app_store_url")) {
                            object appStoreLink = gameAppStore ["app_store_url"];
                            if (appStoreLink != null && appStoreLink is string) {
                                gameStoreLinks.Add (game_id, (string)appStoreLink);
                            }
                        }
                    }

                    // Rules
                    Dictionary<string, object> rules = (Dictionary<string, object>)root ["rules"];
                    int delayFirstMessage = (rules.ContainsKey ("delay_first_message")) ? MiniJsonHelper.GetInt (rules, "delay_first_message") : DefaultDelayFirstMessage;
                    long maxShows = (rules.ContainsKey ("max_messages_per_session")) ? MiniJsonHelper.GetLong (rules, "max_messages_per_session") : DefaultMaxShows;
                    int minDelay = (rules.ContainsKey ("min_delay_between_messages")) ? MiniJsonHelper.GetInt (rules, "min_delay_between_messages") : DefaultMinDelay;

                    DateTime now = SwrveHelper.GetNow ();
                    this.minDelayBetweenMessage = minDelay;
                    this.messagesLeftToShow = maxShows;
                    this.showMessagesAfterLaunch = initialisedTime + TimeSpan.FromSeconds (delayFirstMessage);

                    SwrveLog.Log ("Game rules OK: Delay Seconds: " + delayFirstMessage + " Max shows: " + maxShows);
                    SwrveLog.Log ("Time is " + now.ToString () + " show messages after " + this.showMessagesAfterLaunch.ToString ());

                    Dictionary<int, string> campaignsDownloaded = null;

                    // QA
                    if (root.ContainsKey ("qa")) {
                        Dictionary<string, object> jsonQa = (Dictionary<string, object>)root ["qa"];
                        SwrveLog.Log ("You are a QA user!");
                        campaignsDownloaded = new Dictionary<int, string> ();
                        qaUser = new SwrveQAUser (this, jsonQa);

                        if (jsonQa.ContainsKey ("campaigns")) {
                            IList<object> jsonQaCampaigns = (List<object>)jsonQa ["campaigns"];
                            for (int i = 0; i < jsonQaCampaigns.Count; i++) {
                                Dictionary<string, object> jsonQaCampaign = (Dictionary<string, object>)jsonQaCampaigns [i];
                                int campaignId = MiniJsonHelper.GetInt (jsonQaCampaign, "id");
                                string campaignReason = (string)jsonQaCampaign ["reason"];

                                SwrveLog.Log ("Campaign " + campaignId + " not downloaded because: " + campaignReason);

                                // Add campaign for QA purposes
                                campaignsDownloaded.Add (campaignId, campaignReason);
                            }
                        }
                    } else {
                        qaUser = null;
                    }

                    // Campaigns
                    IList<object> jsonCampaigns = (List<object>)root ["campaigns"];

                    for (int i = 0, j = jsonCampaigns.Count; i < j; i++) {
                        Dictionary<string, object> campaignData = (Dictionary<string, object>)jsonCampaigns [i];
                        SwrveCampaign campaign = SwrveCampaign.LoadFromJSON (this, campaignData, initialisedTime, swrveTemporaryPath);
                        if (campaign.Messages.Count > 0) {
                            assetsQueue.AddRange (campaign.ListOfAssets ());

                            if (campaignSettings != null && (qaUser == null || !qaUser.ResetDevice)) {
                                // Load next
                                if (campaignSettings.ContainsKey ("Next" + campaign.Id)) {
                                    int next = MiniJsonHelper.GetInt (campaignSettings, "Next" + campaign.Id);
                                    campaign.Next = next;
                                }
                                // Load impressions
                                if (campaignSettings.ContainsKey ("Impressions" + campaign.Id)) {
                                    int impressions = MiniJsonHelper.GetInt (campaignSettings, "Impressions" + campaign.Id);
                                    campaign.Impressions = impressions;
                                }
                            }

                            newCampaigns.Add (campaign);

                            if (qaUser != null) {
                                // Add campaign for QA purposes
                                campaignsDownloaded.Add (campaign.Id, null);
                            }
                        }
                    }

                    // QA logging
                    if (qaUser != null) {
                        qaUser.TalkSession (campaignsDownloaded);
                    }
                }
            }
        } catch (Exception exp) {
            SwrveLog.LogError ("Could not process campaigns: " + exp.ToString ());
        }

        StartTask ("DownloadAssets", DownloadAssets (assetsQueue));
        campaigns = new List<SwrveCampaign> (newCampaigns);
    }

    private void LoadResourcesAndCampaigns ()
    {
        try {
            if (!campaignsConnecting) {
                if (!config.AutoDownloadCampaignsAndResources) {

                    if (campaignsAndResourcesLastRefreshed != 0) {
                        long currentTime = GetSessionTime ();
                        if (currentTime < campaignsAndResourcesLastRefreshed) {
                            SwrveLog.Log ("Request to retrieve campaign and user resource data was rate-limited.");
                            return;
                        }
                    }

                    // Set next time gate
                    campaignsAndResourcesLastRefreshed = GetSessionTime () + (long)(campaignsAndResourcesFlushFrequency * 1000);
                }

                campaignsConnecting = true;

                float dpi = (Screen.dpi == 0) ? DefaultDPI : Screen.dpi;
                string deviceName = GetDeviceModel ();
                string osVersion = SystemInfo.operatingSystem;
                StringBuilder getRequest = new StringBuilder (resourcesAndCampaignsUrl)
                .AppendFormat ("?user={0}&api_key={1}&app_version={2}&joined={3}", escapedUserId, ApiKey, WWW.EscapeURL (GetAppVersion ()), installTimeEpoch);

                if (config.TalkEnabled) {
                    getRequest.AppendFormat ("&version={0}&orientation={1}&language={2}&app_store={3}&device_width={4}&device_height={5}&device_dpi={6}&os_version={7}&device_name={8}",
                                             CampaignEndpointVersion, config.Orientation.ToString ().ToLower (), Language, config.AppStore,
                                             deviceWidth, deviceHeight, dpi, WWW.EscapeURL (osVersion), WWW.EscapeURL (deviceName));
                }

                if (!string.IsNullOrEmpty (lastETag)) {
                    getRequest.AppendFormat ("&etag={0}", lastETag);
                }
                StartTask ("GetCampaignsAndResources_Coroutine", GetCampaignsAndResources_Coroutine (getRequest.ToString ()));
            }
        } catch (Exception e) {
            SwrveLog.LogError ("Error while trying to get user resources and campaign data: " + e);
        }
    }

    private string GetDeviceModel ()
    {
        string deviceModel = SystemInfo.deviceModel;
        if (string.IsNullOrEmpty (deviceModel)) {
            deviceModel = "ModelUnknown";
        }
        return deviceModel;
    }

    private IEnumerator GetCampaignsAndResources_Coroutine (string getRequest)
    {
        SwrveLog.Log ("Campaigns and resources request: " + getRequest);
        yield return Container.StartCoroutine(restClient.Get(getRequest, delegate(RESTResponse response) {
            if (response.Error == WwwDeducedError.NoError) {
                // Save etag for future requests
                string etag = null;
                if (response.Headers != null) {
                    response.Headers.TryGetValue("ETAG", out etag);
                    if (!string.IsNullOrEmpty(etag)) {
                        lastETag = etag;
                        storage.Save(ResourcesCampaignTagSave, etag, userId);
                    }
                }

                if (!string.IsNullOrEmpty(response.Body)) {
                    Dictionary<string, object> root = (Dictionary<string, object>)Json.Deserialize (response.Body);

                    if (root != null) {
                        // Save flush settings
                        if (root.ContainsKey("flush_frequency")) {
                            string strFlushFrequency = MiniJsonHelper.GetString(root, "flush_frequency");
                            if (!string.IsNullOrEmpty(strFlushFrequency)) {
                                if (float.TryParse(strFlushFrequency, out campaignsAndResourcesFlushFrequency)) {
                                    campaignsAndResourcesFlushFrequency /= 1000;
                                    storage.Save(ResourcesCampaignFlushFrequencySave, strFlushFrequency, userId);
                                }
                            }
                        }
                        if (root.ContainsKey("flush_refresh_delay")) {
                            string strFlushRefreshDelay = MiniJsonHelper.GetString(root, "flush_refresh_delay");
                            if (!string.IsNullOrEmpty(strFlushRefreshDelay)) {
                                if (float.TryParse(strFlushRefreshDelay, out campaignsAndResourcesFlushRefreshDelay)) {
                                    campaignsAndResourcesFlushRefreshDelay /= 1000;
                                    storage.Save(ResourcesCampaignFlushDelaySave, strFlushRefreshDelay, userId);
                                }
                            }
                        }

                        if (root.ContainsKey("user_resources")) {
                            // Process user resources
                            IList<object> userResourcesData = (IList<object>)root["user_resources"];
                            string userResourcesJson = SwrveMiniJSON.Json.Serialize(userResourcesData);
                            storage.SaveSecure(AbTestUserResourcesSave, userResourcesJson, userId);
                            userResources = ProcessUserResources(userResourcesData);
                            userResourcesRaw = userResourcesJson;

                            if (campaignsAndResourcesInitialized) {
                                NotifyUpdateUserResources();
                            }
                        }

                        if (config.TalkEnabled) {
                            if (root.ContainsKey("campaigns")) {
                                Dictionary<string, object> campaignsData = (Dictionary<string, object>)root["campaigns"];
                                string campaignsJson = SwrveMiniJSON.Json.Serialize(campaignsData);
                                SaveCampaignsCache (campaignsJson);

                                AutoShowMessages();

                                ProcessCampaigns (campaignsData);
                                // Construct debug event
                                StringBuilder campaignIds = new StringBuilder ();
                                for (int i = 0, j = campaigns.Count; i < j; i++) {
                                    SwrveCampaign campaign = campaigns [i];
                                    if (i != 0) {
                                        campaignIds.Append (',');
                                    }
                                    campaignIds.Append (campaign.Id);
                                }
                                Dictionary<string, string> payload = new Dictionary<string, string> ();
                                payload.Add ("ids", campaignIds.ToString ());
                                payload.Add ("count", (campaigns == null)? "0" : campaigns.Count.ToString ());
                                NamedEventInternal ("Swrve.Messages.campaigns_downloaded", payload);
                            }
                        }
                    }
                }
            } else {
                SwrveLog.LogError("Resources and campaigns request error: " + response.Error.ToString () + ":" + response.Body);
            }

            if (!campaignsAndResourcesInitialized) {
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

    private void SaveCampaignsCache (string cacheContent)
    {
        try {
            if (cacheContent == null) {
                cacheContent = string.Empty;
            }
            storage.SaveSecure (CampaignsSave, cacheContent, userId);
        } catch (Exception e) {
            SwrveLog.LogError ("Error while saving campaigns to the cache " + e);
        }
    }

    private void SaveCampaignData (SwrveCampaign campaign)
    {
        try {
            campaignSettings ["Next" + campaign.Id] = campaign.Next;
            campaignSettings ["Impressions" + campaign.Id] = campaign.Impressions;

            string serializedCampaignSettings = Json.Serialize (campaignSettings);
            storage.Save (CampaignsSettingsSave, serializedCampaignSettings, userId);
        } catch (Exception e) {
            SwrveLog.LogError ("Error while trying to save campaign settings " + e);
        }
    }

    private void LoadTalkData ()
    {
        // Load campaign settings
        try {
            string campaignSettingsStr;
            string loadedData = storage.Load (CampaignsSettingsSave, userId);
            if (loadedData != null && loadedData.Length != 0) {
                if (ResponseBodyTester.TestUTF8 (loadedData, out campaignSettingsStr)) {
                    campaignSettings = (Dictionary<string, object>)Json.Deserialize (campaignSettingsStr);
                }
            }
        } catch (Exception) {
        }

        // Load campaigns
        try {
            string loadedData = storage.LoadSecure (CampaignsSave, userId);
            if (!string.IsNullOrEmpty (loadedData)) {
                string campaignsCandidate = null;
                if (ResponseBodyTester.TestUTF8 (loadedData, out campaignsCandidate)) {
                    Dictionary<string, object> campaignsData = (Dictionary<string, object>)Json.Deserialize (campaignsCandidate);
                    ProcessCampaigns (campaignsData);
                } else {
                    SwrveLog.Log ("Failed to parse campaigns cache");
                    InvalidateETag ();
                }
            } else {
                InvalidateETag ();
            }
        } catch (Exception e) {
            SwrveLog.LogWarning ("Could not read campaigns from cache, using default (" + e.ToString () + ")");
            InvalidateETag ();
        }
    }

    private void SendPushNotificationEngagedEvent (string pushId)
    {
        if (pushId != lastPushEngagedId) {
            lastPushEngagedId = pushId;
            string eventName = "Swrve.Messages.Push-" + pushId + ".engaged";
            NamedEvent (eventName);
            SwrveLog.Log ("Got Swrve notification with ID " + pushId);
        }
    }

    protected int ConvertInt64ToInt32Hack (Int64 val)
    {
        // SWRVE-5613
        // Hack to solve Unity issue where the id is an in64
        // with a random high part and the int32 value we
        // need in the lower part.
        return (int)(val & 0xFFFFFFFF);
    }

    protected virtual ICarrierInfo GetCarrierInfoProvider()
    {
        return deviceCarrierInfo;
    }

#if UNITY_IPHONE

    protected void RegisterForPushNotificationsIOS()
    {
        // Use Swrve's native plugin to register for push on old Unity versions that did not support iOS8
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6)
        try {
            _swrveRegisterForPushNotifications();
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't invoke native code to register for push notifications, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
            NotificationServices.RegisterForRemoteNotificationTypes(RemoteNotificationType.Alert | RemoteNotificationType.Badge | RemoteNotificationType.Sound);
        }
#else
        UnityEngine.iOS.NotificationServices.RegisterForNotifications(UnityEngine.iOS.NotificationType.Alert | UnityEngine.iOS.NotificationType.Badge | UnityEngine.iOS.NotificationType.Sound);
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

#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6)
    private void ProcessRemoteNotification (RemoteNotification notification)
#else
    private void ProcessRemoteNotification (UnityEngine.iOS.RemoteNotification notification)
#endif
    {
        if(config.PushNotificationEnabled) {
            if (notification.userInfo != null && notification.userInfo.Contains("_p")) {
                object rawId = notification.userInfo["_p"];
                string pushId = rawId.ToString();
                // SWRVE-5613 Hack
                if (rawId is Int64) {
                    pushId = ConvertInt64ToInt32Hack((Int64)rawId).ToString();
                }
                SendPushNotificationEngagedEvent(pushId);
            } else {
                SwrveLog.Log("Got unidentified notification");
            }

            if(PushNotificationListener != null) {
                PushNotificationListener.OnRemoteNotification(notification);
            }
            if(qaUser != null) {
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6)
                qaUser.PushNotification(NotificationServices.remoteNotifications, NotificationServices.remoteNotificationCount);
#else
                qaUser.PushNotification(UnityEngine.iOS.NotificationServices.remoteNotifications, UnityEngine.iOS.NotificationServices.remoteNotificationCount);
#endif
            }
        }
    }
#endif

#if UNITY_ANDROID
    private const int GooglePlayPushPluginVersion = 3;

    private void GooglePlayRegisterForPushNotification(MonoBehaviour container, string senderId)
    {
        try {
            bool registered = false;
            this.gcmDeviceToken = storage.Load (GcmdeviceTokenSave);

            if (!androidPluginInitialized) {
                androidPluginInitialized = true;

                using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                    string jniPluginClassName = SwrveAndroidPushPluginPackageName.Replace(".", "/");

                    if (AndroidJNI.FindClass(jniPluginClassName).ToInt32() != 0) {
                        androidPlugin = new AndroidJavaClass(SwrveAndroidPushPluginPackageName);

                        if (androidPlugin != null) {
                            // Check that the version is the same
                            int pluginVersion = androidPlugin.CallStatic<int>("getVersion");

                            if (pluginVersion != GooglePlayPushPluginVersion) {
                                // Plugin with changes to the public API not supported
                                androidPlugin = null;
                                throw new Exception("The version of the Swrve Android Push plugin is different. This Swrve SDK needs version " + GooglePlayPushPluginVersion);
                            } else {
                                androidPluginInitializedSuccessfully = true;
                            }
                        }
                    }
                }
            }

            if (androidPluginInitializedSuccessfully) {
                registered = androidPlugin.CallStatic<bool>("registerDevice", container.name, senderId, config.GCMPushNotificationTitle);
            }

            if (!registered) {
                SwrveLog.LogError("Could not communicate with the Swrve Android Push plugin. Have you copied all the jars to the directory?");
            }
        } catch (Exception exp) {
            SwrveLog.LogError("Could not retrieve the device Registration Id: " + exp.ToString());
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

    private string AndroidGetAppVersion()
    {
        try {
            using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                AndroidJavaObject context = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
                string packageName = context.Call<string>("getPackageName");
                string versionName = context.Call<AndroidJavaObject>("getPackageManager")
                                     .Call<AndroidJavaObject>("getPackageInfo", packageName, 0).Get<string>("versionName");
                return versionName;
            }
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get the device app version, make sure you are running on an Android device: " + exp.ToString());
        }

        return null;
    }
#endif

    public string GetAppVersion ()
    {
        if (string.IsNullOrEmpty (config.AppVersion)) {
#if UNITY_ANDROID
            config.AppVersion = AndroidGetAppVersion();
#elif UNITY_IPHONE
            try {
                config.AppVersion = _swrveiOSGetAppVersion();
                SwrveLog.Log ("got iOS version name " + config.AppVersion);
            } catch (Exception exp) {
                SwrveLog.LogWarning("Couldn't get the device app version, make sure you have the iOS plugin inside your project and you are running on a iOS device: " + exp.ToString());
            }
#endif
        }
        return config.AppVersion;
    }

    private void SetInputManager (IInputManager inputManager)
    {
        this.inputManager = inputManager;
    }

    protected class CoroutineReference<T>
    {
        private T val;

        public CoroutineReference ()
        {
        }

        public CoroutineReference (T val)
        {
            this.val = val;
        }

        public T Value ()
        {
            return val;
        }

        public void Value (T val)
        {
            this.val = val;
        }
    }

    protected void StartCampaignsAndResourcesTimer ()
    {
        if (!config.AutoDownloadCampaignsAndResources) {
            return;
        }

        RefreshUserResourcesAndCampaigns ();

		// Start repeating timer to auto-send events after a specified frequency. eg: 60s
		Container.StartCoroutine (CheckForCampaignsAndResourcesUpdates_Coroutine ());

		// Call refresh once after refresh delay (eg: 5s) to ensure campaigns are reloaded after initial events have been sent.
		Container.StartCoroutine (WaitAndRefreshResourcesAndCampaigns_Coroutine (campaignsAndResourcesFlushRefreshDelay));
    }

    protected void DisableAutoShowAfterDelay ()
    {
        // Start timer to disable autoshow
        Container.StartCoroutine (DisableAutoShowAfterDelay_Coroutine ());
    }

    private IEnumerator DisableAutoShowAfterDelay_Coroutine ()
    {
        yield return new WaitForSeconds(config.AutoShowMessagesMaxDelay);
        autoShowMessagesEnabled = false;
    }
}

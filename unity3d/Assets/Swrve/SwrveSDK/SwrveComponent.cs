using UnityEngine;
using SwrveUnity;
using SwrveUnityMiniJSON;
using SwrveUnity.Helpers;
using System.Collections.Generic;

/// <summary>
/// Swrve SDK Unity3D Script. Use this script to easily include the SDK
/// in your scene.
/// </summary>
public class SwrveComponent : MonoBehaviour
{
    /// <summary>
    /// Instance of the Swrve SDK.
    /// </summary>
    public SwrveSDK SDK;

    /// <summary>
    /// Automatically flush events on application quit.
    /// </summary>
    public bool FlushEventsOnApplicationQuit = true;

    /// Singleton instance access.
    protected static SwrveComponent instance;

    /// <summary>
    /// Return a SwrveComponent located in the scene.
    /// </summary>
    public static SwrveComponent Instance
    {
        get
        {
            if (!instance)
            {
                // Obtain the first instance of the SwrveComponent in the scene
                SwrveComponent[] instances = Object.FindObjectsOfType(typeof(SwrveComponent)) as SwrveComponent[];
                if (instances != null && instances.Length > 0)
                {
                    instance = instances[0];
                }
                else
                {
                    SwrveLog.LogError("There needs to be one active SwrveComponent script on a GameObject in your scene.");
                }
            }

            return instance;
        }
    }

    /// <summary>
    /// Default constructor. Will be called by Unity when
    /// placing this script in your scene.
    /// </summary>
    public SwrveComponent()
    {
        SDK = new SwrveEmpty();
    }

    /// <summary>
    /// Initialize the SDK.
    /// </summary>
    /// <param name="appId">
    /// ID for your app, as supplied by Swrve.
    /// </param>
    /// <param name="apiKey">
    /// Scret API key for your app, as supplied by Swrve.
    /// </param>
    /// <param name="config">
    /// Extra configuration for the SDK.
    /// </param>
    public void Init(int appId, string apiKey, SwrveConfig config = null)
    {
        if (SDK == null || SDK is SwrveEmpty)
        {
            bool supportedOSAndVersion = true;
#if !UNITY_EDITOR

#if UNITY_IOS
            supportedOSAndVersion = SwrveSDK.IsSupportediOSVersion();
#elif UNITY_ANDROID
            supportedOSAndVersion = SwrveSDK.IsSupportedAndroidVersion();
#else
#warning "We do not officially support this plaform. tracking is disabled."
            supportedOSAndVersion = false;
#endif

#elif !UNITY_IOS && !UNITY_ANDROID
#warning "We do not officially support this plaform. tracking is disabled."
            supportedOSAndVersion = false;
#endif
            if (supportedOSAndVersion)
            {
                SDK = new SwrveSDK();
            }
            else
            {
                SDK = new SwrveEmpty();
            }
        }
        if (config == null)
        {
            config = new SwrveConfig();
        }
        SDK.Init(this, appId, apiKey, config);
    }

    /// <summary>
    /// Initialize the SDK on start.
    /// </summary>
    public void Start()
    {
        useGUILayout = false;
    }

    /// <summary>
    /// Render in-app messages.
    /// </summary>
    public void OnGUI()
    {
        SDK.OnGUI();
    }


#if UNITY_IOS
    protected bool deviceTokenSent = false;

    // Used by the native internals
    public void SetPushNotificationsPermissionStatus(string pushStatus)
    {
        SDK.SetPushNotificationsPermissionStatus(pushStatus);
    }
#endif

    public void Update()
    {
        if (SDK != null && SDK.Initialised)
        {
            SDK.Update();
#if UNITY_IOS
            SDK.ProcessRemoteNotifications();
#endif
        }
    }

#if UNITY_ANDROID
    /// Called by the push plugin to notify
    /// of a device registration id.
    public virtual void OnDeviceRegistered(string registrationId)
    {
        if (SDK != null)
        {
            SDK.RegistrationIdReceived(registrationId);
        }
    }

    /// Called by the push plugin to notify
    /// of a received push notification.
    public virtual void OnNotificationReceived(string notificationJson)
    {
        if (SDK != null)
        {
            SDK.NotificationReceived(notificationJson);
        }
    }

    /// Called by the push plugin to notify
    /// of the push notification that opened the app.
    public virtual void OnOpenedFromPushNotification(string notificationJson)
    {
        if (SDK != null)
        {
            SDK.OpenedFromPushNotification(notificationJson);
        }
    }

    /// Called by the ADM plugin to notify
    /// of a device registration id.
    public virtual void OnDeviceRegisteredADM(string registrationId)
    {
        if (SDK != null)
        {
            SDK.RegistrationIdReceivedADM(registrationId);
        }
    }

    /// Called by the FCM plugin to notify
    /// of the Advertising Id.
    public virtual void OnNewAdvertisingId(string advertisingId)
    {
        if (SDK != null)
        {
            SDK.SetGooglePlayAdvertisingId(advertisingId);
        }
    }
#endif

    /// <summary>
    /// Stop all the work of the SDK.
    /// </summary>
    public void OnDestroy()
    {
        if (SDK.Initialised)
        {
            SDK.OnSwrveDestroy();
        }
        StopAllCoroutines();
    }

    /// <summary>
    /// Automatically called by Unity3D when the application quits.
    /// </summary>
    public void OnApplicationQuit()
    {
        if (SDK.Initialised && FlushEventsOnApplicationQuit)
        {
            SDK.OnSwrveDestroy();
        }
    }

    /// <summary>
    /// Automatically called by Unity3D when the app is paused or resumed.
    /// </summary>
    public void OnApplicationPause(bool pauseStatus)
    {
        if (SDK != null && SDK.Initialised)
        {
            if (pauseStatus)
            {
                SDK.OnSwrvePause();
            }
            else
            {
                SDK.OnSwrveResume();
            }
        }

#if UNITY_IOS
        if (!pauseStatus)
        {
            // Refresh token after application resume
            deviceTokenSent = false;
        }
#endif
    }

    // Used by the native internals. Please use the SDK object directly.
    public void UserUpdate(string userUpdate)
    {
        try
        {
            Dictionary<string, object> o = (Dictionary<string, object>)Json.Deserialize(userUpdate);
            Dictionary<string, string> _o = new Dictionary<string, string>();
            Dictionary<string, object>.Enumerator it = o.GetEnumerator();

            while (it.MoveNext())
            {
                _o[it.Current.Key] = string.Format("{0}", it.Current.Value);
            }

            SDK.UserUpdate(_o);
        }
        catch (System.Exception e)
        {
            SwrveLog.LogError(e.ToString(), "userUpdate");
        }
    }

    public void NativeConversationClosed(string msg)
    {
        try
        {
            SDK.ConversationClosed();
        }
        catch (System.Exception e)
        {
            SwrveLog.LogError(e.ToString(), "nativeConversationClosed");
        }
    }

    // Keep the SDK alive between scene loads
    void Awake()
    {
        DontDestroyOnLoad(transform.gameObject);
    }
}

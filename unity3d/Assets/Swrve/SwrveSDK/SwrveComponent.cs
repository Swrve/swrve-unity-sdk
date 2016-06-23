using UnityEngine;
using Swrve;
using SwrveMiniJSON;
using Swrve.Helpers;
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
    protected SwrveSDK _SDK;

    /// <summary>
    /// ID for your game, as supplied by Swrve.
    /// </summary>
    public int GameId = 0;

    /// <summary>
    /// Secret API key, as supplied by Swrve.
    /// </summary>
    public string ApiKey = "your_api_key_here";

    /// <summary>
    /// SDK configuration.
    /// </summary>
    public SwrveConfig Config;

    /// <summary>
    /// Automatically flush events on application quit.
    /// </summary>
    public bool FlushEventsOnApplicationQuit = true;

    /// <summary>
    /// Automatically initialise the SDK on Start.
    /// </summary>
    public bool InitialiseOnStart = true;

    /// Singleton instance access.
    protected static SwrveComponent instance;

    /// <summary>
    /// Return a SwrveComponent located in the scene.
    /// </summary>
    public static SwrveComponent Instance
    {
        get {
            if (!instance) {
                // Obtain the first instance of the SwrveComponent in the scene
                SwrveComponent[] instances = Object.FindObjectsOfType (typeof(SwrveComponent)) as SwrveComponent[];
                if (instances != null && instances.Length > 0) {
                    instance = instances [0];
                } else {
                    SwrveLog.LogError ("There needs to be one active SwrveComponent script on a GameObject in your scene.");
                }
            }

            return instance;
        }
    }

    public static SwrveSDK SDK { get { return Instance._SDK; } }

    /// <summary>
    /// Default constructor. Will be called by Unity when
    /// placing this script in your scene.
    /// </summary>
    public SwrveComponent ()
    {
        Config = new SwrveConfig ();
        _SDK = new SwrveSDK ();
    }

    /// <summary>
    /// Manually initialize the SDK.
    /// </summary>
    /// <param name="gameId">
    /// ID for your game, as supplied by Swrve.
    /// </param>
    /// <param name="apiKey">
    /// Scret API key for your game, as supplied by Swrve.
    /// </param>
    public void Init (int gameId, string apiKey)
    {
        _SDK.Init (this, gameId, apiKey, Config);
    }

    /// <summary>
    /// Initialize the SDK on start.
    /// </summary>
    public void Start ()
    {
        useGUILayout = false;
        if (InitialiseOnStart) {
            Init (GameId, ApiKey);
        }
    }

    /// <summary>
    /// Render in-app messages.
    /// </summary>
    public void OnGUI ()
    {
        _SDK.OnGUI ();
    }


#if UNITY_IPHONE
    protected bool deviceTokenSent = false;
#endif

    public void Update ()
    {
        if (_SDK != null && _SDK.Initialised) {
            _SDK.Update ();
#if UNITY_IPHONE
            if (!deviceTokenSent) {
                deviceTokenSent = _SDK.ObtainIOSDeviceToken();
            }
            _SDK.ProcessRemoteNotifications();
#endif
        }
    }

#if UNITY_ANDROID
    /// Called by the Google Cloud Messaging plugin to notify
    /// of a device registration id.
    public virtual void OnDeviceRegistered(string registrationId)
    {
        if (SDK != null && SDK.Initialised) {
            SDK.RegistrationIdReceived(registrationId);
        }
    }

    /// Called by the Google Cloud Messaging plugin to notify
    /// of a received push notification.
    public virtual void OnNotificationReceived(string notificationJson)
    {
        if (SDK != null && SDK.Initialised) {
            SDK.NotificationReceived(notificationJson);
        }
    }

    /// Called by the Google Cloud Messaging plugin to notify
    /// of the push notification that opened the app.
    public virtual void OnOpenedFromPushNotification(string notificationJson)
    {
        if (SDK != null && SDK.Initialised) {
            SDK.OpenedFromPushNotification(notificationJson);
        }
    }

    /// Called by the Google Cloud Messaging plugin to notify
    /// of the Advertising Id.
    public virtual void OnNewAdvertisingId(string advertisingId)
    {
        if (SDK != null && SDK.Initialised) {
            SDK.SetGooglePlayAdvertisingId(advertisingId);
        }
    }
#endif

    /// <summary>
    /// Stop all the work of the SDK.
    /// </summary>
    public void OnDestroy ()
    {
        if (_SDK.Initialised) {
            _SDK.OnSwrveDestroy ();
        }
        StopAllCoroutines ();
    }

    /// <summary>
    /// Automatically called by Unity3D when the application quits.
    /// </summary>
    public void OnApplicationQuit ()
    {
        if (_SDK.Initialised && FlushEventsOnApplicationQuit) {
            _SDK.OnSwrveDestroy ();
        }
    }

    /// <summary>
    /// Automatically called by Unity3D when the app is paused or resumed.
    /// </summary>
    public void OnApplicationPause (bool pauseStatus)
    {
        if (_SDK != null && _SDK.Initialised && Config != null && Config.AutomaticSessionManagement) {
            if (pauseStatus) {
                _SDK.OnSwrvePause ();
            } else {
                _SDK.OnSwrveResume ();
            }
        }

#if UNITY_IPHONE
        if (!pauseStatus) {
            // Refresh token after application resume
            deviceTokenSent = false;
        }
#endif
    }

    public void SetLocationSegmentVersion(string locationSegmentVersion) {
        try {
            _SDK.SetLocationSegmentVersion(int.Parse(locationSegmentVersion));
        } catch (System.Exception e) {
            SwrveLog.LogError (e.ToString(), "location");
        }
    }

    public void UserUpdate(string userUpdate) {
        try {
            Dictionary<string, object> o = (Dictionary<string, object>)Json.Deserialize (userUpdate);
            Dictionary<string, string> _o = new Dictionary<string, string>();
            Dictionary<string, object>.Enumerator it = o.GetEnumerator();

            while(it.MoveNext()) {
                _o[it.Current.Key] = (string) it.Current.Value;
            }

            _SDK.UserUpdate(_o);
        } catch (System.Exception e) {
            SwrveLog.LogError (e.ToString(), "userUpdate");
        }
    }
}
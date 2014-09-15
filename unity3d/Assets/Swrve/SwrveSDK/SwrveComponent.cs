using UnityEngine;
using Swrve;
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
    public SwrveSDK SDK;

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
                SwrveComponent[] instances = Resources.FindObjectsOfTypeAll (typeof(SwrveComponent)) as SwrveComponent[];
                if (instances != null && instances.Length > 0) {
                    instance = instances [0];
                } else {
                    SwrveLog.LogError ("There needs to be one active SwrveComponent script on a GameObject in your scene.");
                }
            }

            return instance;
        }
    }

    /// <summary>
    /// Default constructor. Will be called by Unity when
    /// placing this script in your scene.
    /// </summary>
    public SwrveComponent ()
    {
        Config = new SwrveConfig ();
        SDK = new SwrveSDK ();
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
        SDK.Init (this, gameId, apiKey, Config);
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
        SDK.OnGUI ();
    }


#if UNITY_IPHONE
    protected bool deviceTokenSent = false;
#endif

    public void Update ()
    {
        if (SDK != null && SDK.Initialised) {
            SDK.Update ();
#if UNITY_IPHONE
            if (!deviceTokenSent) {
                deviceTokenSent = SDK.ObtainIOSDeviceToken();
            }
            SDK.ProcessRemoteNotifications();
#endif
        }
    }

#if UNITY_ANDROID
    /// Called by the Google Cloud Messaging plugin to notify
    /// of a device registration id.
    public virtual void OnDeviceRegistered(string registrationId)
    {
        SwrveLog.LogError("Obtained registration id: " + registrationId);
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
#endif

    /// <summary>
    /// Stop all the work of the SDK.
    /// </summary>
    public void OnDestroy ()
    {
        if (SDK.Initialised) {
            SDK.OnSwrveDestroy ();
        }
        StopAllCoroutines ();
    }

    /// <summary>
    /// Automatically called by Unity3D when the application quits.
    /// </summary>
    public void OnApplicationQuit ()
    {
        if (SDK.Initialised && FlushEventsOnApplicationQuit) {
            SDK.OnSwrveDestroy ();
        }
    }

    /// <summary>
    /// Automatically called by Unity3D when the app is paused or resumed.
    /// </summary>
    public void OnApplicationPause (bool pauseStatus)
    {
        if (SDK != null && SDK.Initialised && Config != null && Config.AutomaticSessionManagement) {
            if (pauseStatus) {
                SDK.OnSwrvePause ();
            } else {
                SDK.OnSwrveResume ();
            }
        }

#if UNITY_IPHONE
        if (!pauseStatus) {
            // Refresh token after application resume
            deviceTokenSent = false;
        }
#endif
    }
}
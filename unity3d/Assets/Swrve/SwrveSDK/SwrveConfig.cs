/*
 * SWRVE CONFIDENTIAL
 * 
 * (c) Copyright 2010-2014 Swrve New Media, Inc. and its licensors.
 * All Rights Reserved.
 *
 * NOTICE: All information contained herein is and remains the property of Swrve
 * New Media, Inc or its licensors.  The intellectual property and technical
 * concepts contained herein are proprietary to Swrve New Media, Inc. or its
 * licensors and are protected by trade secret and/or copyright law.
 * Dissemination of this information or reproduction of this material is
 * strictly forbidden unless prior written permission is obtained from Swrve.
 */

using System;
using Swrve.Messaging;
using System.Collections.Generic;
using System.Globalization;

namespace Swrve
{
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
    /// App store where the game will be submitted.
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
    /// Enable or disable Talk features for in-app campaigns/
    /// </summary>
    public bool TalkEnabled = true;

    /// <summary>
    /// Automatically download campaigns and user resources.
    /// </summary>
    public bool AutoDownloadCampaignsAndResources = true;

    /// <summary>
    /// Orientations supported by the game.
    /// </summary>
    public SwrveOrientation Orientation = SwrveOrientation.Both;

    /// <summary>
    /// The URL of the server to send events to.
    /// </summary>
    public string EventsServer = DefaultEventsServer;
    public const string DefaultEventsServer = "http://api.swrve.com";

    /// <summary>
    /// The URL of the server to request campaign and resources data from.
    /// </summary>
    public string ContentServer = DefaultContentServer;
    public const string DefaultContentServer = "http://content.swrve.com";

    /// <summary>
    /// The SDK will send a session start on init and manage game pauses and resumes.
    /// </summary>
    public bool AutomaticSessionManagement = true;

    /// <summary>
    /// Threshold in seconds to send a new session start after the game lost focus and regained it again.
    /// </summary>
    public int NewSessionInterval = 30;

    /// <summary>
    /// The maximum number of event characters to buffer before events are discarded.
    /// </summary>
    public int MaxBufferChars = 20000;

    /// <summary>
    /// Force saved data into PlayerPrefs.
    /// </summary>
    /// <remarks>
    /// Used for debugging; an appropriate location is otherwise automatically chosen for saved data.
    /// </remarks>
    public bool StoreDataInPlayerPrefs = false;

    /// <summary>
    /// Enable push notification on this game.
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
    /// Maximum delay in seconds for in-app messages to appear after initialization.
    /// </summary>
    public float AutoShowMessagesMaxDelay = 5;
}
}


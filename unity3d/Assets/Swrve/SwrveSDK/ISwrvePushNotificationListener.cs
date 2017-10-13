using System;
using UnityEngine;
using System.Collections.Generic;

namespace SwrveUnity
{
/// <summary>
/// Implement this interface to be notified of any Swrve push notification
/// to your app.
/// </summary>
public interface ISwrvePushNotificationListener
{
#if UNITY_IPHONE
    /// <summary>
    /// This method will be called when a push notification is received by your app,
    /// after the user has opened your app from it or if the app was on the foreground.
    /// </summary>
    /// <param name="notification">
    /// Push notification information including custom payloads.
    /// </param>
#if UNITY_5 || UNITY_2017_1_OR_NEWER
    void OnRemoteNotification(UnityEngine.iOS.RemoteNotification notification);
#else
    void OnRemoteNotification(RemoteNotification notification);
#endif

#endif

#if UNITY_ANDROID
    /// <summary>
    /// This method will be called as soon as a push notification is received by
    /// your app, without no user interaction.
    /// </summary>
    /// <param name="notificationJson">
    /// Push notification information including custom payloads.
    /// </param>
    void OnNotificationReceived(Dictionary<string, object> notificationJson);

    /// <summary>
    /// This method will be called when a push notification is received by your app,
    /// after the user has opened your app from it.
    /// </summary>
    /// <param name="notificationJson">
    /// Push notification information including custom payloads.
    /// </param>
    void OnOpenedFromPushNotification(Dictionary<string, object> notificationJson);
#endif

#if UNITY_WSA_10_0
    /// <summary>
    /// Represents a method that handles a push notification payload.
    /// </summary>
    /// <param name="payload">Push notification payload.</param>
    void OnOpenedFromPushNotification(Dictionary<string, string> payload);
#endif
}
}

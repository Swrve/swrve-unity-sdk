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
using UnityEngine;
using System.Collections.Generic;

namespace Swrve
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
    void OnRemoteNotification(RemoteNotification notification);
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
}
}
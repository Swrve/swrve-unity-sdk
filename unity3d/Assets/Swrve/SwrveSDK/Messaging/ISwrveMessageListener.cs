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

namespace Swrve.Messaging
{
/// <summary>
/// Implement this interface to respond to messages being
/// shown, rendered or dismissed:
/// SwrveSDK.GlobalMessageListener = new YourMessageListener();
/// </summary>
public interface ISwrveMessageListener
{
    /// <summary>
    /// Called once per message being shown. Pause your game
    /// here if necessary.
    /// </summary>
    /// <param name="format">
    /// In-app message information.
    /// </param>
    void OnShow (SwrveMessageFormat format);

    /// <summary>
    /// Called every frame a message is being displayed.
    /// </summary>
    /// <param name="format">
    /// In-app message information.
    /// </param>
    void OnShowing (SwrveMessageFormat format);

    /// <summary>
    /// Called when the message has been dismissed. Resume
    /// your game here if necessary.
    /// </summary>
    /// <param name="format">
    /// In-app message information.
    /// </param>
    void OnDismiss (SwrveMessageFormat format);
}
}


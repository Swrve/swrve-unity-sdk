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
/// Implement this to use your own rendering code for in-app messages.
/// Will disable the Swrve implementation.
/// </summary>
public interface ISwrveTriggeredMessageListener
{
    /// <summary>
    /// Called once per message being shown. Pause your game
    /// here if necessary.
    /// </summary>
    /// <param name="message">
    /// Message to be rendered. Contains multiple formats.
    /// </param>
    void OnMessageTriggered (SwrveMessage message);

    /// <summary>
    /// The current message has to be dismissed.
    /// </summary>
    void DismissCurrentMessage ();
}
}


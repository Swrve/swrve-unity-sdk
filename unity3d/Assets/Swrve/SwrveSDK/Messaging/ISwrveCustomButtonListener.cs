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
/// Implement this interface to handle custom deep-links in your app as result
/// of an in-app custom button.
/// </summary>
public interface ISwrveCustomButtonListener
{
    /// <summary>
    /// This method is invoked when a custom button has been pressed on an in-app message.
    /// </summary>
    /// <param name="customAction">
    /// Custom action of button that was pressed.
    /// </param>
    void OnAction (string customAction);
}
}

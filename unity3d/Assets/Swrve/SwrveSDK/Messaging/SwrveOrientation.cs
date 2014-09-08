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
/// Used for device orientation and specifying orientation filters.
/// </summary>
public enum SwrveOrientation {
    Portrait,
    Landscape,
    Both
}

public static class SwrveOrientationHelper
{
    /// <summary>
    /// Convert from String to SwrveOrientation.
    /// </summary>
    /// <param name="orientation">
    /// Device orientation or filter.
    /// </param>
    /// <returns>
    /// Parsed orientation.
    /// </returns>
    public static SwrveOrientation Parse (string orientation)
    {
        if (orientation.ToLower ().Equals ("portrait")) {
            return SwrveOrientation.Portrait;
        } else if (orientation.ToLower ().Equals ("both")) {
            return SwrveOrientation.Both;
        }

        return SwrveOrientation.Landscape;
    }
}
}


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

namespace Swrve.Messaging
{
/// <summary>
/// Used internally to represent a point in 2D space.
/// </summary>
public struct Point
{
    public int X;
    public int Y;

    public Point (int x, int y)
    {
        this.X = x;
        this.Y = y;
    }
}
}

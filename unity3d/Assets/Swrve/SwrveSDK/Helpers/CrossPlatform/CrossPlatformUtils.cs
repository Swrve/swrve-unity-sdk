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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Swrve
{
/// <summary>
/// Used internally to support all platform.
/// </summary>
public static class CrossPlatformUtils
{
    public static WWW MakeWWW (string url, byte[] encodedData, Dictionary<string, string> headers)
    {
#if UNITY_METRO || UNITY_WP8
        return new WWW(url, encodedData, headers);
#else
        return new WWW (url, encodedData, new Hashtable (headers));
#endif
    }
}
}

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
using System.Text;
using System.Text.RegularExpressions;

namespace Swrve.Helpers
{
/// <summary>
/// Used internally to build POST requests for submission to %Swrve.
/// </summary>
public class PostBodyBuilder
{
    private const int ApiVersion = 2;
    private static readonly string Format = Regex.Replace (@"
{{
 ""user"":""{0}"",
 ""version"":{1},
 ""app_version"":""{2}"",
 ""session_token"":""{3}"",
 ""device_id"":""{4}"",
 ""data"":[{5}]
}}", @"\s", "");

    public static byte[] Build (string apiKey, int gameId, string userId, string deviceId, string appVersion, long time, string events)
    {
        var sessionToken = CreateSessionToken (apiKey, gameId, userId, time);
        var postString = String.Format (Format, userId, ApiVersion, appVersion, sessionToken, deviceId, events);
        var encodedData = Encoding.UTF8.GetBytes (postString);
        return encodedData;
    }

    private static string CreateSessionToken (string apiKey, int gameId, string userId, long time)
    {
        var md5Hash = SwrveHelper.ApplyMD5 (String.Format ("{0}{1}{2}", userId, time, apiKey));
        return String.Format ("{0}={1}={2}={3}", gameId, userId, time, md5Hash);
    }
}
}

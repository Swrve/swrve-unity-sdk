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
using Swrve.Helpers;
using System.Collections.Generic;

namespace Swrve.REST
{
/// <summary>
/// Used internally to wrap REST responses.
/// </summary>
public class RESTResponse
{
    public readonly string Body;
    public readonly WwwDeducedError Error = WwwDeducedError.NoError;
    public readonly Dictionary<string, string> Headers;

    public RESTResponse (string body)
    {
        Body = body;
    }

    public RESTResponse (string body, Dictionary<string, string> headers) : this(body)
    {
        Headers = headers;
    }

    public RESTResponse (WwwDeducedError error)
    {
        Error = error;
    }
}
}


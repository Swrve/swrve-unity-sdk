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
using System.Collections;
using System.Collections.Generic;
using Swrve.Helpers;

namespace Swrve.REST
{
/// <summary>
/// Used internally to connect to REST services.
/// </summary>
public interface IRESTClient
{
    IEnumerator Get (string url, Action<RESTResponse> listener);

    IEnumerator Post (string url, byte[] encodedData, Dictionary<string, string> headers, Action<RESTResponse> listener);
}
}


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

namespace Swrve.Helpers
{
/// <summary>
/// Used internally to test the content of HTTP responses.
/// </summary>
public class ResponseBodyTester
{
    /// <summary>
    /// Tests a %Swrve response body.
    /// </summary>
    /// <returns>
    /// True if the response was correctly encoded.
    /// </returns>
    /// <param name="data">
    /// Body bytes to test.
    /// </param>
    /// <param name="decodedString">
    /// Decoded string if return value is true.
    /// </param>
    public static bool TestUTF8(string data, out string decodedString)
    {
        return TestUTF8(Encoding.UTF8.GetBytes (data), out decodedString);
    }

    /// <summary>
    /// Tests a %Swrve response body.
    /// </summary>
    /// <returns>
    /// True if the response was correctly encoded.
    /// </returns>
    /// <param name="bodyBytes">
    /// Body bytes to test.
    /// </param>
    /// <param name="decodedString">
    /// Decoded string if return value is true.
    /// </param>
    public static bool TestUTF8(byte[] bodyBytes, out string decodedString)
    {
        // UTF8 might be invalid
        try {
            decodedString = Encoding.UTF8.GetString(bodyBytes, 0, bodyBytes.Length);
            return true;
        } catch(Exception) {
            decodedString = string.Empty;
        }

        return false;
    }
}
}

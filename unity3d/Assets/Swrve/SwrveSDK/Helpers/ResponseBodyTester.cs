using System;
using System.Text;

namespace SwrveUnity.Helpers
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

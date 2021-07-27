using System;
using SwrveUnity.Helpers;
using System.Collections.Generic;

namespace SwrveUnity.REST
{
/// <summary>
/// Used internally to wrap REST responses.
/// </summary>
public class RESTResponse
{
    public readonly string Body;
    public readonly WwwDeducedError Error = WwwDeducedError.NoError;
    public readonly long ResponseCode;
    public readonly Dictionary<string, string> Headers;

    public RESTResponse (long responseCode = 0, string responseBody = "", Dictionary<string, string> headers =  null, WwwDeducedError error = WwwDeducedError.NoError)
    {
        ResponseCode = responseCode;
        Body = responseBody;
        Headers = headers;
        Error = error;
    }
}
}


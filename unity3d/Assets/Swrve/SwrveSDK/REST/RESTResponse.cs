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


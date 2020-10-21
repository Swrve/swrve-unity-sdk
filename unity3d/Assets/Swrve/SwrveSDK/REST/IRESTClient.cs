using System;
using System.Collections;
using System.Collections.Generic;

namespace SwrveUnity.REST
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


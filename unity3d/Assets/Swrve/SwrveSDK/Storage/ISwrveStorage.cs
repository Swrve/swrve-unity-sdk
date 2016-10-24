using System;

namespace SwrveUnity
{
/// <summary>
/// Used internally to define a common storage for events, click thrus and other persistent data.
/// </summary>
public interface ISwrveStorage
{
    void Save (string tag, string data, string userId = null);

    string Load (string tag, string userId = null);

    void Remove (string tag, string userId = null);

    void SetSecureFailedListener (Action callback);

    void SaveSecure (string tag, string data, string userId = null);

    string LoadSecure (string tag, string userId = null);
}
}


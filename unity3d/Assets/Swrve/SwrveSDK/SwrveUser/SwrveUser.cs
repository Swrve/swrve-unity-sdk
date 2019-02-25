#if UNITY_IPHONE || UNITY_ANDROID || UNITY_STANDALONE
#define SWRVE_SUPPORTED_PLATFORM
#endif

using System;
using System.Reflection;
using SwrveUnity.Helpers;
using UnityEngine;

namespace SwrveUnity.SwrveUsers
{
/// <summary>
/// Used internally to keep User info.
/// </summary>
[Serializable]
public class SwrveUser: IEquatable<SwrveUser>
{
    /// <summary>
    /// swrveId is an Iternal UserId that is provided by swrve.
    /// </summary>
    public string swrveId;
    /// <summary>
    /// externalId is an External UserId that might be provided by the final custumer using the IdentifyCall in our SDK,
    /// if the user call the identify API and don't provide it, a random UUID will be generated for this externalId.
    /// </summary>
    public string externalId;
    /// <summary>
    /// verified is a boolean that is related if this SwrveUser was already validate by our servers.
    /// </summary>
    public bool verified;

    /// <summary>
    /// Initialise an SwrveUser passing each a given externalId, swrveId and verified.
    /// </summary>
    /// <param name="swrveId">
    /// SwrveId is an internalId for this user.
    /// </param>
    /// <param name="externalId">
    /// ExternalId is an externalId for this user.
    /// </param>
    /// <param name="verified">
    /// Verified mean if this user already was veryfied by Swrve.
    /// </param>
    public SwrveUser(string swrveId, string externalId, bool verified)
    {
        this.externalId = externalId;
        this.swrveId = swrveId;
        this.verified = verified;
    }

    #region IEquatable<SwrveUser> Members
    // Implementation of IEquatable<SwrveUser> interface,
    // so we can compare SwrveUser using methods from a "List<SwrveUser>" like: "swrveUsers.Contains(swrveUser)".
    bool IEquatable<SwrveUser>.Equals(SwrveUser other)
    {
        if (other == null) return false;
        // Check comparing the externalId and swrveId. We don't compare the verified flag.
        if(other.externalId == externalId && swrveId == other.swrveId) {
            return true;
        }
        return false;
    }
    #endregion
}

}

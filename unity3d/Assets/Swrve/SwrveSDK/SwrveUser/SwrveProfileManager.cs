
#if UNITY_IPHONE || UNITY_ANDROID || UNITY_STANDALONE
#define SWRVE_SUPPORTED_PLATFORM
#endif
using System;
using System.Collections.Generic;
using SwrveUnity;
using SwrveUnity.Helpers;
using UnityEngine;

namespace SwrveUnity.SwrveUsers
{
/// <summary>
/// Swrve profile manager for Swrve SDK. (responsable to load/save from cache Swrve users.)
/// </summary>
public class SwrveProfileManager
{
    /// <summary>
    /// Key used to store the current userId.
    /// </summary>
    private const string SwrveUserIdKey = "Swrve.deviceUniqueIdentifier";
    /// <summary>
    /// Key used to store the current cache of users.
    /// </summary>
    private const string SwrveUsersKey = "swrve_users";
    /// <summary>
    /// UserId for the current user.
    /// </summary>
    public string userId;
    /// <summary>
    /// Boolean that will define if the current user is a new user or not.
    /// </summary>
    public bool isNewUser;

    /// <summary>
    /// SwrveProfileManager is a class that is reponsable to load from cache and manage any info related with a SwrveUser.
    /// </summary>
    public SwrveProfileManager(SwrveInitMode initMode)
    {
        // Get User UUID from PlayerPrefs
        userId = PlayerPrefs.GetString (SwrveUserIdKey, null);
        if (initMode == SwrveInitMode.AUTO && string.IsNullOrEmpty (userId)) {
            PrepareAndSetUserId(userId);
        }
        
        if (string.IsNullOrEmpty (userId) ) {
            SwrveLog.Log("The userId is not currently set");
        } else {
            SwrveLog.Log("Your current user id is: " + userId);
        }
    }
    
    /// <summary>
    /// Generates a userId or populates the userId property if provided one
    /// </summary>
    public void PrepareAndSetUserId(string inputUserId = null)
    {   
        if (string.IsNullOrEmpty(inputUserId) == false) {
            if (userId != inputUserId) {
                userId = inputUserId;
                this.SaveSwrveUserId(userId);
            }
        } else if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(inputUserId)) {
            userId = SwrveHelper.GetRandomUUID();
            this.SaveSwrveUserId(userId);
        }
    }

    /// <summary>
    /// Update a SwrveUser in cache if is able to match the swrveUserId or externalUserId with an user in our cache, it also set this user as verified -
    /// this method is called with the internal SuccessCallback in our identify call.
    /// </summary>
    /// <param name="swrveUserId">
    /// InternalUserId to find and update in our cache.
    /// </param>
    /// <param name="externalUserId">
    /// ExternalUserId to find and update in our cache.
    /// </param>
    public void UpdateSwrveUser(string swrveUserId, string externalUserId)
    {
        if (swrveUserId == null) return;
        List<SwrveUser> swrveUsers = this.GetSwrveUsers();
        for (int i = 0; i < swrveUsers.Count; i++) {
            SwrveUser swrveUser = swrveUsers[i];
            if(swrveUser.swrveId == swrveUserId || swrveUser.externalId == externalUserId) {
                swrveUser.verified = true;
                swrveUser.swrveId = swrveUserId;
                swrveUser.externalId = externalUserId;
                this.SaveSwrveUsers(swrveUsers);
                return;
            }
        }
    }
    #region Save / Load Users from cache.

    /// <summary>
    /// Try load from cache the user with the respective UserId or ExternalUserId.
    /// </summary>
    public SwrveUser GetSwrveUser(string aUserId)
    {
        if (aUserId == null) return null;
        List<SwrveUser> swrveUsers = this.GetSwrveUsers();
        for (int i = 0; i < swrveUsers.Count; i++) {
            SwrveUser swrveUser = swrveUsers[i];
            if (swrveUser.externalId == aUserId) {
                return swrveUser;
            }
            if (swrveUser.swrveId == aUserId) {
                return swrveUser;
            }
        }
        return null;
    }

    /// <summary>
    /// Load the List<SwrveUser> from cache.
    /// </summary>
    /// <returns>
    /// List of Swrve users from cache.
    /// </returns>
    public List<SwrveUser> GetSwrveUsers()
    {
        List<SwrveUser> swrveUsers = new List<SwrveUser>();
        string usersJsonObj = PlayerPrefs.GetString(SwrveUsersKey, null);
        // If find something in cache, return it.
        if(!string.IsNullOrEmpty(usersJsonObj) || usersJsonObj == "[]") {
            swrveUsers.AddRange(MiniJsonHelper.FromJson<SwrveUser>(usersJsonObj));
        }
        return swrveUsers;
    }
    /// <summary>
    /// Remove the respective SwrveUser from cache
    /// </summary>
    /// <param name="aUserId">
    /// An InternalUserId or ExternalUser to find and remove from cache.
    /// </param>
    public void RemoveSwrveUser(string aUserId)
    {
        if (aUserId == null) return;
        List<SwrveUser> swrveUsers = this.GetSwrveUsers();
        for (int i = 0; i < swrveUsers.Count; i++) {
            SwrveUser swrveUser = swrveUsers[i];
            if(swrveUser.swrveId == aUserId || swrveUser.externalId == aUserId) {
                swrveUsers.Remove(swrveUser);
                this.SaveSwrveUsers(swrveUsers);
                return;
            }
        }
    }

    /// <summary>
    /// Save the respective SwrveUser in cache
    /// </summary>
    /// <param name="swrveUser">
    /// A SwrveUser that will be saved in cache.
    /// </param>
    public void SaveSwrveUser(SwrveUser swrveUser)
    {
        if(swrveUser == null) return;
        List<SwrveUser> swrveUsers = this.GetSwrveUsers();
        for (int i = 0; i < swrveUsers.Count; i++) {
            SwrveUser cachedSwrveUser = swrveUsers[i];
            if(cachedSwrveUser.swrveId == swrveUser.swrveId || cachedSwrveUser.externalId == swrveUser.externalId) {
                cachedSwrveUser.swrveId = swrveUser.swrveId;
                cachedSwrveUser.externalId = swrveUser.externalId;
                this.SaveSwrveUsers(swrveUsers);
                return;
            }
        }
        // Cant find it on cache, add a new user.
        swrveUsers.Add(swrveUser);
        this.SaveSwrveUsers(swrveUsers);
    }
    /// <summary>
    /// Save the respective userId in cache this is used as the "Current" user.
    /// </summary>
    /// <param name="userId">
    /// An InternalUserId or ExternalUser to save in cache.
    /// </param>
    public void SaveSwrveUserId(string userId)
    {
        PlayerPrefs.SetString (SwrveUserIdKey, userId);
        PlayerPrefs.Save ();
    }

    /// <summary>
    /// Save the respective SwrveUser in cache
    /// </summary>
    /// <param name="swrveUsers">
    /// A List of user that will be cached (This method replace the preview list)
    /// </param>
    private void SaveSwrveUsers(List<SwrveUser> swrveUsers)
    {
        if(swrveUsers == null) return;
        string swrveUsersJsonObj = MiniJsonHelper.ToJson(swrveUsers.ToArray());
        PlayerPrefs.SetString(SwrveUsersKey, swrveUsersJsonObj);
    }
    #endregion
}

}

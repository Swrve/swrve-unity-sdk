using System;
using System.Collections.Generic;
using SwrveUnity;
using SwrveUnity.Helpers;
using UnityEngine;

namespace SwrveUnity.SwrveUsers
{
public class SwrveProfileManager
{
    private const string SwrveUserIdKey = "Swrve.deviceUniqueIdentifier";
    private const string SwrveUsersKey = "swrve_users";
    public string userId;
    public bool isNewUser; // Boolean that will define if the current user is a new user or not.
    private SwrveTrackingState trackingState;

    public SwrveProfileManager(SwrveInitMode initMode)
    {
        userId = PlayerPrefs.GetString(SwrveUserIdKey, null); // Get User UUID from PlayerPrefs
        if (initMode == SwrveInitMode.AUTO && string.IsNullOrEmpty(userId)) {
            InitUserId(userId);
        }

        if (string.IsNullOrEmpty (userId) ) {
            SwrveLog.Log("The userId is not currently set");
        } else {
            SwrveLog.Log("Your current user id is: " + userId);
        }

        trackingState = SwrveTracking.GetTrackingState();
    }

    public void InitUserId(string inputUserId = null)
    {
        if (string.IsNullOrEmpty(inputUserId) == false) {
            if (userId != inputUserId) {
                userId = inputUserId;
                SaveSwrveUserId(userId);
            }
        } else if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(inputUserId)) {
            userId = SwrveHelper.GetRandomUUID();
            SaveSwrveUserId(userId);
        }
    }

    public SwrveTrackingState GetTrackingState()
    {
        return trackingState;
    }

    public void SetTrackingState(SwrveTrackingState trackingState)
    {
        this.trackingState = trackingState;
        SwrveTracking.SaveTrackingState(trackingState);
    }

    // Update a SwrveUser in cache if is able to match the swrveUserId or externalUserId with an user in our cache, it also set this user as verified -
    // this method is called with the internal SuccessCallback in our identify call.
    public void UpdateSwrveUser(string swrveUserId, string externalUserId)
    {
        if (swrveUserId == null) {
            return;
        }
        List<SwrveUser> swrveUsers = this.GetSwrveUsers();
        for(int i = 0; i < swrveUsers.Count; i++) {
            SwrveUser swrveUser = swrveUsers[i];
            if (swrveUser.swrveId == swrveUserId || swrveUser.externalId == externalUserId) {
                swrveUser.verified = true;
                swrveUser.swrveId = swrveUserId;
                swrveUser.externalId = externalUserId;
                SaveSwrveUsers(swrveUsers);
                return;
            }
        }
    }

    #region Save / Load Users from cache.

    public SwrveUser GetSwrveUser(string aUserId)
    {
        if (aUserId == null) {
            return null;
        }
        List<SwrveUser> swrveUsers = this.GetSwrveUsers();
        for(int i = 0; i < swrveUsers.Count; i++) {
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

    public List<SwrveUser> GetSwrveUsers()
    {
        List<SwrveUser> swrveUsers = new List<SwrveUser>();
        string usersJsonObj = PlayerPrefs.GetString(SwrveUsersKey, null);
        // If find something in cache, return it.
        if (!string.IsNullOrEmpty(usersJsonObj) || usersJsonObj == "[]") {
            swrveUsers.AddRange(MiniJsonHelper.FromJson<SwrveUser>(usersJsonObj));
        }
        return swrveUsers;
    }

    public void RemoveSwrveUser(string aUserId)
    {
        if (aUserId == null) {
            return;
        }
        List<SwrveUser> swrveUsers = this.GetSwrveUsers();
        for(int i = 0; i < swrveUsers.Count; i++) {
            SwrveUser swrveUser = swrveUsers[i];
            if (swrveUser.swrveId == aUserId || swrveUser.externalId == aUserId) {
                swrveUsers.Remove(swrveUser);
                this.SaveSwrveUsers(swrveUsers);
                return;
            }
        }
    }

    public void SaveSwrveUser(SwrveUser swrveUser)
    {
        if (swrveUser == null) {
            return;
        }
        List<SwrveUser> swrveUsers = GetSwrveUsers();
        for(int i = 0; i < swrveUsers.Count; i++) {
            SwrveUser cachedSwrveUser = swrveUsers[i];
            if (cachedSwrveUser.swrveId == swrveUser.swrveId || cachedSwrveUser.externalId == swrveUser.externalId) {
                cachedSwrveUser.swrveId = swrveUser.swrveId;
                cachedSwrveUser.externalId = swrveUser.externalId;
                SaveSwrveUsers(swrveUsers);
                return;
            }
        }
        // Cant find it on cache, add a new user.
        swrveUsers.Add(swrveUser);
        SaveSwrveUsers(swrveUsers);
    }

    public void SaveSwrveUserId(string userId)
    {
        PlayerPrefs.SetString(SwrveUserIdKey, userId);
        PlayerPrefs.Save();
    }

    private void SaveSwrveUsers(List<SwrveUser> swrveUsers)
    {
        if (swrveUsers == null) {
            return;
        }
        string swrveUsersJsonObj = MiniJsonHelper.ToJson(swrveUsers.ToArray());
        PlayerPrefs.SetString(SwrveUsersKey, swrveUsersJsonObj);
    }

    #endregion
}

}

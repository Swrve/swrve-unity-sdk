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
using UnityEngine;
using Swrve.Helpers;

namespace Swrve.Storage
{
/// <summary>
/// Used internally to persist data to the Unity player preferences.
/// </summary>
public class SwrvePlayerPrefsStorage : ISwrveStorage
{
    public virtual void Save (string tag, string data, string userId = null)
    {
        bool saved = false;

        try {
            string prefPath = tag + ((userId == null) ? string.Empty : userId);
            SwrveLog.Log ("Setting " + prefPath + " in PlayerPrefs", "storage");
            PlayerPrefs.SetString (prefPath, data);
            saved = true;
        } catch (PlayerPrefsException ppe) {
            SwrveLog.LogError (ppe.ToString (), "storage");
        }

        if (!saved) {
            SwrveLog.LogWarning (tag + " not saved!", "storage");
        }
    }

    public virtual string Load (string tag, string userId = null)
    {
        string result = null;
        try {
            string prefPath = tag + ((userId == null) ? string.Empty : userId);
            if (PlayerPrefs.HasKey (prefPath)) {
                SwrveLog.Log ("Got " + tag + " from PlayerPrefs", "storage");
                result = PlayerPrefs.GetString (prefPath);
            }
        } catch (PlayerPrefsException ppe) {
            SwrveLog.LogError (ppe.ToString (), "storage");
        }

        return result;
    }

    public virtual void Remove (string tag, string userId = null)
    {
        try {
            string prefPath = tag + ((userId == null) ? string.Empty : userId);
            SwrveLog.Log ("Setting " + prefPath + " to null", "storage");
            PlayerPrefs.SetString (prefPath, null);
        } catch (PlayerPrefsException ppe) {
            SwrveLog.LogError (ppe.ToString ());
        }
    }

    public void SetSecureFailedListener (Action callback)
    {
        // No security
    }

    public virtual void SaveSecure (string tag, string data, string userId = null)
    {
        Save (tag, data, userId);
    }

    public virtual string LoadSecure (string tag, string userId = null)
    {
        return Load (tag, userId);
    }

}
}


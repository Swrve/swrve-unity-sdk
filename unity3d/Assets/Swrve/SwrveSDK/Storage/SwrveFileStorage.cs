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
/// Used internally to persist data to the disk.
/// </summary>
public class SwrveFileStorage : ISwrveStorage
{
    const string SIGNATURE_SUFFIX = "_SGT";
    protected string swrvePath;
    protected string uniqueKey;
    protected Action callback;

    public SwrveFileStorage (string swrvePath, string uniqueKey)
    {
        this.swrvePath = swrvePath;
        this.uniqueKey = uniqueKey;
    }

    public virtual void Save (string tag, string data, string userId = null)
    {
        if (string.IsNullOrEmpty (data)) {
            return;
        }

        bool saved = false;

        try {
            string saveFileName = GetFileName (tag, userId);
            SwrveLog.Log ("Saving: " + saveFileName, "storage");
            CrossPlatformFile.SaveText (saveFileName, data);
            saved = true;
        } catch (Exception e) {
            SwrveLog.LogError (e.ToString (), "storage");
        }

        if (!saved) {
            SwrveLog.LogWarning (tag + " not saved!", "storage");
        }
    }

    public virtual void SaveSecure (string tag, string data, string userId = null)
    {
        if (string.IsNullOrEmpty (data)) {
            return;
        }

        bool saved = false;

        try {
            string saveFileName = GetFileName (tag, userId);
            SwrveLog.Log ("Saving: " + saveFileName, "storage");
            CrossPlatformFile.SaveText (saveFileName, data);
            string signatureFileName = saveFileName + SIGNATURE_SUFFIX;
            string signature = SwrveHelper.CreateHMACMD5 (data, uniqueKey);

            CrossPlatformFile.SaveText (signatureFileName, signature);
            saved = true;
        } catch (Exception e) {
            SwrveLog.LogError (e.ToString (), "storage");
        }

        if (!saved) {
            SwrveLog.LogWarning (tag + " not saved!", "storage");
        }
    }

    public virtual string Load (string tag, string userId = null)
    {
        string result = null;
        try {
            // Read from file
            string loadFileName = GetFileName (tag, userId);
            if (CrossPlatformFile.Exists (loadFileName)) {
                result = CrossPlatformFile.LoadText (loadFileName);
            } else {
                // Skipping file load, doesn't exist
            }
        } catch (Exception e) {
            SwrveLog.LogError (e.ToString (), "storage");
        }

        return result;
    }

    public virtual void Remove (string tag, string userId = null)
    {
        try {
            // Read from file
            string deleteFilename = GetFileName (tag, userId);
            if (CrossPlatformFile.Exists (deleteFilename)) {
                SwrveLog.Log ("Removing: " + deleteFilename, "storage");
                CrossPlatformFile.Delete (deleteFilename);
            } else {
                // Skipping file removal, doesn't exist
            }

            string signatureFileName = deleteFilename + SIGNATURE_SUFFIX;
            if (CrossPlatformFile.Exists (signatureFileName)) {
                CrossPlatformFile.Delete (signatureFileName);
            }
        } catch (Exception e) {
            SwrveLog.LogError (e.ToString (), "storage");
        }
    }

    public void SetSecureFailedListener (Action callback)
    {
        this.callback = callback;
    }

    public virtual string LoadSecure (string tag, string userId = null)
    {
        string result = null;

        // Read from file
        string loadFileName = GetFileName (tag, userId);
        string signatureFileName = loadFileName + SIGNATURE_SUFFIX;
        if (CrossPlatformFile.Exists (loadFileName)) {
            result = CrossPlatformFile.LoadText (loadFileName);

            if (!string.IsNullOrEmpty (result)) {
                string signature = null;
                if (CrossPlatformFile.Exists (signatureFileName)) {
                    signature = CrossPlatformFile.LoadText (signatureFileName);
                } else {
                    SwrveLog.LogError ("Could not read signature file: " + signatureFileName);
                    result = null;
                }

                if (!string.IsNullOrEmpty (signature)) {
                    string computedSignature = SwrveHelper.CreateHMACMD5 (result, uniqueKey);

                    if (string.IsNullOrEmpty (computedSignature)) {
                        SwrveLog.LogError ("Could not compute signature for data in file " + loadFileName);
                        result = null;
                    } else {
                        if (!signature.Equals (computedSignature)) {
                            // Notify of invalid signature
                            if (callback != null) {
                                callback.Invoke ();
                            }
                            SwrveLog.LogError ("Signature validation failed for " + loadFileName);
                            result = null;
                        }
                    }
                }
            } else {
                SwrveLog.LogError ("Could not read file " + loadFileName);
            }
        } else {
            // No cache available
        }

        return result;
    }

    private string GetFileName (string tag, string userId)
    {
        string sep = swrvePath.Length > 0 ? "/" : string.Empty;
        return swrvePath + sep + tag + ((userId == null) ? string.Empty : userId);
    }
}
}


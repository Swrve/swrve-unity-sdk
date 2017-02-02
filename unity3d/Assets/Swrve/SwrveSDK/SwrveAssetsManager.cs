﻿using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SwrveUnity.Helpers;

namespace SwrveUnity
{
public class SwrveAssetsManager : ISwrveAssetsManager
{
    private MonoBehaviour Container;
    private string SwrveTemporaryPath;

    public SwrveAssetsManager(MonoBehaviour container, string swrveTemporaryPath)
    {
        Container = container;
        SwrveTemporaryPath = swrveTemporaryPath;
        AssetsOnDisk = new HashSet<string>();
    }

    public string CdnImages
    {
        get;
        set;
    }

    public string CdnFonts
    {
        get;
        set;
    }

    public HashSet<string> AssetsOnDisk
    {
        get;
        set;
    }

    public IEnumerator DownloadAssets(HashSet<SwrveAssetsQueueItem> assetsQueue, Action callBack)
    {
        yield return StartTask ("SwrveAssetsManager.DownloadAssetQueue", DownloadAssetQueue (assetsQueue));

        if(callBack!=null) {
            callBack.Invoke(); // AutoShowMessages;
        }
        TaskFinished("SwrveAssetsManager.DownloadAssets");
    }

    private IEnumerator DownloadAssetQueue(HashSet<SwrveAssetsQueueItem> assetsQueue)
    {
        IEnumerator<SwrveAssetsQueueItem> enumerator = assetsQueue.GetEnumerator();
        while (enumerator.MoveNext()) {
            SwrveAssetsQueueItem item = enumerator.Current;
            if (!CheckAsset(item.Name)) {
                yield return StartTask("SwrveAssetsManager.DownloadAsset", DownloadAsset(item));
            } else {
                AssetsOnDisk.Add(item.Name); // Already downloaded
            }
        }

        TaskFinished("SwrveAssetsManager.DownloadAssetQueue");
    }

    protected virtual IEnumerator DownloadAsset(SwrveAssetsQueueItem item)
    {
        string cdn = item.IsImage ? CdnImages : CdnFonts;
        string url = cdn + item.Name;
        SwrveLog.Log("Downloading asset: " + url);
        WWW www = new WWW(url);
        yield return www;
        WwwDeducedError err = UnityWwwHelper.DeduceWwwError(www);
        if (www != null && WwwDeducedError.NoError == err && www.isDone) {
            if(item.IsImage) {
                SaveImageAsset(item, www);
            } else {
                SaveBinaryAsset(item, www);
            }
        }
        TaskFinished("SwrveAssetsManager.DownloadAsset");
    }

    private bool CheckAsset(string fileName)
    {
        if (CrossPlatformFile.Exists(GetTemporaryPathFileName(fileName))) {
            return true;
        }
        return false;
    }

    private string GetTemporaryPathFileName(string fileName)
    {
        return Path.Combine(SwrveTemporaryPath, fileName);
    }

    protected virtual void SaveImageAsset(SwrveAssetsQueueItem item, WWW www)
    {
        Texture2D loadedTexture = www.texture;
        if (loadedTexture != null) {
            byte[] rawBytes = www.bytes;
            string sha1 = SwrveHelper.sha1(rawBytes);
            if(sha1 == item.Digest) {
                byte[] bytes = loadedTexture.EncodeToPNG();
                string filePath = GetTemporaryPathFileName(item.Name);
                SwrveLog.Log("Saving to " + filePath);
                CrossPlatformFile.SaveBytes(filePath, bytes);
                bytes = null;
                Texture2D.Destroy(loadedTexture);
                AssetsOnDisk.Add(item.Name);
            } else {
                SwrveLog.Log ("Error downloading image assetItem:" + item.Name + ". Did not match digest:" + sha1);
            }
        }
    }

    protected virtual void SaveBinaryAsset(SwrveAssetsQueueItem item, WWW www)
    {
        byte[] bytes = www.bytes;
        string sha1 = SwrveHelper.sha1(bytes);
        if(sha1 == item.Digest) {
            string filePath = GetTemporaryPathFileName(item.Name);
            SwrveLog.Log("Saving to " + filePath);
            CrossPlatformFile.SaveBytes(filePath, bytes);
            bytes = null;
            AssetsOnDisk.Add(item.Name);
        } else {
            SwrveLog.Log ("Error downloading binary assetItem:" + item.Name + ". Did not match digest:" + sha1);
        }
    }

    protected virtual Coroutine StartTask (string tag, IEnumerator task)
    {
        return Container.StartCoroutine(task);
    }

    protected virtual void TaskFinished(string tag)
    {
    }
}
}
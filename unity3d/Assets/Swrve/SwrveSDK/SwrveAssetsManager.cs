using UnityEngine;

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

        public string CdnImages { get; set; }

        public HashSet<string> AssetsOnDisk { get; set; }

        public delegate void MyDelegateType();

		public IEnumerator DownloadAssets(HashSet<SwrveAssetsQueueItem> assetsQueueImages, Action callBack)
		{
			yield return StartTask ("SwrveAssetsManager.DownloadAssetQueue", DownloadAssetQueue (assetsQueueImages));

            callBack.Invoke(); // AutoShowMessages;
            TaskFinished("SwrveAssetsManager.DownloadAssets");
		}

        private IEnumerator DownloadAssetQueue(HashSet<SwrveAssetsQueueItem> assetsQueueImages)
        {
            IEnumerator<SwrveAssetsQueueItem> enumerator = assetsQueueImages.GetEnumerator();
            while (enumerator.MoveNext())
            {
                SwrveAssetsQueueItem item = enumerator.Current;
                string asset = item.Name;
                if (!CheckAsset(asset))
                {
                    CoroutineReference<Texture2D> resultTexture = new CoroutineReference<Texture2D>();
                    yield return StartTask("SwrveAssetsManager.DownloadAsset", DownloadAsset(asset, resultTexture));
                    Texture2D texture = resultTexture.Value();
                    if (texture != null)
                    {
                        AssetsOnDisk.Add(asset);
                        Texture2D.Destroy(texture);
                    }
                }
                else
                {
                    AssetsOnDisk.Add(asset); // Already downloaded
                }
            }
            
            TaskFinished("SwrveAssetsManager.DownloadAssetQueue");
        }

        protected virtual IEnumerator DownloadAsset(string fileName, CoroutineReference<Texture2D> texture)
        {
            string url = CdnImages + fileName;
            SwrveLog.Log("Downloading asset: " + url);
            WWW www = new WWW(url);
            yield return www;
            WwwDeducedError err = UnityWwwHelper.DeduceWwwError(www);
            if (www != null && WwwDeducedError.NoError == err && www.isDone)
            {
                Texture2D loadedTexture = www.texture;
                if (loadedTexture != null)
                {
                    string filePath = GetTemporaryPathFileName(fileName);
                    SwrveLog.Log("Saving to " + filePath);
                    byte[] bytes = loadedTexture.EncodeToPNG();
                    CrossPlatformFile.SaveBytes(filePath, bytes);
                    bytes = null;

                    // Assign texture
                    texture.Value(loadedTexture);
                }
            }
            TaskFinished("SwrveAssetsManager.DownloadAsset");
        }

        private bool CheckAsset(string fileName)
        {
            if (CrossPlatformFile.Exists(GetTemporaryPathFileName(fileName)))
            {
                return true;
            }
            return false;
        }

        private string GetTemporaryPathFileName(string fileName)
        {
            return Path.Combine(SwrveTemporaryPath, fileName);
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

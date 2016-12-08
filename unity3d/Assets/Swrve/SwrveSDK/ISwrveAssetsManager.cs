using System;
using System.Collections;
using System.Collections.Generic;

namespace SwrveUnity
{
	/// <summary>
	/// Used internally to download assets to a file directory.
	/// </summary>
	public interface ISwrveAssetsManager
	{
		string CdnImages { get; set; }
		HashSet<string> AssetsOnDisk { get; set; }
		IEnumerator DownloadAssets(HashSet<SwrveAssetsQueueItem> assetsQueueImages, Action callBack);
	}
}

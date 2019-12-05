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
    string CdnImages {
        get;
        set;
    }
    string CdnFonts {
        get;
        set;
    }
    HashSet<string> AssetsOnDisk {
        get;
        set;
    }

    IEnumerator DownloadAnyMissingAssets (Action callBack);
    IEnumerator DownloadAssets(HashSet<SwrveAssetsQueueItem> assetsQueue, Action callBack);
    IEnumerator DownloadAssets(HashSet<SwrveAssetsQueueItem> assetsQueue, Action<object> callBack, object arg);
    IEnumerator DownloadAssets(HashSet<SwrveAssetsQueueItem> autoShowQueue,  HashSet<SwrveAssetsQueueItem> assetQueue, Action callBack);
}
}

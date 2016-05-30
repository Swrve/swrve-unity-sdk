using System.Collections.Generic;

public interface ISwrveAssetController {
    HashSet<string> GetAssetsOnDisk();
    bool IsAssetInCache(string asset);
}

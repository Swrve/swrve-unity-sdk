namespace SwrveUnity
{
public class SwrveAssetsQueueItem
{
    public SwrveAssetsQueueItem(string name, string digest, bool isImage, bool isExternalSource)
    {
        Name = name;
        Digest = digest;
        IsImage = isImage;
        IsExternalSource = isExternalSource;
    }

    public string Name
    {
        get;
        private set;
    }

    public string Digest
    {
        get;
        private set;
    }
    public bool IsImage
    {
        get;
        private set;
    }

    public bool IsExternalSource
    {
        get;
        private set;
    }

    public override bool Equals(object obj)
    {
        SwrveAssetsQueueItem item = obj as SwrveAssetsQueueItem;
        return item != null && item.Name == this.Name && item.Digest == this.Digest && item.IsImage == this.IsImage && item.IsExternalSource == this.IsExternalSource;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 23 + this.Name.GetHashCode();
        hash = hash * 23 + this.Digest.GetHashCode();
        hash = hash * 23 + this.IsImage.GetHashCode();
        return hash;
    }
}
}

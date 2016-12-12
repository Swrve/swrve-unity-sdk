namespace SwrveUnity
{
    public class SwrveAssetsQueueItem
    {
        public SwrveAssetsQueueItem(string name, string digest)
        {
            Name = name;
            Digest = digest;
        }

        public string Name { get; private set; }

        public string Digest { get; private set; }

        public override bool Equals(object obj)
        {
            SwrveAssetsQueueItem item = obj as SwrveAssetsQueueItem;
            return item != null && item.Name == this.Name && item.Digest == this.Digest;
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() ^ this.Digest.GetHashCode();
        }
    }
}

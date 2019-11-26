namespace FolderStats 
{
    public class FileStatistics
    {
        public long Count { get; set; }
        public long Size { get; set; }
        public long Lines { get; set; }

        public FileStatistics CombineWith(FileStatistics? other)
        {
            var clone = (FileStatistics) MemberwiseClone();
            if (other == null)
                return clone;
            clone.Count += other.Count;
            clone.Size += other.Size;
            clone.Lines += other.Lines;
            return clone;
        }
    }
}

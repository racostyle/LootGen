namespace GUI.Models
{
    public readonly struct STableFile
    {
        public string RelativePath { get; }

        public string Contents { get; }

        public STableFile(string relativePath, string contents)
        {
            RelativePath = relativePath;
            Contents = contents;
        }
    }
}

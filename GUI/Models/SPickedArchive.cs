namespace GUI.Models
{
    public readonly struct SPickedArchive
    {
        public string FileName { get; }

        public byte[] Contents { get; }

        public SPickedArchive(string fileName, byte[] contents)
        {
            FileName = fileName;
            Contents = contents;
        }
    }
}

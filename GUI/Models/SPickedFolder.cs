namespace GUI.Models
{
    public readonly struct SPickedFolder
    {
        public string FolderName { get; }

        public IReadOnlyList<STableFile> Files { get; }

        public SPickedFolder(string folderName, IReadOnlyList<STableFile> files)
        {
            FolderName = folderName;
            Files = files;
        }
    }
}

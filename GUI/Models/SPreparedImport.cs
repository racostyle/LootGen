namespace GUI.Models
{
    public enum ImportCollision
    {
        None,
        ReplacesImported,
        AvoidsBuiltIn,
        AvoidsBuiltInAndReplacesImported
    }

    public readonly struct SPreparedImport
    {
        public string ProfileName { get; }

        public string SourceFolderName { get; }

        public IReadOnlyList<STableFile> Files { get; }

        public ImportCollision Collision { get; }

        public SPreparedImport(
            string profileName,
            string sourceFolderName,
            IReadOnlyList<STableFile> files,
            ImportCollision collision)
        {
            ProfileName = profileName;
            SourceFolderName = sourceFolderName;
            Files = files;
            Collision = collision;
        }
    }
}

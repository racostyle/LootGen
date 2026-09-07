namespace GUI.Abstractions
{
    public interface IAppFileSystem
    {
        string Root { get; }

        bool FileExists(string relativePath);

        string ReadAllText(string relativePath);

        void WriteAllText(string relativePath, string contents);

        bool DirectoryExists(string relativePath);

        void CreateDirectory(string relativePath);

        void DeleteDirectory(string relativePath);

        void MoveDirectory(string relativeFrom, string relativeTo);

        IReadOnlyList<string> GetDirectoryNames(string relativePath);

        IReadOnlyList<string> GetRelativeFilePaths(string relativeDirectory, string searchPattern);

        string GetAbsolutePath(string relativePath);
    }
}

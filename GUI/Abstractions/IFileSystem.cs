namespace GUI.Abstractions
{
    public interface IFileSystem
    {
        string Root { get; }

        bool FileExists(string relativePath);

        string ReadAllText(string relativePath);

        void WriteAllText(string relativePath, string contents);

        bool DirectoryExists(string relativePath);

        void CreateDirectory(string relativePath);
    }
}

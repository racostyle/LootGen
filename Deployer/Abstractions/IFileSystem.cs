namespace Deployer.Abstractions
{
    public interface IFileSystem
    {
        bool FileExists(string path);

        bool DirectoryExists(string path);

        void CreateDirectory(string path);

        void DeleteFile(string path);

        void DeleteDirectory(string path, bool recursive);

        void CopyFile(string source, string destination, bool overwrite);

        string ReadAllText(string path);

        void WriteAllText(string path, string contents);

        IReadOnlyList<string> EnumerateFiles(string directory, string searchPattern, bool recursive);

        IReadOnlyList<string> EnumerateDirectories(string directory, string searchPattern);

        string GetFullPath(string path);

        string Combine(params string[] paths);

        string? GetDirectoryName(string path);

        string GetFileName(string path);

        string GetFileNameWithoutExtension(string path);

        string? GetParentPath(string path);

        string GetTempFileName(string fileName);
    }
}

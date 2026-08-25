using Deployer.Abstractions;

namespace Deployer.Infrastructure
{
    public sealed class PhysicalFileSystem : IFileSystem
    {
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive);
            }
        }

        public void CopyFile(string source, string destination, bool overwrite)
        {
            var directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(source, destination, overwrite);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllText(string path, string contents)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, contents);
        }

        public IReadOnlyList<string> EnumerateFiles(string directory, string searchPattern, bool recursive)
        {
            if (!Directory.Exists(directory))
            {
                return [];
            }

            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.GetFiles(directory, searchPattern, option);
        }

        public IReadOnlyList<string> EnumerateDirectories(string directory, string searchPattern)
        {
            if (!Directory.Exists(directory))
            {
                return [];
            }

            return Directory.GetDirectories(directory, searchPattern);
        }

        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        public string Combine(params string[] paths)
        {
            return Path.Combine(paths);
        }

        public string? GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }

        public string GetFileNameWithoutExtension(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        public string? GetParentPath(string path)
        {
            return Directory.GetParent(path)?.FullName;
        }

        public string GetTempFileName(string fileName)
        {
            var directory = Path.Combine(Path.GetTempPath(), "LootGenDeployer");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, fileName);
        }
    }
}

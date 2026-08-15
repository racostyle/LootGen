using GUI.Abstractions;
using Microsoft.Extensions.Logging;

namespace GUI.Infrastructure
{
    public sealed class MauiFileSystem : IAppFileSystem
    {
        private readonly ILogger<MauiFileSystem> _logger;
        private readonly string _root;

        public MauiFileSystem(ILogger<MauiFileSystem> logger)
        {
            _logger = logger;
            _root = Path.GetFullPath(ResolveRoot());
            Directory.CreateDirectory(_root);
            _logger.LogInformation("User data root is {Root}", _root);
        }

        public string Root => _root;

        public bool FileExists(string relativePath)
        {
            return File.Exists(GetFullPath(relativePath));
        }

        public string ReadAllText(string relativePath)
        {
            return File.ReadAllText(GetFullPath(relativePath));
        }

        public void WriteAllText(string relativePath, string contents)
        {
            var fullPath = GetFullPath(relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, contents);
        }

        public bool DirectoryExists(string relativePath)
        {
            return Directory.Exists(GetFullPath(relativePath));
        }

        public void CreateDirectory(string relativePath)
        {
            Directory.CreateDirectory(GetFullPath(relativePath));
        }

        private string GetFullPath(string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

            var combined = Path.GetFullPath(Path.Combine(_root, relativePath));
            var rootWithSeparator = _root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                   + Path.DirectorySeparatorChar;

            if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(combined, _root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Path is outside the user data root.");
            }

            return combined;
        }

        private static string ResolveRoot()
        {
#if WINDOWS
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "LootGen");
#else
            return Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
#endif
        }
    }
}

using GUI.Abstractions;

namespace GUI.Infrastructure
{
    public sealed class FileAppPackage : IAppPackage
    {
        private readonly string _baseDirectory;

        public FileAppPackage()
            : this(AppContext.BaseDirectory)
        {
        }

        public FileAppPackage(string baseDirectory)
        {
            _baseDirectory = Path.GetFullPath(baseDirectory);
        }

        public IReadOnlyList<string> ListFiles(string relativeDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativeDirectory);
            var root = Path.GetFullPath(Path.Combine(_baseDirectory, relativeDirectory));
            if (!root.StartsWith(AppendSeparator(_baseDirectory), StringComparison.OrdinalIgnoreCase)
                && !string.Equals(root, _baseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Path is outside the application package root.");
            }

            if (!Directory.Exists(root))
            {
                return [];
            }

            return Directory.GetFiles(root, "*.json", SearchOption.AllDirectories)
                .Select(ToPackagePath)
                .ToArray();
        }

        public string ReadAllText(string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
            var fullPath = Path.GetFullPath(Path.Combine(_baseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(AppendSeparator(_baseDirectory), StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullPath, _baseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Path is outside the application package root.");
            }

            return File.ReadAllText(fullPath);
        }

        private string ToPackagePath(string fullPath)
        {
            var relative = Path.GetRelativePath(_baseDirectory, fullPath);
            return relative.Replace('\\', '/');
        }

        private static string AppendSeparator(string path)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   + Path.DirectorySeparatorChar;
        }
    }
}

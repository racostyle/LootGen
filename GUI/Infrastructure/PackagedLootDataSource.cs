using GUI.Abstractions;
using Microsoft.Extensions.Logging;

namespace GUI.Infrastructure
{
    public sealed class PackagedLootDataSource : IPackagedLootDataSource
    {
        public const string RelativeRoot = "packaged-data";

        private readonly IAppPackage _appPackage;
        private readonly IAppFileSystem _fileSystem;
        private readonly ILogger<PackagedLootDataSource> _logger;
        private readonly object _gate = new();
        private string? _materializedRoot;

        public PackagedLootDataSource(
            IAppPackage appPackage,
            IAppFileSystem fileSystem,
            ILogger<PackagedLootDataSource> logger)
        {
            _appPackage = appPackage;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public string EnsureMaterialized()
        {
            lock (_gate)
            {
                if (_materializedRoot is not null)
                {
                    return _materializedRoot;
                }

                var files = _appPackage.ListFiles("Data");
                _logger.LogInformation("Materializing {Count} packaged loot data files", files.Count);

                if (_fileSystem.DirectoryExists(RelativeRoot))
                {
                    _fileSystem.DeleteDirectory(RelativeRoot);
                }

                foreach (var packagePath in files)
                {
                    var relative = ToUserDataPath(packagePath);
                    var contents = _appPackage.ReadAllText(packagePath);
                    _fileSystem.WriteAllText(relative, contents);
                }

                _materializedRoot = _fileSystem.GetAbsolutePath(RelativeRoot);
                _logger.LogInformation("Packaged loot data root is {Root}", _materializedRoot);
                return _materializedRoot;
            }
        }

        private static string ToUserDataPath(string packagePath)
        {
            var normalized = packagePath.Replace('\\', '/').Trim('/');
            if (normalized.StartsWith("Data/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized["Data/".Length..];
            }

            var combined = Path.Combine(RelativeRoot, normalized.Replace('/', Path.DirectorySeparatorChar));
            return combined;
        }
    }
}

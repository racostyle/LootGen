using GUI.Abstractions;
using GUI.Services;
using Microsoft.Extensions.Logging;

namespace GUI.Infrastructure
{
    public sealed class ResourceCatalog : IResourceCatalog
    {
        private readonly ILogger<ResourceCatalog> _logger;
        private readonly string _dataRoot;

        public ResourceCatalog(
            ILogger<ResourceCatalog> logger,
            IPackagedLootDataSource packagedLootDataSource)
            : this(logger, packagedLootDataSource.EnsureMaterialized())
        {
        }

        public ResourceCatalog(ILogger<ResourceCatalog> logger, string dataRoot)
        {
            _logger = logger;
            _dataRoot = Path.GetFullPath(dataRoot);
            _logger.LogInformation("Resource data root is {Root}", _dataRoot);
        }

        public IReadOnlyList<string> GetProfileNames()
        {
            if (!Directory.Exists(_dataRoot))
            {
                _logger.LogWarning("Resource data folder does not exist at {Root}", _dataRoot);
                return [];
            }

            return Directory.GetDirectories(_dataRoot)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray()!;
        }

        public string GetProfilePath(string profileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(profileName);
            if (profileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException("Profile name is not a valid folder name.", nameof(profileName));
            }

            var combined = Path.GetFullPath(Path.Combine(_dataRoot, profileName));
            var rootWithSeparator = _dataRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                   + Path.DirectorySeparatorChar;

            if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(combined, _dataRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Path is outside the resource data root.");
            }

            return combined;
        }
    }
}

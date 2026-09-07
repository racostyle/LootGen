using GUI.Abstractions;
using GUI.Models;
using GUI.Services;
using Microsoft.Extensions.Logging;

namespace GUI.Infrastructure
{
    public sealed class ResourceCatalog : IResourceCatalog
    {
        private readonly ILogger<ResourceCatalog> _logger;
        private readonly IUserLootProfileStore _userStore;
        private readonly IAppFileSystem _fileSystem;
        private int _revision;

        public ResourceCatalog(
            ILogger<ResourceCatalog> logger,
            IPackagedLootDataSource packagedLootDataSource,
            IUserLootProfileStore userStore,
            IAppFileSystem fileSystem)
        {
            _logger = logger;
            _userStore = userStore;
            _fileSystem = fileSystem;
            var packagedRoot = packagedLootDataSource.EnsureMaterialized();
            _logger.LogInformation("Resource data root is {Root}", packagedRoot);
            RelocateUserProfilesThatMatchBuiltIns();
        }

        public int Revision => _revision;

        public IReadOnlyList<string> GetProfileNames()
        {
            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in GetPackagedProfileNames())
            {
                names[name] = name;
            }

            foreach (var name in _userStore.GetProfileNames())
            {
                if (!names.ContainsKey(name))
                {
                    names[name] = name;
                }
            }

            return names.Values
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public bool IsBuiltInProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                return false;
            }

            return GetPackagedProfileNames().Any(name =>
                string.Equals(name, profileName, StringComparison.OrdinalIgnoreCase));
        }

        public string GetProfilePath(string profileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(profileName);
            if (IsBuiltInProfile(profileName))
            {
                return _fileSystem.GetAbsolutePath(GetPackagedRelativePath(profileName));
            }

            if (_userStore.Exists(profileName))
            {
                return _userStore.GetAbsoluteProfilePath(profileName);
            }

            return _fileSystem.GetAbsolutePath(GetPackagedRelativePath(profileName));
        }

        public bool IsUserProfile(string profileName)
        {
            return _userStore.Exists(profileName) && !IsBuiltInProfile(profileName);
        }

        public IReadOnlyList<STableFile> GetTableFiles(string profileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(profileName);

            var relativeRoot = IsBuiltInProfile(profileName)
                ? GetPackagedRelativePath(profileName)
                : _userStore.Exists(profileName)
                    ? _userStore.GetRelativeProfilePath(profileName)
                    : GetPackagedRelativePath(profileName);

            if (!_fileSystem.DirectoryExists(relativeRoot))
            {
                return [];
            }

            var files = new List<STableFile>();
            foreach (var relativePath in _fileSystem.GetRelativeFilePaths(relativeRoot, "*.json"))
            {
                if (TableArchive.ShouldSkipTableFile(relativePath))
                {
                    continue;
                }

                var contents = _fileSystem.ReadAllText(Path.Combine(relativeRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                files.Add(new STableFile(relativePath, contents));
            }

            return files;
        }

        public void NotifyUserDataChanged()
        {
            _revision++;
            _logger.LogInformation("Resource catalog revision is now {Revision}", _revision);
        }

        private void RelocateUserProfilesThatMatchBuiltIns()
        {
            foreach (var userName in _userStore.GetProfileNames().ToArray())
            {
                if (!IsBuiltInProfile(userName))
                {
                    continue;
                }

                var target = UniqueRelocationName(userName);
                _userStore.Rename(userName, target);
                _logger.LogWarning(
                    "Moved imported profile {From} to {To} because built-in folders are never overwritten",
                    userName,
                    target);
                _revision++;
            }
        }

        private string UniqueRelocationName(string builtInName)
        {
            var candidate = TableArchive.ToImportedProfileName(builtInName);
            var n = 2;
            while (IsBuiltInProfile(candidate) || _userStore.Exists(candidate))
            {
                candidate = $"{builtInName}_imported{n}";
                n++;
            }

            return TableArchive.SanitizeProfileName(candidate);
        }

        private IReadOnlyList<string> GetPackagedProfileNames()
        {
            if (!_fileSystem.DirectoryExists(PackagedLootDataSource.RelativeRoot))
            {
                _logger.LogWarning("Resource data folder does not exist at {Root}", PackagedLootDataSource.RelativeRoot);
                return [];
            }

            return _fileSystem.GetDirectoryNames(PackagedLootDataSource.RelativeRoot);
        }

        private string GetPackagedRelativePath(string profileName)
        {
            if (profileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException("Profile name is not a valid folder name.", nameof(profileName));
            }

            var match = GetPackagedProfileNames().FirstOrDefault(name =>
                string.Equals(name, profileName, StringComparison.OrdinalIgnoreCase));
            var folder = match ?? profileName;
            var combined = Path.Combine(PackagedLootDataSource.RelativeRoot, folder);
            _ = _fileSystem.GetAbsolutePath(combined);
            return combined;
        }
    }
}

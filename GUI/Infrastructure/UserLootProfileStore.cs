using GUI.Abstractions;
using GUI.Models;
using GUI.Services;
using Microsoft.Extensions.Logging;

namespace GUI.Infrastructure
{
    public sealed class UserLootProfileStore : IUserLootProfileStore
    {
        public const string RelativeRoot = "user-data";

        private readonly IAppFileSystem _fileSystem;
        private readonly ILogger<UserLootProfileStore> _logger;

        public UserLootProfileStore(IAppFileSystem fileSystem, ILogger<UserLootProfileStore> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public IReadOnlyList<string> GetProfileNames()
        {
            if (!_fileSystem.DirectoryExists(RelativeRoot))
            {
                return [];
            }

            return _fileSystem.GetDirectoryNames(RelativeRoot);
        }

        public bool Exists(string profileName)
        {
            return TryGetActualName(profileName) is not null;
        }

        public string GetAbsoluteProfilePath(string profileName)
        {
            return _fileSystem.GetAbsolutePath(GetRelativeProfilePath(profileName));
        }

        public string GetRelativeProfilePath(string profileName)
        {
            var actual = TryGetActualName(profileName)
                         ?? TableArchive.SanitizeProfileName(profileName);
            return Path.Combine(RelativeRoot, actual);
        }

        public void Replace(string profileName, IReadOnlyList<STableFile> files)
        {
            ArgumentNullException.ThrowIfNull(files);
            var name = TryGetActualName(profileName) ?? TableArchive.SanitizeProfileName(profileName);
            var relativeRoot = Path.Combine(RelativeRoot, name);
            if (_fileSystem.DirectoryExists(relativeRoot))
            {
                _fileSystem.DeleteDirectory(relativeRoot);
            }

            foreach (var file in files)
            {
                var relative = Path.Combine(relativeRoot, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                _fileSystem.WriteAllText(relative, file.Contents);
            }

            _logger.LogInformation("Stored imported profile {Profile} with {Count} files", name, files.Count);
        }

        public void Rename(string fromName, string toName)
        {
            var actualFrom = TryGetActualName(fromName)
                             ?? throw new InvalidOperationException($"Imported profile {fromName} was not found.");
            var actualTo = TableArchive.SanitizeProfileName(toName);
            if (string.Equals(actualFrom, actualTo, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Exists(actualTo))
            {
                throw new InvalidOperationException($"Imported profile {actualTo} already exists.");
            }

            _fileSystem.MoveDirectory(
                Path.Combine(RelativeRoot, actualFrom),
                Path.Combine(RelativeRoot, actualTo));
            _logger.LogInformation("Renamed imported profile {From} to {To}", actualFrom, actualTo);
        }

        public void Delete(string profileName)
        {
            var actual = TryGetActualName(profileName);
            if (actual is null)
            {
                return;
            }

            _fileSystem.DeleteDirectory(Path.Combine(RelativeRoot, actual));
            _logger.LogInformation("Removed imported profile {Profile}", actual);
        }

        private string? TryGetActualName(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                return null;
            }

            return GetProfileNames().FirstOrDefault(name =>
                string.Equals(name, profileName, StringComparison.OrdinalIgnoreCase));
        }
    }
}

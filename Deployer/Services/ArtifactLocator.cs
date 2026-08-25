using Deployer.Abstractions;
using Deployer.Models;
using Microsoft.Extensions.Logging;

namespace Deployer.Services
{
    public sealed class ArtifactLocator : IArtifactLocator
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<ArtifactLocator> _logger;

        public ArtifactLocator(IFileSystem fileSystem, ILogger<ArtifactLocator> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public string? FindWindowsPublishFolder(string guiProjectDirectory)
        {
            var releaseRoot = _fileSystem.Combine(guiProjectDirectory, "bin", "Release", PublishConstants.WindowsTargetFramework);
            var ridCandidates = new[] { PublishConstants.WindowsRuntimeIdentifier, "win-x64" };
            foreach (var rid in ridCandidates)
            {
                var publishFolder = _fileSystem.Combine(releaseRoot, rid, "publish");
                if (_fileSystem.DirectoryExists(publishFolder) && HasExecutable(publishFolder))
                {
                    _logger.LogInformation("Windows publish folder is {Path}", publishFolder);
                    return publishFolder;
                }
            }

            foreach (var publishFolder in _fileSystem.EnumerateFiles(releaseRoot, "*.exe", recursive: true)
                         .Select(path => _fileSystem.GetDirectoryName(path))
                         .Where(directory => !string.IsNullOrEmpty(directory)
                                             && directory!.EndsWith("publish", StringComparison.OrdinalIgnoreCase))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Windows publish folder is {Path}", publishFolder);
                return publishFolder;
            }

            _logger.LogWarning("No Windows publish folder was found under {Root}", releaseRoot);
            return null;
        }

        public string? FindSignedApk(string guiProjectDirectory)
        {
            var androidRoot = _fileSystem.Combine(guiProjectDirectory, "bin", "Release", PublishConstants.AndroidTargetFramework);
            var apks = _fileSystem.EnumerateFiles(androidRoot, "*.apk", recursive: true);
            var signed = apks.FirstOrDefault(path =>
                path.Contains("-Signed", StringComparison.OrdinalIgnoreCase));
            var selected = signed ?? apks.LastOrDefault();
            if (selected is null)
            {
                _logger.LogWarning("No APK was found under {Root}", androidRoot);
                return null;
            }

            _logger.LogInformation("Selected APK {Path}", selected);
            return selected;
        }

        private bool HasExecutable(string folder)
        {
            return _fileSystem.EnumerateFiles(folder, "*.exe", recursive: false).Count > 0;
        }
    }
}

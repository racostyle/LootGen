using Deployer.Abstractions;
using Deployer.Models;
using Microsoft.Extensions.Logging;

namespace Deployer.Services
{
    public sealed class RepositoryLocator : IRepositoryLocator
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<RepositoryLocator> _logger;

        public RepositoryLocator(IFileSystem fileSystem, ILogger<RepositoryLocator> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public bool TryFindRoot(string startPath, out string root)
        {
            var current = _fileSystem.GetFullPath(startPath);
            while (!string.IsNullOrEmpty(current))
            {
                var solutionPath = _fileSystem.Combine(current, PublishConstants.SolutionFileName);
                var guiProjectPath = _fileSystem.Combine(current, "GUI", "GUI.csproj");
                if (_fileSystem.FileExists(solutionPath) && _fileSystem.FileExists(guiProjectPath))
                {
                    _logger.LogInformation("Repository root is {Root}", current);
                    root = current;
                    return true;
                }

                current = _fileSystem.GetParentPath(current);
            }

            _logger.LogWarning("Could not find LootGen repository root from {StartPath}", startPath);
            root = string.Empty;
            return false;
        }
    }
}

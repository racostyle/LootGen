using Deployer.Abstractions;
using Deployer.Models;
using Microsoft.Extensions.Logging;

namespace Deployer.Services
{
    public sealed class DeployService : IDeployService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IWindowsPackagePublisher _windowsPublisher;
        private readonly IAndroidApkPublisher _androidPublisher;
        private readonly ILogger<DeployService> _logger;

        public DeployService(
            IFileSystem fileSystem,
            IWindowsPackagePublisher windowsPublisher,
            IAndroidApkPublisher androidPublisher,
            ILogger<DeployService> logger)
        {
            _fileSystem = fileSystem;
            _windowsPublisher = windowsPublisher;
            _androidPublisher = androidPublisher;
            _logger = logger;
        }

        public async Task<SDeployResult> DeployAsync(SDeployRequest request, CancellationToken cancellationToken)
        {
            var error = Validate(request);
            if (error is not null)
            {
                _logger.LogWarning("Deploy validation failed: {Error}", error);
                return new SDeployResult
                {
                    Succeeded = false,
                    Message = error,
                    ArtifactPaths = []
                };
            }

            var outputDirectory = _fileSystem.GetFullPath(request.OutputDirectory);
            var normalized = request with
            {
                RepositoryRoot = _fileSystem.GetFullPath(request.RepositoryRoot),
                OutputDirectory = outputDirectory
            };

            _fileSystem.CreateDirectory(outputDirectory);
            _logger.LogInformation("Starting {Target} deploy for version {Version}", normalized.Target, normalized.DisplayVersion);

            var publishResult = normalized.Target == DeployTarget.WindowsPackage
                ? await _windowsPublisher.PublishAsync(normalized, cancellationToken).ConfigureAwait(false)
                : await _androidPublisher.PublishAsync(normalized, cancellationToken).ConfigureAwait(false);

            return new SDeployResult
            {
                Succeeded = publishResult.Succeeded,
                Message = publishResult.Message,
                ArtifactPaths = publishResult.ArtifactPaths
            };
        }

        private string? Validate(SDeployRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayVersion))
            {
                return "Display version is required (for example 1.0.0).";
            }

            if (request.ApplicationVersion <= 0)
            {
                return "Version code must be a positive integer. Increase it for every Android release.";
            }

            if (string.IsNullOrWhiteSpace(request.RepositoryRoot)
                || !_fileSystem.FileExists(_fileSystem.Combine(request.RepositoryRoot, "GUI", "GUI.csproj")))
            {
                return "Repository root must contain GUI/GUI.csproj.";
            }

            if (string.IsNullOrWhiteSpace(request.OutputDirectory))
            {
                return "Output folder is required.";
            }

            return null;
        }
    }
}

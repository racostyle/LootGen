using Deployer.Abstractions;
using Deployer.Models;
using Microsoft.Extensions.Logging;

namespace Deployer.Services
{
    public sealed class WindowsPackagePublisher : IWindowsPackagePublisher
    {
        private readonly IFileSystem _fileSystem;
        private readonly IProcessRunner _processRunner;
        private readonly IPublishCommandFactory _commandFactory;
        private readonly IArtifactLocator _artifactLocator;
        private readonly IArchiveService _archiveService;
        private readonly ILogger<WindowsPackagePublisher> _logger;

        public WindowsPackagePublisher(
            IFileSystem fileSystem,
            IProcessRunner processRunner,
            IPublishCommandFactory commandFactory,
            IArtifactLocator artifactLocator,
            IArchiveService archiveService,
            ILogger<WindowsPackagePublisher> logger)
        {
            _fileSystem = fileSystem;
            _processRunner = processRunner;
            _commandFactory = commandFactory;
            _artifactLocator = artifactLocator;
            _archiveService = archiveService;
            _logger = logger;
        }

        public async Task<SPublishResult> PublishAsync(SDeployRequest request, CancellationToken cancellationToken)
        {
            var guiProjectPath = _fileSystem.Combine(request.RepositoryRoot, "GUI", "GUI.csproj");
            var guiDirectory = _fileSystem.Combine(request.RepositoryRoot, "GUI");
            var command = _commandFactory.CreateWindowsPublish(
                guiProjectPath,
                guiDirectory,
                request.DisplayVersion,
                request.ApplicationVersion);

            _logger.LogInformation("Publishing Windows package {Version}", request.DisplayVersion);
            var processResult = await _processRunner.RunAsync(command, null, cancellationToken).ConfigureAwait(false);
            if (!processResult.Succeeded)
            {
                var errors = (processResult.OutputLines ?? [])
                    .Where(line => line.Contains(" error ", StringComparison.OrdinalIgnoreCase)
                                   || line.Contains(": error", StringComparison.OrdinalIgnoreCase))
                    .TakeLast(12)
                    .ToArray();
                var detail = errors.Length == 0
                    ? string.Empty
                    : Environment.NewLine + string.Join(Environment.NewLine, errors);
                return new SPublishResult
                {
                    Succeeded = false,
                    Message = $"Windows publish failed with exit code {processResult.ExitCode}.{detail}",
                    ArtifactPaths = []
                };
            }

            var publishFolder = _artifactLocator.FindWindowsPublishFolder(guiDirectory);
            if (publishFolder is null)
            {
                return new SPublishResult
                {
                    Succeeded = false,
                    Message = "Windows publish completed but the output folder was not found.",
                    ArtifactPaths = []
                };
            }

            _fileSystem.CreateDirectory(request.OutputDirectory);
            var zipName = $"{PublishConstants.AppTitle}-{request.DisplayVersion}-win-x64.zip";
            var zipPath = _fileSystem.Combine(request.OutputDirectory, zipName);
            var entryPrefix = $"{PublishConstants.AppTitle}-{request.DisplayVersion}-win-x64";
            _archiveService.CreateZipFromDirectory(publishFolder, zipPath, entryPrefix);
            _logger.LogInformation("Created GitHub artifact {Path}", zipPath);

            return new SPublishResult
            {
                Succeeded = true,
                Message = $"Windows package created. Upload {zipName} to a GitHub Release. Recipients unzip and run {PublishConstants.AppTitle}.exe.",
                ArtifactPaths = [zipPath]
            };
        }
    }
}

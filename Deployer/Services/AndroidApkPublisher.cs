using Deployer.Abstractions;
using Deployer.Models;
using Microsoft.Extensions.Logging;

namespace Deployer.Services
{
    public sealed class AndroidApkPublisher : IAndroidApkPublisher
    {
        private readonly IFileSystem _fileSystem;
        private readonly IProcessRunner _processRunner;
        private readonly IPublishCommandFactory _commandFactory;
        private readonly IArtifactLocator _artifactLocator;
        private readonly ILogger<AndroidApkPublisher> _logger;

        public AndroidApkPublisher(
            IFileSystem fileSystem,
            IProcessRunner processRunner,
            IPublishCommandFactory commandFactory,
            IArtifactLocator artifactLocator,
            ILogger<AndroidApkPublisher> logger)
        {
            _fileSystem = fileSystem;
            _processRunner = processRunner;
            _commandFactory = commandFactory;
            _artifactLocator = artifactLocator;
            _logger = logger;
        }

        public async Task<SPublishResult> PublishAsync(SDeployRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.KeystorePath) || !_fileSystem.FileExists(request.KeystorePath))
            {
                return Fail("Create or select an Android keystore before building an APK.");
            }

            if (string.IsNullOrWhiteSpace(request.KeystoreAlias))
            {
                return Fail("Android keystore alias is required.");
            }

            var password = request.KeyPassword ?? request.KeystorePassword;
            if (string.IsNullOrWhiteSpace(password))
            {
                return Fail("Android keystore password is required.");
            }

            var guiProjectPath = _fileSystem.Combine(request.RepositoryRoot, "GUI", "GUI.csproj");
            var guiDirectory = _fileSystem.Combine(request.RepositoryRoot, "GUI");
            var keystoreForBuild = CopyKeystoreToBuildFolder(guiDirectory, request.KeystorePath);
            var command = _commandFactory.CreateAndroidPublish(
                guiProjectPath,
                guiDirectory,
                request.DisplayVersion,
                request.ApplicationVersion,
                keystoreForBuild,
                request.KeystoreAlias.Trim()) with
            {
                EnvironmentVariables = new Dictionary<string, string>
                {
                    [PublishConstants.AndroidSigningPasswordEnvVar] = password
                }
            };

            _logger.LogInformation("Publishing Android APK {Version}", request.DisplayVersion);
            var processResult = await _processRunner.RunAsync(command, null, cancellationToken).ConfigureAwait(false);
            if (!processResult.Succeeded)
            {
                return Fail(FormatProcessFailure("Android publish failed", processResult));
            }

            var apkPath = _artifactLocator.FindSignedApk(guiDirectory);
            if (apkPath is null)
            {
                return Fail("Android publish completed but no APK was found.");
            }

            _fileSystem.CreateDirectory(request.OutputDirectory);
            var destination = _fileSystem.Combine(
                request.OutputDirectory,
                $"{PublishConstants.AppTitle}-{request.DisplayVersion}.apk");
            _fileSystem.CopyFile(apkPath, destination, overwrite: true);
            _logger.LogInformation("Copied APK to {Path}", destination);

            return new SPublishResult
            {
                Succeeded = true,
                Message = $"APK created. Upload {_fileSystem.GetFileName(destination)} to a GitHub Release. Recipients install from unknown sources (not Play Store).",
                ArtifactPaths = [destination]
            };
        }

        private string CopyKeystoreToBuildFolder(string guiDirectory, string keystorePath)
        {
            var signingDir = _fileSystem.Combine(guiDirectory, "obj", "deploy-signing");
            _fileSystem.CreateDirectory(signingDir);
            var destination = _fileSystem.Combine(signingDir, "lootgen.keystore");
            _fileSystem.CopyFile(keystorePath, destination, overwrite: true);
            return _fileSystem.GetFullPath(destination);
        }

        private static string FormatProcessFailure(string prefix, SProcessResult processResult)
        {
            var errors = (processResult.OutputLines ?? [])
                .Where(IsErrorLine)
                .TakeLast(12)
                .ToArray();

            if (errors.Length == 0)
            {
                return $"{prefix} with exit code {processResult.ExitCode}.";
            }

            return $"{prefix} with exit code {processResult.ExitCode}.{Environment.NewLine}{string.Join(Environment.NewLine, errors)}";
        }

        private static bool IsErrorLine(string line)
        {
            return line.Contains(" error ", StringComparison.OrdinalIgnoreCase)
                   || line.Contains(": error", StringComparison.OrdinalIgnoreCase)
                   || line.Contains("error MSB", StringComparison.OrdinalIgnoreCase)
                   || line.Contains("error XA", StringComparison.OrdinalIgnoreCase)
                   || line.Contains("java.exe", StringComparison.OrdinalIgnoreCase);
        }

        private static SPublishResult Fail(string message)
        {
            return new SPublishResult
            {
                Succeeded = false,
                Message = message,
                ArtifactPaths = []
            };
        }
    }
}

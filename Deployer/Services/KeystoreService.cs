using Deployer.Abstractions;
using Deployer.Models;
using Microsoft.Extensions.Logging;

namespace Deployer.Services
{
    public sealed class KeystoreService : IKeystoreService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IProcessRunner _processRunner;
        private readonly IJdkLocator _jdkLocator;
        private readonly ILogger<KeystoreService> _logger;

        public KeystoreService(
            IFileSystem fileSystem,
            IProcessRunner processRunner,
            IJdkLocator jdkLocator,
            ILogger<KeystoreService> logger)
        {
            _fileSystem = fileSystem;
            _processRunner = processRunner;
            _jdkLocator = jdkLocator;
            _logger = logger;
        }

        public async Task CreateAsync(SKeystoreCreateRequest request, CancellationToken cancellationToken)
        {
            if (!_jdkLocator.TryFindKeytool(out var keytoolPath))
            {
                throw new InvalidOperationException(
                    "JDK keytool was not found. Install a JDK (Visual Studio's Android OpenJDK is enough) and try again.");
            }

            var directory = _fileSystem.GetDirectoryName(request.KeystorePath);
            if (!string.IsNullOrEmpty(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            if (_fileSystem.FileExists(request.KeystorePath))
            {
                throw new InvalidOperationException($"A keystore already exists at {request.KeystorePath}.");
            }

            var workingDirectory = directory ?? _fileSystem.GetFullPath(".");
            var command = new SProcessStart
            {
                FileName = keytoolPath,
                WorkingDirectory = workingDirectory,
                Arguments =
                [
                    "-genkeypair",
                    "-keystore",
                    request.KeystorePath,
                    "-alias",
                    request.Alias,
                    "-keyalg",
                    "RSA",
                    "-keysize",
                    "2048",
                    "-validity",
                    "10000",
                    "-storepass",
                    request.Password,
                    "-keypass",
                    request.Password,
                    "-dname",
                    request.DistinguishedName
                ]
            };

            _logger.LogInformation("Creating Android keystore at {Path}", request.KeystorePath);
            var result = await _processRunner.RunAsync(command, null, cancellationToken).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"keytool failed with exit code {result.ExitCode}.");
            }
        }
    }
}

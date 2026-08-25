using Deployer.Abstractions;
using Microsoft.Extensions.Logging;

namespace Deployer.Infrastructure
{
    public sealed class JdkLocator : IJdkLocator
    {
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentInfo _environment;
        private readonly ILogger<JdkLocator> _logger;

        public JdkLocator(IFileSystem fileSystem, IEnvironmentInfo environment, ILogger<JdkLocator> logger)
        {
            _fileSystem = fileSystem;
            _environment = environment;
            _logger = logger;
        }

        public bool TryFindKeytool(out string keytoolPath)
        {
            foreach (var candidate in EnumerateCandidates())
            {
                if (_fileSystem.FileExists(candidate))
                {
                    _logger.LogInformation("Found keytool at {Path}", candidate);
                    keytoolPath = candidate;
                    return true;
                }
            }

            _logger.LogWarning("keytool was not found on this machine");
            keytoolPath = string.Empty;
            return false;
        }

        private IEnumerable<string> EnumerateCandidates()
        {
            var javaHome = _environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrWhiteSpace(javaHome))
            {
                yield return _fileSystem.Combine(javaHome, "bin", "keytool.exe");
            }

            var programFiles = _environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = _environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var localAppData = _environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            foreach (var root in new[]
                     {
                         _fileSystem.Combine(programFiles, "Microsoft"),
                         _fileSystem.Combine(programFiles, "Eclipse Adoptium"),
                         _fileSystem.Combine(programFiles, "Java"),
                         _fileSystem.Combine(programFiles, "Android", "jdk"),
                         _fileSystem.Combine(programFilesX86, "Android", "openjdk"),
                         _fileSystem.Combine(localAppData, "Programs", "Eclipse Adoptium")
                     })
            {
                foreach (var directory in _fileSystem.EnumerateDirectories(root, "jdk*"))
                {
                    yield return _fileSystem.Combine(directory, "bin", "keytool.exe");
                }
            }
        }
    }
}

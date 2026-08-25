using System.Text.Json;
using Deployer.Abstractions;
using Deployer.Models;
using Microsoft.Extensions.Logging;

namespace Deployer.Services
{
    public sealed class DeployerSettingsService : IDeployerSettingsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentInfo _environment;
        private readonly ILogger<DeployerSettingsService> _logger;
        private readonly string _settingsPath;
        private readonly string _settingsDirectory;

        public DeployerSettingsService(
            IFileSystem fileSystem,
            IEnvironmentInfo environment,
            ILogger<DeployerSettingsService> logger)
        {
            _fileSystem = fileSystem;
            _environment = environment;
            _logger = logger;
            var appData = _environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsDirectory = _fileSystem.Combine(appData, PublishConstants.AppFolderName, PublishConstants.SettingsFolderName);
            _settingsPath = _fileSystem.Combine(_settingsDirectory, PublishConstants.SettingsFileName);
        }

        public DeployerSettings Load()
        {
            if (!_fileSystem.FileExists(_settingsPath))
            {
                return new DeployerSettings
                {
                    KeystorePath = GetDefaultKeystorePath()
                };
            }

            try
            {
                var json = _fileSystem.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<DeployerSettings>(json, JsonOptions) ?? new DeployerSettings();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read deployer settings; using defaults");
                return new DeployerSettings
                {
                    KeystorePath = GetDefaultKeystorePath()
                };
            }
        }

        public void Save(DeployerSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            _fileSystem.WriteAllText(_settingsPath, json);
        }

        public string GetDefaultKeystorePath()
        {
            return _fileSystem.Combine(_settingsDirectory, PublishConstants.DefaultKeystoreFileName);
        }

        public string GetDefaultOutputDirectory(string repositoryRoot)
        {
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                return string.Empty;
            }

            return _fileSystem.Combine(repositoryRoot, PublishConstants.DistFolderName);
        }
    }
}

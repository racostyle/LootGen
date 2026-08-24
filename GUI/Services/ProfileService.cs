using System.Text.Json;
using GUI.Abstractions;
using GUI.Models;
using Microsoft.Extensions.Logging;

namespace GUI.Services
{
    public sealed class ProfileService : IProfileService
    {
        private const string MainSettingsFileName = "main-settings.json";
        private const string ProfileSettingsFileName = "settings.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private readonly IAppFileSystem _fileSystem;
        private readonly IResourceCatalog _resourceCatalog;
        private readonly ILogger<ProfileService> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private MainSettings? _settings;

        public ProfileService(
            IAppFileSystem fileSystem,
            IResourceCatalog resourceCatalog,
            ILogger<ProfileService> logger)
        {
            _fileSystem = fileSystem;
            _resourceCatalog = resourceCatalog;
            _logger = logger;
        }

        public async Task<MainSettings> GetAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await EnsureLoadedAsync().ConfigureAwait(false);
                return _settings!.Clone();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<string> GetSelectedNameAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await EnsureLoadedAsync().ConfigureAwait(false);
                return _settings!.SelectedProfile;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<bool> SelectAsync(string name)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await EnsureLoadedAsync().ConfigureAwait(false);
                var match = _settings!.Profiles.FirstOrDefault(profile =>
                    string.Equals(profile, name, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    return false;
                }

                if (!string.Equals(_settings.SelectedProfile, match, StringComparison.Ordinal))
                {
                    _settings.SelectedProfile = match;
                    EnsureProfileData(match);
                    SaveMainSettings();
                    _logger.LogInformation("Selected profile {Profile}", match);
                }

                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        private Task EnsureLoadedAsync()
        {
            if (_settings is not null)
            {
                if (SyncProfilesFromResources())
                {
                    SaveMainSettings();
                }

                return Task.CompletedTask;
            }

            var repaired = false;
            if (!_fileSystem.FileExists(MainSettingsFileName))
            {
                _settings = CreateDefault();
                repaired = true;
            }
            else
            {
                try
                {
                    var json = _fileSystem.ReadAllText(MainSettingsFileName);
                    var loaded = JsonSerializer.Deserialize<MainSettings>(json, JsonOptions);
                    if (loaded is null)
                    {
                        _settings = CreateDefault();
                        repaired = true;
                    }
                    else
                    {
                        _settings = loaded;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read {File}; using defaults", MainSettingsFileName);
                    _settings = CreateDefault();
                    repaired = true;
                }
            }

            repaired = SyncProfilesFromResources() || repaired;
            EnsureProfileData(_settings!.SelectedProfile);
            if (repaired)
            {
                SaveMainSettings();
            }

            return Task.CompletedTask;
        }

        private bool SyncProfilesFromResources()
        {
            var discovered = _resourceCatalog.GetProfileNames().ToList();
            var changed = false;

            if (!ProfilesEqual(_settings!.Profiles, discovered))
            {
                _settings.Profiles = discovered;
                changed = true;
            }

            if (discovered.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(_settings.SelectedProfile))
                {
                    _settings.SelectedProfile = ProfileDefaults.DefaultProfileName;
                    changed = true;
                }

                return changed;
            }

            if (!_settings.Profiles.Any(profile =>
                    string.Equals(profile, _settings.SelectedProfile, StringComparison.OrdinalIgnoreCase)))
            {
                var preferred = discovered.FirstOrDefault(profile =>
                    string.Equals(profile, ProfileDefaults.DefaultProfileName, StringComparison.OrdinalIgnoreCase));
                _settings.SelectedProfile = preferred ?? discovered[0];
                changed = true;
            }

            return changed;
        }

        private void EnsureProfileData(string profile)
        {
            if (string.IsNullOrWhiteSpace(profile))
            {
                return;
            }

            var settingsPath = Path.Combine(profile, ProfileSettingsFileName);
            if (_fileSystem.FileExists(settingsPath))
            {
                return;
            }

            _fileSystem.CreateDirectory(profile);
            var json = JsonSerializer.Serialize(DefaultSettings.Create(), JsonOptions);
            _fileSystem.WriteAllText(settingsPath, json);
            _logger.LogInformation("Created settings for profile {Profile}", profile);
        }

        private void SaveMainSettings()
        {
            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            _fileSystem.WriteAllText(MainSettingsFileName, json);
        }

        private static MainSettings CreateDefault()
        {
            return new MainSettings
            {
                Profiles = [],
                SelectedProfile = ProfileDefaults.DefaultProfileName
            };
        }

        private static bool ProfilesEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

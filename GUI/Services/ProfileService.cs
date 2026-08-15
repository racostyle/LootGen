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
        private readonly ILogger<ProfileService> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private MainSettings? _settings;

        public ProfileService(IAppFileSystem fileSystem, ILogger<ProfileService> logger)
        {
            _fileSystem = fileSystem;
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

        public async Task<bool> AddAsync(string name)
        {
            var trimmed = name.Trim();
            if (!IsValidProfileName(trimmed))
            {
                return false;
            }

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await EnsureLoadedAsync().ConfigureAwait(false);
                if (_settings!.Profiles.Any(profile => string.Equals(profile, trimmed, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                _settings.Profiles.Add(trimmed);
                EnsureProfileData(trimmed);
                SaveMainSettings();
                _logger.LogInformation("Added profile {Profile}", trimmed);
                return true;
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
                        repaired = Normalize();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read {File}; using defaults", MainSettingsFileName);
                    _settings = CreateDefault();
                    repaired = true;
                }
            }

            EnsureProfileData(_settings!.SelectedProfile);
            if (repaired)
            {
                SaveMainSettings();
            }

            return Task.CompletedTask;
        }

        private bool Normalize()
        {
            var changed = false;
            _settings!.Profiles ??= [];

            if (_settings.Profiles.Count == 0)
            {
                _settings.Profiles.Add(ProfileDefaults.DefaultProfileName);
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(_settings.SelectedProfile)
                || !_settings.Profiles.Any(profile =>
                    string.Equals(profile, _settings.SelectedProfile, StringComparison.OrdinalIgnoreCase)))
            {
                _settings.SelectedProfile = _settings.Profiles[0];
                changed = true;
            }

            return changed;
        }

        private void EnsureProfileData(string profile)
        {
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
                Profiles = [ProfileDefaults.DefaultProfileName],
                SelectedProfile = ProfileDefaults.DefaultProfileName
            };
        }

        private static bool IsValidProfileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name is "." or "..")
            {
                return false;
            }

            return name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
    }
}

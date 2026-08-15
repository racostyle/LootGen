using System.Text.Json;
using GUI.Abstractions;
using GUI.Models;
using Microsoft.Extensions.Logging;

namespace GUI.Services
{
    public sealed class AppSettingsService : IAppSettingsService
    {
        private const string SettingsFileName = "settings.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private readonly IAppFileSystem _fileSystem;
        private readonly IProfileService _profileService;
        private readonly ILogger<AppSettingsService> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private AppSettings? _settings;
        private string? _loadedProfile;

        public AppSettingsService(
            IAppFileSystem fileSystem,
            IProfileService profileService,
            ILogger<AppSettingsService> logger)
        {
            _fileSystem = fileSystem;
            _profileService = profileService;
            _logger = logger;
        }

        public async Task<AppSettings> GetAsync()
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

        public async Task<bool> AddAsync(SettingsCategory category, string value)
        {
            var trimmed = value.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return false;
            }

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await EnsureLoadedAsync().ConfigureAwait(false);
                var list = GetList(category);
                if (list.Any(item => string.Equals(item, trimmed, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                list.Add(trimmed);
                Save();
                _logger.LogInformation("Added {Value} to {Category} for profile {Profile}", trimmed, category, _loadedProfile);
                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<bool> RemoveAsync(SettingsCategory category, string value)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await EnsureLoadedAsync().ConfigureAwait(false);
                var list = GetList(category);
                var removed = list.RemoveAll(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) > 0;
                if (removed)
                {
                    Save();
                    _logger.LogInformation("Removed {Value} from {Category} for profile {Profile}", value, category, _loadedProfile);
                }

                return removed;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task RestoreDefaultsAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await EnsureLoadedAsync().ConfigureAwait(false);
                _settings = DefaultSettings.Create();
                Save();
                _logger.LogInformation("Restored default settings for profile {Profile}", _loadedProfile);
            }
            finally
            {
                _gate.Release();
            }
        }

        private List<string> GetList(SettingsCategory category)
        {
            return category switch
            {
                SettingsCategory.Type => _settings!.Types,
                SettingsCategory.Rarity => _settings!.Rarities,
                SettingsCategory.Size => _settings!.Sizes,
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown settings category.")
            };
        }

        private async Task EnsureLoadedAsync()
        {
            var profile = await _profileService.GetSelectedNameAsync().ConfigureAwait(false);
            if (_settings is not null && string.Equals(_loadedProfile, profile, StringComparison.Ordinal))
            {
                return;
            }

            _loadedProfile = profile;
            var relativePath = Path.Combine(profile, SettingsFileName);
            if (!_fileSystem.FileExists(relativePath))
            {
                _settings = DefaultSettings.Create();
                Save();
                return;
            }

            try
            {
                var json = _fileSystem.ReadAllText(relativePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                _settings = Normalize(loaded);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read {File}; using defaults", relativePath);
                _settings = DefaultSettings.Create();
                Save();
            }
        }

        private void Save()
        {
            var relativePath = Path.Combine(_loadedProfile!, SettingsFileName);
            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            _fileSystem.WriteAllText(relativePath, json);
        }

        private static AppSettings Normalize(AppSettings? loaded)
        {
            var settings = loaded ?? DefaultSettings.Create();
            settings.Types ??= [];
            settings.Rarities ??= [];
            settings.Sizes ??= [];
            return settings;
        }
    }
}

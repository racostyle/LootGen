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

        private readonly IFileSystem _fileSystem;
        private readonly ILogger<AppSettingsService> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private AppSettings? _settings;

        public AppSettingsService(IFileSystem fileSystem, ILogger<AppSettingsService> logger)
        {
            _fileSystem = fileSystem;
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
                await SaveAsync().ConfigureAwait(false);
                _logger.LogInformation("Added {Value} to {Category}", trimmed, category);
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
                    await SaveAsync().ConfigureAwait(false);
                    _logger.LogInformation("Removed {Value} from {Category}", value, category);
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
                _settings = DefaultSettings.Create();
                await SaveAsync().ConfigureAwait(false);
                _logger.LogInformation("Restored default settings");
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

        private Task EnsureLoadedAsync()
        {
            if (_settings is not null)
            {
                return Task.CompletedTask;
            }

            if (!_fileSystem.FileExists(SettingsFileName))
            {
                _settings = DefaultSettings.Create();
                return SaveAsync();
            }

            try
            {
                var json = _fileSystem.ReadAllText(SettingsFileName);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                _settings = Normalize(loaded);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read {File}; using defaults", SettingsFileName);
                _settings = DefaultSettings.Create();
                return SaveAsync();
            }
        }

        private Task SaveAsync()
        {
            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            _fileSystem.WriteAllText(SettingsFileName, json);
            return Task.CompletedTask;
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

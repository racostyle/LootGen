using GUI.Models;

namespace GUI.Services
{
    public interface IAppSettingsService
    {
        Task<AppSettings> GetAsync();

        Task<bool> AddAsync(SettingsCategory category, string value);

        Task<bool> RemoveAsync(SettingsCategory category, string value);

        Task RestoreDefaultsAsync();
    }
}

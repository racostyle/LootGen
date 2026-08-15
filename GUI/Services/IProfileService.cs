using GUI.Models;

namespace GUI.Services
{
    public interface IProfileService
    {
        Task<MainSettings> GetAsync();

        Task<string> GetSelectedNameAsync();

        Task<bool> AddAsync(string name);

        Task<bool> SelectAsync(string name);
    }
}

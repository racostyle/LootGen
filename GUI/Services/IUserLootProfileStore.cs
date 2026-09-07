using GUI.Models;

namespace GUI.Services
{
    public interface IUserLootProfileStore
    {
        IReadOnlyList<string> GetProfileNames();

        bool Exists(string profileName);

        string GetAbsoluteProfilePath(string profileName);

        string GetRelativeProfilePath(string profileName);

        void Replace(string profileName, IReadOnlyList<STableFile> files);

        void Rename(string fromName, string toName);

        void Delete(string profileName);
    }
}

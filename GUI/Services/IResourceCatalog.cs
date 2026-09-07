using GUI.Models;

namespace GUI.Services
{
    public interface IResourceCatalog
    {
        int Revision { get; }

        IReadOnlyList<string> GetProfileNames();

        bool IsBuiltInProfile(string profileName);

        string GetProfilePath(string profileName);

        bool IsUserProfile(string profileName);

        IReadOnlyList<STableFile> GetTableFiles(string profileName);

        void NotifyUserDataChanged();
    }
}

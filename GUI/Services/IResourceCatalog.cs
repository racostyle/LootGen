namespace GUI.Services
{
    public interface IResourceCatalog
    {
        IReadOnlyList<string> GetProfileNames();

        string GetProfilePath(string profileName);
    }
}

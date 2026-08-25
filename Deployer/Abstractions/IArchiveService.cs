namespace Deployer.Abstractions
{
    public interface IArchiveService
    {
        void CreateZipFromDirectory(string sourceDirectory, string zipPath, string entryPrefix);
    }
}

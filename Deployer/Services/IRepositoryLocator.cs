namespace Deployer.Services
{
    public interface IRepositoryLocator
    {
        bool TryFindRoot(string startPath, out string root);
    }
}

using Deployer.Models;

namespace Deployer.Services
{
    public interface IDeployerSettingsService
    {
        DeployerSettings Load();

        void Save(DeployerSettings settings);

        string GetDefaultKeystorePath();

        string GetDefaultOutputDirectory(string repositoryRoot);
    }
}

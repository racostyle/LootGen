using Deployer.Models;

namespace Deployer.Services
{
    public interface IPublishCommandFactory
    {
        SProcessStart CreateWindowsPublish(string guiProjectPath, string workingDirectory, string displayVersion, int applicationVersion);

        SProcessStart CreateAndroidPublish(
            string guiProjectPath,
            string workingDirectory,
            string displayVersion,
            int applicationVersion,
            string keystorePath,
            string alias);
    }
}

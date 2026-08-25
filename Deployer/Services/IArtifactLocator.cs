using Deployer.Models;

namespace Deployer.Services
{
    public interface IArtifactLocator
    {
        string? FindWindowsPublishFolder(string guiProjectDirectory);

        string? FindSignedApk(string guiProjectDirectory);
    }
}

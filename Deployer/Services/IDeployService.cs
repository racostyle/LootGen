using Deployer.Models;

namespace Deployer.Services
{
    public interface IDeployService
    {
        Task<SDeployResult> DeployAsync(SDeployRequest request, CancellationToken cancellationToken);
    }
}

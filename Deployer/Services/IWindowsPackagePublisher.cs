using Deployer.Models;

namespace Deployer.Services
{
    public interface IWindowsPackagePublisher
    {
        Task<SPublishResult> PublishAsync(SDeployRequest request, CancellationToken cancellationToken);
    }
}

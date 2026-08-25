using Deployer.Models;

namespace Deployer.Services
{
    public interface IAndroidApkPublisher
    {
        Task<SPublishResult> PublishAsync(SDeployRequest request, CancellationToken cancellationToken);
    }
}

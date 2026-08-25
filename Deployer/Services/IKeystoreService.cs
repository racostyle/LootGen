using Deployer.Models;

namespace Deployer.Services
{
    public interface IKeystoreService
    {
        Task CreateAsync(SKeystoreCreateRequest request, CancellationToken cancellationToken);
    }
}

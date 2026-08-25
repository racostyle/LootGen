using Deployer.Models;

namespace Deployer.Abstractions
{
    public interface IProcessRunner
    {
        Task<SProcessResult> RunAsync(
            SProcessStart start,
            Action<string>? onOutput,
            CancellationToken cancellationToken);
    }
}

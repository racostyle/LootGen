using Deployer.Abstractions;

namespace Deployer.Infrastructure
{
    public sealed class SystemEnvironmentInfo : IEnvironmentInfo
    {
        public string? GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        public string GetFolderPath(Environment.SpecialFolder folder)
        {
            return Environment.GetFolderPath(folder);
        }

        public string ApplicationBaseDirectory => AppContext.BaseDirectory;

        public string CurrentDirectory => Directory.GetCurrentDirectory();
    }
}

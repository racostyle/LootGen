namespace Deployer.Abstractions
{
    public interface IEnvironmentInfo
    {
        string? GetEnvironmentVariable(string name);

        string GetFolderPath(Environment.SpecialFolder folder);

        string ApplicationBaseDirectory { get; }

        string CurrentDirectory { get; }
    }
}

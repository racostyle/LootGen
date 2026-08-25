namespace Deployer.Models
{
    public readonly struct SDeployRequest
    {
        public required DeployTarget Target { get; init; }

        public required string DisplayVersion { get; init; }

        public required int ApplicationVersion { get; init; }

        public required string RepositoryRoot { get; init; }

        public required string OutputDirectory { get; init; }

        public string? KeystorePath { get; init; }

        public string? KeystoreAlias { get; init; }

        public string? KeystorePassword { get; init; }

        public string? KeyPassword { get; init; }
    }
}

namespace Deployer.Models
{
    public readonly struct SDeployResult
    {
        public required bool Succeeded { get; init; }

        public required string Message { get; init; }

        public required IReadOnlyList<string> ArtifactPaths { get; init; }
    }
}

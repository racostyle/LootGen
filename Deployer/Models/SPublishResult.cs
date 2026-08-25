namespace Deployer.Models
{
    public readonly struct SPublishResult
    {
        public required bool Succeeded { get; init; }

        public required string Message { get; init; }

        public required IReadOnlyList<string> ArtifactPaths { get; init; }
    }
}

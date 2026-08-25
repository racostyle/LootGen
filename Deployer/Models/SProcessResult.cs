namespace Deployer.Models
{
    public readonly struct SProcessResult
    {
        public required int ExitCode { get; init; }

        public required IReadOnlyList<string> OutputLines { get; init; }

        public bool Succeeded => ExitCode == 0;
    }
}

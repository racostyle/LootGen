namespace Deployer.Models
{
    public readonly struct SProcessStart
    {
        public required string FileName { get; init; }

        public required IReadOnlyList<string> Arguments { get; init; }

        public required string WorkingDirectory { get; init; }

        public IReadOnlyDictionary<string, string>? EnvironmentVariables { get; init; }
    }
}

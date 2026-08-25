namespace Deployer.Models
{
    public readonly struct SKeystoreCreateRequest
    {
        public required string KeystorePath { get; init; }

        public required string Alias { get; init; }

        public required string Password { get; init; }

        public required string DistinguishedName { get; init; }
    }
}

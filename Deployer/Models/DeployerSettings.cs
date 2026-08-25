namespace Deployer.Models
{
    public sealed class DeployerSettings
    {
        public string DisplayVersion { get; set; } = "1.0.0";

        public int ApplicationVersion { get; set; } = 1;

        public string RepositoryRoot { get; set; } = string.Empty;

        public string OutputDirectory { get; set; } = string.Empty;

        public string KeystorePath { get; set; } = string.Empty;

        public string KeystoreAlias { get; set; } = PublishConstants.DefaultKeystoreAlias;

        public DeployTarget LastTarget { get; set; } = DeployTarget.WindowsPackage;
    }
}

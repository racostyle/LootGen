namespace Deployer.Models
{
    public static class PublishConstants
    {
        public const string AppTitle = "LootGen";
        public const string ApplicationId = "com.lootgen.app";
        public const string GuiProjectRelativePath = "GUI/GUI.csproj";
        public const string SolutionFileName = "LootGen.sln";
        public const string WindowsTargetFramework = "net9.0-windows10.0.19041.0";
        public const string AndroidTargetFramework = "net9.0-android";
        public const string WindowsRuntimeIdentifier = "win10-x64";
        public const string DefaultKeystoreAlias = "lootgen";
        public const string SettingsFileName = "settings.json";
        public const string SettingsFolderName = "deployer";
        public const string AppFolderName = "LootGen";
        public const string DefaultKeystoreFileName = "lootgen.keystore";
        public const string DistFolderName = "dist";
        public const string WindowsDistFolderName = "Windows";
        public const string AndroidDistFolderName = "Android";
        public const string AndroidSigningPasswordEnvVar = "LOOTGEN_ANDROID_SIGNING_PASSWORD";
    }
}

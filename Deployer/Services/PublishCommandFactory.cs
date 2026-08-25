using Deployer.Models;

namespace Deployer.Services
{
    public sealed class PublishCommandFactory : IPublishCommandFactory
    {
        public SProcessStart CreateWindowsPublish(
            string guiProjectPath,
            string workingDirectory,
            string displayVersion,
            int applicationVersion)
        {
            return new SProcessStart
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                Arguments =
                [
                    "publish",
                    guiProjectPath,
                    "-f",
                    PublishConstants.WindowsTargetFramework,
                    "-c",
                    "Release",
                    "-p:RuntimeIdentifierOverride=" + PublishConstants.WindowsRuntimeIdentifier,
                    "-p:WindowsPackageType=None",
                    "-p:WindowsAppSDKSelfContained=true",
                    "--self-contained",
                    "true",
                    "-p:ApplicationTitle=" + PublishConstants.AppTitle,
                    "-p:ApplicationId=" + PublishConstants.ApplicationId,
                    "-p:ApplicationDisplayVersion=" + displayVersion,
                    "-p:ApplicationVersion=" + applicationVersion,
                    "-p:AssemblyName=" + PublishConstants.AppTitle
                ]
            };
        }

        public SProcessStart CreateAndroidPublish(
            string guiProjectPath,
            string workingDirectory,
            string displayVersion,
            int applicationVersion,
            string keystorePath,
            string alias)
        {
            return new SProcessStart
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                Arguments =
                [
                    "publish",
                    guiProjectPath,
                    "-f",
                    PublishConstants.AndroidTargetFramework,
                    "-c",
                    "Release",
                    "-p:AndroidPackageFormats=apk",
                    "-p:AndroidKeyStore=true",
                    "-p:AndroidSigningKeyStore=" + keystorePath,
                    "-p:AndroidSigningKeyAlias=" + alias,
                    "-p:AndroidSigningKeyPass=env:" + PublishConstants.AndroidSigningPasswordEnvVar,
                    "-p:AndroidSigningStorePass=env:" + PublishConstants.AndroidSigningPasswordEnvVar,
                    "-p:ApplicationTitle=" + PublishConstants.AppTitle,
                    "-p:ApplicationId=" + PublishConstants.ApplicationId,
                    "-p:ApplicationDisplayVersion=" + displayVersion,
                    "-p:ApplicationVersion=" + applicationVersion
                ]
            };
        }
    }
}

using Deployer.Abstractions;
using Deployer.Infrastructure;
using Deployer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Deployer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();

            var services = new ServiceCollection();
            ConfigureServices(services);

            using var provider = services.BuildServiceProvider();
            Application.Run(provider.GetRequiredService<MainForm>());
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<UiLogSink>();
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddDebug();
                builder.Services.AddSingleton<ILoggerProvider, ActionLoggerProvider>();
            });

            services.AddSingleton<IFileSystem, PhysicalFileSystem>();
            services.AddSingleton<IProcessRunner, ProcessRunner>();
            services.AddSingleton<IArchiveService, ZipArchiveService>();
            services.AddSingleton<IEnvironmentInfo, SystemEnvironmentInfo>();
            services.AddSingleton<IJdkLocator, JdkLocator>();
            services.AddSingleton<IRepositoryLocator, RepositoryLocator>();
            services.AddSingleton<IPublishCommandFactory, PublishCommandFactory>();
            services.AddSingleton<IArtifactLocator, ArtifactLocator>();
            services.AddSingleton<IWindowsPackagePublisher, WindowsPackagePublisher>();
            services.AddSingleton<IAndroidApkPublisher, AndroidApkPublisher>();
            services.AddSingleton<IKeystoreService, KeystoreService>();
            services.AddSingleton<IDeployerSettingsService, DeployerSettingsService>();
            services.AddSingleton<IDeployService, DeployService>();
            services.AddTransient<MainForm>();
        }
    }
}

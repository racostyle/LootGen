using GUI.Abstractions;
using GUI.Infrastructure;
using GUI.Pages;
using GUI.Services;
using Microsoft.Extensions.Logging;

namespace GUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<IAppFileSystem, MauiFileSystem>();
#if ANDROID
            builder.Services.AddSingleton<IAppPackage, AndroidAppPackage>();
#else
            builder.Services.AddSingleton<IAppPackage, FileAppPackage>();
#endif
            builder.Services.AddSingleton<IPackagedLootDataSource, PackagedLootDataSource>();
            builder.Services.AddSingleton<IResourceCatalog, ResourceCatalog>();
            builder.Services.AddSingleton<IProfileService, ProfileService>();
            builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();
            builder.Services.AddSingleton<IGenerateResultStore, GenerateResultStore>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<ProfilesPage>();
            builder.Services.AddTransient<StorePage>();
            builder.Services.AddTransient<ItemListPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<ResultsPage>();
            builder.Services.AddSingleton<AppShell>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

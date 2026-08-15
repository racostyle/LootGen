using GUI.Pages;

namespace GUI
{
    public partial class AppShell : Shell
    {
        public AppShell(MainPage mainPage)
        {
            InitializeComponent();

            Items.Add(new ShellContent
            {
                Title = "Home",
                Route = nameof(MainPage),
                Content = mainPage
            });

            Routing.RegisterRoute(nameof(ProfilesPage), typeof(ProfilesPage));
            Routing.RegisterRoute(nameof(GeneratePage), typeof(GeneratePage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        }
    }
}

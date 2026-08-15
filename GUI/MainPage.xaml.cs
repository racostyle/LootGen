using GUI.Pages;
using GUI.Services;
using Microsoft.Extensions.Logging;

namespace GUI
{
    public partial class MainPage : ContentPage
    {
        private readonly IProfileService _profileService;
        private readonly ILogger<MainPage> _logger;

        public MainPage(IProfileService profileService, ILogger<MainPage> logger)
        {
            InitializeComponent();
            _profileService = profileService;
            _logger = logger;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RefreshProfileAsync();
        }

        private async void OnProfileClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ProfilesPage));
        }

        private async void OnGenerateClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("GeneratePage");
        }

        private async void OnSettingsClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("SettingsPage");
        }

        private async Task RefreshProfileAsync()
        {
            try
            {
                var profile = await _profileService.GetSelectedNameAsync();
                ProfileButton.Text = $"Profile: {profile}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load selected profile");
                ProfileButton.Text = "Profile";
            }
        }
    }
}

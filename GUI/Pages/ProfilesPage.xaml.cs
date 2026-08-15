using GUI.Services;
using Microsoft.Extensions.Logging;

namespace GUI.Pages
{
    public partial class ProfilesPage : ContentPage
    {
        private readonly IProfileService _profileService;
        private readonly ILogger<ProfilesPage> _logger;
        private string _selectedProfile = string.Empty;

        public ProfilesPage(IProfileService profileService, ILogger<ProfilesPage> logger)
        {
            InitializeComponent();
            _profileService = profileService;
            _logger = logger;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ReloadAsync();
        }

        private async void OnAddClicked(object? sender, EventArgs e)
        {
            var name = NewProfileEntry.Text ?? string.Empty;
            var added = await _profileService.AddAsync(name);
            if (!added)
            {
                await DisplayAlert("Profile", "Enter a unique profile name that is a valid folder name.", "OK");
                return;
            }

            NewProfileEntry.Text = string.Empty;
            await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            try
            {
                var settings = await _profileService.GetAsync();
                _selectedProfile = settings.SelectedProfile;
                ProfilesLayout.Children.Clear();

                foreach (var profile in settings.Profiles)
                {
                    var isSelected = string.Equals(profile, _selectedProfile, StringComparison.Ordinal);
                    var button = new Button
                    {
                        Text = isSelected ? $"{profile} (selected)" : profile,
                        HorizontalOptions = LayoutOptions.Fill
                    };
                    ApplySelectionVisual(button, isSelected);
                    var captured = profile;
                    button.Clicked += async (_, _) => await SelectProfileAsync(captured);
                    ProfilesLayout.Children.Add(button);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profiles");
            }
        }

        private async Task SelectProfileAsync(string profile)
        {
            var selected = await _profileService.SelectAsync(profile);
            if (!selected)
            {
                await DisplayAlert("Profile", "Could not select that profile.", "OK");
                return;
            }

            await ReloadAsync();
        }

        private static void ApplySelectionVisual(Button button, bool isSelected)
        {
            var resources = Application.Current?.Resources;
            if (resources is null)
            {
                return;
            }

            button.BackgroundColor = isSelected
                ? (Color)resources["Primary"]
                : (Color)resources["Gray500"];
        }
    }
}

using GUI.Services;
using Microsoft.Extensions.Logging;

namespace GUI.Pages
{
    public partial class ProfilesPage : ContentPage
    {
        private readonly IProfileService _profileService;
        private readonly IResourceCatalog _resourceCatalog;
        private readonly ILogger<ProfilesPage> _logger;
        private string _selectedProfile = string.Empty;

        public ProfilesPage(
            IProfileService profileService,
            IResourceCatalog resourceCatalog,
            ILogger<ProfilesPage> logger)
        {
            InitializeComponent();
            _profileService = profileService;
            _resourceCatalog = resourceCatalog;
            _logger = logger;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            try
            {
                var settings = await _profileService.GetAsync();
                _selectedProfile = settings.SelectedProfile;
                ProfilesLayout.Children.Clear();

                if (settings.Profiles.Count == 0)
                {
                    ProfilesLayout.Children.Add(new Label
                    {
                        Text = "No resource profiles found."
                    });
                    return;
                }

                foreach (var profile in settings.Profiles)
                {
                    var isSelected = string.Equals(profile, _selectedProfile, StringComparison.Ordinal);
                    var tags = new List<string>();
                    if (_resourceCatalog.IsUserProfile(profile))
                    {
                        tags.Add("imported");
                    }

                    if (isSelected)
                    {
                        tags.Add("selected");
                    }

                    var button = new Button
                    {
                        Text = tags.Count == 0 ? profile : $"{profile} ({string.Join(", ", tags)})",
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

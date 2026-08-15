using GUI.Models;
using GUI.Services;
using Microsoft.Extensions.Logging;

namespace GUI.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private readonly IAppSettingsService _settingsService;
        private readonly ILogger<SettingsPage> _logger;

        public SettingsPage(IAppSettingsService settingsService, ILogger<SettingsPage> logger)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _logger = logger;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ReloadAsync();
        }

        private async void OnAddTypeClicked(object? sender, EventArgs e)
        {
            await AddAsync(SettingsCategory.Type, TypeEntry);
        }

        private async void OnAddRarityClicked(object? sender, EventArgs e)
        {
            await AddAsync(SettingsCategory.Rarity, RarityEntry);
        }

        private async void OnAddSizeClicked(object? sender, EventArgs e)
        {
            await AddAsync(SettingsCategory.Size, SizeEntry);
        }

        private async void OnRestoreDefaultsClicked(object? sender, EventArgs e)
        {
            for (var step = 1; step <= 3; step++)
            {
                var confirmed = await DisplayAlert(
                    "Restore defaults",
                    $"This will replace Type, Rarity, and Size for the current profile with the built-in defaults. Confirm {step} of 3.",
                    "Yes",
                    "No");
                if (!confirmed)
                {
                    return;
                }
            }

            await _settingsService.RestoreDefaultsAsync();
            await ReloadAsync();
        }

        private async Task AddAsync(SettingsCategory category, Entry entry)
        {
            var added = await _settingsService.AddAsync(category, entry.Text ?? string.Empty);
            if (!added)
            {
                return;
            }

            entry.Text = string.Empty;
            await ReloadAsync();
        }

        private async Task RemoveAsync(SettingsCategory category, string value)
        {
            await _settingsService.RemoveAsync(category, value);
            await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            try
            {
                var settings = await _settingsService.GetAsync();
                BindList(TypeItemsLayout, SettingsCategory.Type, settings.Types);
                BindList(RarityItemsLayout, SettingsCategory.Rarity, settings.Rarities);
                BindList(SizeItemsLayout, SettingsCategory.Size, settings.Sizes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
            }
        }

        private void BindList(VerticalStackLayout layout, SettingsCategory category, IReadOnlyList<string> items)
        {
            layout.Children.Clear();
            foreach (var item in items)
            {
                var value = item;
                var removeButton = new Button
                {
                    Text = "Remove",
                    HorizontalOptions = LayoutOptions.End
                };
                removeButton.Clicked += async (_, _) => await RemoveAsync(category, value);

                var row = new Grid
                {
                    ColumnDefinitions =
                    [
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    ],
                    ColumnSpacing = 8
                };
                var label = new Label
                {
                    Text = value,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(label, 0);
                Grid.SetColumn(removeButton, 1);
                row.Children.Add(label);
                row.Children.Add(removeButton);
                layout.Children.Add(row);
            }
        }
    }
}

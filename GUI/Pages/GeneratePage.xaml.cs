using GUI.Models;
using GUI.Services;
using Microsoft.Extensions.Logging;

namespace GUI.Pages
{
    public partial class GeneratePage : ContentPage
    {
        private const string MerchantSource = "Merchant";
        private const string LocationSource = "Location";

        private readonly IAppSettingsService _settingsService;
        private readonly ILogger<GeneratePage> _logger;
        private readonly HashSet<string> _selectedTypes = new(StringComparer.Ordinal);
        private string _source = LocationSource;

        public GeneratePage(IAppSettingsService settingsService, ILogger<GeneratePage> logger)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _logger = logger;
            ApplySourceVisuals();
            UpdateGenerateEnabled();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadFromSettingsAsync();
        }

        private void OnSourceClicked(object? sender, EventArgs e)
        {
            if (sender is not Button clicked)
            {
                return;
            }

            var clickedSource = clicked == MerchantButton ? MerchantSource : LocationSource;
            _source = string.Equals(clickedSource, _source, StringComparison.Ordinal)
                ? OppositeSource(clickedSource)
                : clickedSource;
            ApplySourceVisuals();
        }

        private void OnGenerateClicked(object? sender, EventArgs e)
        {
            var request = new SGenerateRequest
            {
                Source = _source,
                Types = TypeButtonsLayout.Children
                    .OfType<Button>()
                    .Select(button => button.Text)
                    .Where(type => _selectedTypes.Contains(type))
                    .ToList(),
                Size = SizePicker.SelectedItem as string ?? string.Empty,
                Rarity = RarityPicker.SelectedItem as string ?? string.Empty
            };

            _logger.LogInformation(
                "Generate requested. Source={Source}, Types={Types}, Size={Size}, Rarity={Rarity}",
                request.Source,
                string.Join(", ", request.Types),
                request.Size,
                request.Rarity);
        }

        private async Task LoadFromSettingsAsync()
        {
            try
            {
                var settings = await _settingsService.GetAsync();
                RebuildTypeButtons(settings.Types);
                BindPicker(SizePicker, settings.Sizes);
                BindPicker(RarityPicker, settings.Rarities);
                UpdateGenerateEnabled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load generate options");
            }
        }

        private void RebuildTypeButtons(IReadOnlyList<string> types)
        {
            var available = new HashSet<string>(types, StringComparer.Ordinal);
            _selectedTypes.RemoveWhere(type => !available.Contains(type));

            TypeButtonsLayout.Children.Clear();
            foreach (var type in types)
            {
                var isSelected = _selectedTypes.Contains(type);
                var button = new Button
                {
                    Text = type,
                    Margin = new Thickness(0, 0, 8, 8)
                };
                ApplyToggleVisual(button, isSelected);
                var captured = type;
                button.Clicked += (_, _) => ToggleType(captured, button);
                TypeButtonsLayout.Children.Add(button);
            }
        }

        private void ToggleType(string type, Button button)
        {
            if (!_selectedTypes.Add(type))
            {
                _selectedTypes.Remove(type);
            }

            ApplyToggleVisual(button, _selectedTypes.Contains(type));
            UpdateGenerateEnabled();
        }

        private void BindPicker(Picker picker, IReadOnlyList<string> items)
        {
            var previous = picker.SelectedItem as string;
            var list = items.ToList();
            picker.ItemsSource = list;
            if (list.Count == 0)
            {
                picker.SelectedIndex = -1;
                return;
            }

            var index = previous is null
                ? 0
                : list.FindIndex(item => string.Equals(item, previous, StringComparison.Ordinal));
            picker.SelectedIndex = index >= 0 ? index : 0;
        }

        private void UpdateGenerateEnabled()
        {
            GenerateButton.IsEnabled = _selectedTypes.Count > 0;
        }

        private void ApplySourceVisuals()
        {
            ApplyToggleVisual(MerchantButton, string.Equals(_source, MerchantSource, StringComparison.Ordinal));
            ApplyToggleVisual(LocationButton, string.Equals(_source, LocationSource, StringComparison.Ordinal));
        }

        private static void ApplyToggleVisual(Button button, bool isSelected)
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

        private static string OppositeSource(string source)
        {
            return string.Equals(source, MerchantSource, StringComparison.Ordinal)
                ? LocationSource
                : MerchantSource;
        }
    }
}

using GUI.Infrastructure;
using GUI.Models;
using GUI.Services;
using Microsoft.Extensions.Logging;
using TableLib;

namespace GUI.Pages
{
    public partial class ItemListPage : ContentPage
    {
        private readonly IProfileService _profileService;
        private readonly IResourceCatalog _resourceCatalog;
        private readonly IGenerateResultStore _resultStore;
        private readonly ILogger<ItemListPage> _logger;
        private readonly HashSet<string> _selectedTypes = new(StringComparer.Ordinal);
        private IReadOnlyList<IDataBatch> _batches = [];

        private string _loadedProfile = string.Empty;

        public ItemListPage(
            IProfileService profileService,
            IResourceCatalog resourceCatalog,
            IGenerateResultStore resultStore,
            ILogger<ItemListPage> logger)
        {
            InitializeComponent();
            _profileService = profileService;
            _resourceCatalog = resourceCatalog;
            _resultStore = resultStore;
            _logger = logger;
            BindPicker(RarityPicker, LootScale.Rarities);
            UpdateListEnabled();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadProfileDataAsync();
        }

        private async void OnListItemsClicked(object? sender, EventArgs e)
        {
            if (RarityPicker.SelectedIndex < 0 || _selectedTypes.Count == 0)
            {
                return;
            }

            var rarity = LootScale.Rarities[RarityPicker.SelectedIndex];
            var categories = TypeButtonsLayout.Children
                .OfType<Button>()
                .Select(button => button.Text)
                .Where(type => _selectedTypes.Contains(type))
                .ToArray();

            _logger.LogInformation(
                "Item list requested. Types={Types}, Rarity={Rarity}",
                string.Join(", ", categories),
                rarity.ToString());

            var generator = new TableGenerator(_batches);
            _resultStore.Title = "Item List";
            _resultStore.Items = generator.ListAll(rarity.Value, categories);

            await Shell.Current.GoToAsync(nameof(ResultsPage));
        }

        private async Task LoadProfileDataAsync()
        {
            try
            {
                var profile = await _profileService.GetSelectedNameAsync();
                if (string.Equals(profile, _loadedProfile, StringComparison.Ordinal)
                    && _batches.Count > 0)
                {
                    return;
                }

                var path = _resourceCatalog.GetProfilePath(profile);
                var loader = new FileLoader(path);
                _batches = loader.Batches;
                _loadedProfile = profile;

                var types = _batches
                    .Select(batch => batch.Category)
                    .Where(category => !string.IsNullOrWhiteSpace(category))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                RebuildTypeButtons(types);
                UpdateListEnabled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load item list options");
                _batches = [];
                RebuildTypeButtons([]);
                UpdateListEnabled();
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
                ToggleButtonVisuals.Apply(button, isSelected);
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

            ToggleButtonVisuals.Apply(button, _selectedTypes.Contains(type));
            UpdateListEnabled();
        }

        private static void BindPicker(Picker picker, IReadOnlyList<SLabeledValue> items)
        {
            picker.ItemsSource = items.ToList();
            picker.SelectedIndex = items.Count == 0 ? -1 : 0;
        }

        private void UpdateListEnabled()
        {
            ListItemsButton.IsEnabled = _selectedTypes.Count > 0 && _batches.Count > 0;
        }
    }
}

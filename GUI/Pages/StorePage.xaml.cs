using GUI.Infrastructure;
using GUI.Models;
using GUI.Services;
using Microsoft.Extensions.Logging;
using TableLib;

namespace GUI.Pages
{
    public partial class StorePage : ContentPage
    {
        private enum SelectionMode { Unique, Repeatable }
        private SelectionMode _selectionMode = SelectionMode.Repeatable;

        private readonly IProfileService _profileService;
        private readonly IResourceCatalog _resourceCatalog;
        private readonly IGenerateResultStore _resultStore;
        private readonly ILogger<StorePage> _logger;
        private readonly HashSet<string> _selectedTypes = new(StringComparer.Ordinal);
        private IReadOnlyList<IDataBatch> _batches = [];

        private string _loadedProfile = string.Empty;
        private int _loadedRevision = -1;

        public StorePage(
            IProfileService profileService,
            IResourceCatalog resourceCatalog,
            IGenerateResultStore resultStore,
            ILogger<StorePage> logger)
        {
            InitializeComponent();
            _profileService = profileService;
            _resourceCatalog = resourceCatalog;
            _resultStore = resultStore;
            _logger = logger;
            BindPicker(SizePicker, LootScale.Sizes);
            BindPicker(RarityPicker, LootScale.Rarities, LootScale.DefaultRarityValue);
            ApplySourceVisuals();
            UpdateGenerateEnabled();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadProfileDataAsync();
        }

        private void OnSourceClicked(object? sender, EventArgs e)
        {
            if (sender is not Button clicked)
            {
                return;
            }

            var clickedSource = clicked == UniqueButton ? SelectionMode.Unique : SelectionMode.Repeatable;
            _selectionMode = _selectionMode == clickedSource
                ? OppositeSource(clickedSource)
                : clickedSource;

            ApplySourceVisuals();
        }

        private async void OnGenerateClicked(object? sender, EventArgs e)
        {
            if (RarityPicker.SelectedIndex < 0
                || SizePicker.SelectedIndex < 0
                || _selectedTypes.Count == 0)
            {
                return;
            }

            var rarity = LootScale.Rarities[RarityPicker.SelectedIndex];
            var size = LootScale.Sizes[SizePicker.SelectedIndex];

            var categories = TypeButtonsLayout.Children
                .OfType<Button>()
                .Select(button => button.Text)
                .Where(type => _selectedTypes.Contains(type))
                .ToArray();

            bool isUniqueSelection = _selectionMode == SelectionMode.Unique;

            _logger.LogInformation(
                    "Generate requested. Source={Source}, Types={Types}, Size={Size}, Rarity={Rarity}",
                    isUniqueSelection,
                    string.Join(", ", categories),
                    size.ToString(),
                    rarity.ToString());

            var generator = new TableGenerator(_batches);
            _resultStore.Title = "Results";
            _resultStore.Items = generator.Fetch(rarity.Value, size.Value, isUniqueSelection, categories);

            await Shell.Current.GoToAsync(nameof(ResultsPage));
        }

        private async Task LoadProfileDataAsync()
        {
            try
            {
                var profile = await _profileService.GetSelectedNameAsync();
                if (string.Equals(profile, _loadedProfile, StringComparison.Ordinal)
                    && _batches.Count > 0
                    && _loadedRevision == _resourceCatalog.Revision)
                {
                    return;
                }

                var path = _resourceCatalog.GetProfilePath(profile);
                var loader = new FileLoader(path);
                _batches = loader.Batches;
                _loadedProfile = profile;
                _loadedRevision = _resourceCatalog.Revision;

                var types = _batches
                    .Select(batch => batch.Category)
                    .Where(category => !string.IsNullOrWhiteSpace(category))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                RebuildTypeButtons(types);
                UpdateGenerateEnabled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load generate options");
                _batches = [];
                RebuildTypeButtons([]);
                UpdateGenerateEnabled();
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
            UpdateGenerateEnabled();
        }

        private static void BindPicker(Picker picker, IReadOnlyList<SLabeledValue> items, int? defaultValue = null)
        {
            picker.ItemsSource = items.ToList();
            picker.SelectedIndex = LootScale.IndexOfOrFirst(items, defaultValue);
        }

        private void UpdateGenerateEnabled()
        {
            GenerateButton.IsEnabled = _selectedTypes.Count > 0 && _batches.Count > 0;
        }

        private void ApplySourceVisuals()
        {
            ToggleButtonVisuals.Apply(UniqueButton, _selectionMode == SelectionMode.Unique);
            ToggleButtonVisuals.Apply(RepeatableButton, _selectionMode == SelectionMode.Repeatable);
        }

        private static SelectionMode OppositeSource(SelectionMode source)
        {
            return source == SelectionMode.Unique
                ? SelectionMode.Repeatable
                : SelectionMode.Unique;
        }
    }
}

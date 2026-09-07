using GUI.Models;
using GUI.Services;
using Microsoft.Extensions.Logging;

namespace GUI.Pages
{
    public partial class ImportExportPage : ContentPage
    {
        private readonly ITableTransferService _transferService;
        private readonly IProfileService _profileService;
        private readonly IResourceCatalog _resourceCatalog;
        private readonly ILogger<ImportExportPage> _logger;
        private string _selectedProfile = string.Empty;
        private bool _busy;

        public ImportExportPage(
            ITableTransferService transferService,
            IProfileService profileService,
            IResourceCatalog resourceCatalog,
            ILogger<ImportExportPage> logger)
        {
            InitializeComponent();
            _transferService = transferService;
            _profileService = profileService;
            _resourceCatalog = resourceCatalog;
            _logger = logger;
            ApplyPlatformHelp();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RefreshAsync();
        }

        private async void OnExportClicked(object? sender, EventArgs e)
        {
            if (_busy)
            {
                return;
            }

            try
            {
                SetBusy(true);
                var result = await _transferService.ExportSelectedProfileAsync();
                if (result.Cancelled)
                {
                    await DisplayAlert("Export", "Export cancelled. No file was saved.", "OK");
                    return;
                }

                await DisplayAlert("Export", result.Message, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export failed");
                await DisplayAlert("Export", string.IsNullOrWhiteSpace(ex.Message)
                    ? "Could not export the current profile."
                    : $"Could not export the current profile. {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void OnImportArchiveClicked(object? sender, EventArgs e)
        {
            await ImportAsync(() => _transferService.PrepareArchiveImportAsync());
        }

        private async void OnImportFolderClicked(object? sender, EventArgs e)
        {
            await ImportAsync(() => _transferService.PrepareFolderImportAsync());
        }

        private async void OnRemoveClicked(object? sender, EventArgs e)
        {
            if (_busy || string.IsNullOrEmpty(_selectedProfile))
            {
                return;
            }

            var confirmed = await DisplayAlert(
                "Remove import",
                $"Remove imported tables for {_selectedProfile}? Built-in profiles are not changed.",
                "Remove",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            try
            {
                SetBusy(true);
                var result = await _transferService.RemoveImportedProfileAsync(_selectedProfile);
                await DisplayAlert("Remove import", result.Message, "OK");
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove imported profile {Profile}", _selectedProfile);
                await DisplayAlert("Remove import", "Could not remove the imported profile.", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task ImportAsync(Func<Task<SPreparedImport?>> prepare)
        {
            if (_busy)
            {
                return;
            }

            try
            {
                SetBusy(true);
                var prepared = await prepare();
                if (prepared is null)
                {
                    await DisplayAlert("Import", "Import cancelled. No file was selected.", "OK");
                    return;
                }

                var import = prepared.Value;
                if (!await ConfirmImportAsync(import))
                {
                    return;
                }

                var result = await _transferService.CommitImportAsync(import);
                await DisplayAlert("Import", result.Message, "OK");
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Import failed");
                await DisplayAlert("Import", string.IsNullOrWhiteSpace(ex.Message)
                    ? "Could not import those tables."
                    : ex.Message, "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task<bool> ConfirmImportAsync(SPreparedImport prepared)
        {
            return prepared.Collision switch
            {
                ImportCollision.ReplacesImported => await DisplayAlert(
                    "Replace import",
                    $"Imported tables for {prepared.ProfileName} already exist ({prepared.Files.Count} files in this import). Replace them?",
                    "Replace",
                    "Cancel"),
                ImportCollision.AvoidsBuiltIn => await DisplayAlert(
                    "Keep built-in profile",
                    $"{prepared.SourceFolderName} is a built-in profile and will not be overwritten. Store {prepared.Files.Count} tables as {prepared.ProfileName} instead?",
                    "Import",
                    "Cancel"),
                ImportCollision.AvoidsBuiltInAndReplacesImported => await DisplayAlert(
                    "Keep built-in profile",
                    $"{prepared.SourceFolderName} is a built-in profile and will not be overwritten. Replace existing imported profile {prepared.ProfileName} with {prepared.Files.Count} tables?",
                    "Replace",
                    "Cancel"),
                _ => await DisplayAlert(
                    "Import profile",
                    $"Store {prepared.Files.Count} tables as profile {prepared.ProfileName}?",
                    "Import",
                    "Cancel")
            };
        }

        private async Task RefreshAsync()
        {
            try
            {
                _selectedProfile = await _profileService.GetSelectedNameAsync();
                var isImported = !string.IsNullOrEmpty(_selectedProfile)
                                 && _resourceCatalog.IsUserProfile(_selectedProfile);
                CurrentProfileLabel.Text = string.IsNullOrEmpty(_selectedProfile)
                    ? "No profile is selected."
                    : isImported
                        ? $"Current profile: {_selectedProfile} (imported)"
                        : $"Current profile: {_selectedProfile} (built-in)";
                ExportButton.Text = string.IsNullOrEmpty(_selectedProfile)
                    ? "Export current profile"
                    : $"Export {_selectedProfile}";
                RemoveCard.IsVisible = isImported;
                RemoveHelpLabel.Text = isImported
                    ? $"Remove the imported tables for {_selectedProfile}. Built-in profiles are not changed."
                    : string.Empty;
                RemoveButton.Text = isImported
                    ? $"Remove {_selectedProfile}"
                    : "Remove imported profile";
                ImportFolderButton.IsVisible = _transferService.SupportsFolderImport;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh import/export page");
            }
        }

        private void ApplyPlatformHelp()
        {
            var isAndroid = DeviceInfo.Platform == DevicePlatform.Android;
            StorageHelpLabel.Text = isAndroid
                ? "On this device, imported profiles are stored in the app's private data folder. The folder name is the profile name."
                : "On this PC, imported profiles are stored under %AppData%\\LootGen\\user-data. The folder name is the profile name.";
            TransferHelpLabel.Text = isAndroid
                ? "Android cannot browse that folder, so export uses Share and import uses the file picker."
                : "You can copy folders in and out of that location, or use the buttons below.";
            ExportHelpLabel.Text = isAndroid
                ? "Exports the current profile, including built-in ones such as Fallout2d20_Alternative. Opens the share sheet so you can save or send the zip."
                : "Exports the current profile, including built-in ones such as Fallout2d20_Alternative. Saves a zip whose folder name matches the profile.";
            ImportHelpLabel.Text = isAndroid
                ? "Pick a .zip. The folder inside the zip becomes the profile name, unless that folder is a built-in profile — then it is saved as FolderName_imported."
                : "Import a .zip, or a folder of JSON tables. The folder name becomes the profile name, unless it matches a built-in profile — then it is saved as FolderName_imported.";
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            ExportButton.IsEnabled = !busy;
            ImportArchiveButton.IsEnabled = !busy;
            ImportFolderButton.IsEnabled = !busy;
            RemoveButton.IsEnabled = !busy;
        }
    }
}

using GUI.Abstractions;
using GUI.Models;
using Microsoft.Extensions.Logging;

namespace GUI.Services
{
    public sealed class TableTransferService : ITableTransferService
    {
        private readonly IExternalFileTransfer _fileTransfer;
        private readonly IUserLootProfileStore _userStore;
        private readonly IResourceCatalog _resourceCatalog;
        private readonly IProfileService _profileService;
        private readonly ILogger<TableTransferService> _logger;

        public TableTransferService(
            IExternalFileTransfer fileTransfer,
            IUserLootProfileStore userStore,
            IResourceCatalog resourceCatalog,
            IProfileService profileService,
            ILogger<TableTransferService> logger)
        {
            _fileTransfer = fileTransfer;
            _userStore = userStore;
            _resourceCatalog = resourceCatalog;
            _profileService = profileService;
            _logger = logger;
        }

        public bool SupportsFolderImport => _fileTransfer.SupportsFolderImport;

        public async Task<STableTransferResult> ExportSelectedProfileAsync()
        {
            try
            {
                var profile = await _profileService.GetSelectedNameAsync().ConfigureAwait(true);
                if (string.IsNullOrWhiteSpace(profile))
                {
                    return STableTransferResult.Fail("Select a profile before exporting.");
                }

                IReadOnlyList<STableFile> files;
                try
                {
                    files = _resourceCatalog.GetTableFiles(profile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read tables for export of {Profile}", profile);
                    return STableTransferResult.Fail($"Could not read tables for {profile}. {ex.Message}");
                }

                if (files.Count == 0)
                {
                    return STableTransferResult.Fail($"Profile {profile} has no table files to export.");
                }

                byte[] archive;
                try
                {
                    archive = TableArchive.Create(profile, files);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create export archive for {Profile}", profile);
                    return STableTransferResult.Fail("Could not create the export zip.");
                }

                var exported = await _fileTransfer.ExportArchiveAsync($"{profile}.zip", archive).ConfigureAwait(true);
                if (!exported)
                {
                    return STableTransferResult.Cancel();
                }

                _logger.LogInformation("Exported profile {Profile} with {Count} tables", profile, files.Count);
                return STableTransferResult.Ok($"Exported {profile} ({files.Count} tables).", profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export failed");
                return STableTransferResult.Fail($"Could not export the current profile. {ex.Message}");
            }
        }

        public async Task<SPreparedImport?> PrepareArchiveImportAsync()
        {
            var picked = await _fileTransfer.PickArchiveAsync().ConfigureAwait(true);
            if (picked is null)
            {
                return null;
            }

            var archive = picked.Value;
            try
            {
                var fallback = Path.GetFileNameWithoutExtension(archive.FileName);
                var parsed = TableArchive.Parse(archive.Contents, fallback, ImportCollision.None);
                return ResolveDestination(parsed.SourceFolderName, parsed.Files);
            }
            catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException or IOException or ArgumentException)
            {
                _logger.LogWarning(ex, "Failed to read import archive {File}", archive.FileName);
                throw new InvalidOperationException(ex.Message, ex);
            }
        }

        public async Task<SPreparedImport?> PrepareFolderImportAsync()
        {
            if (!_fileTransfer.SupportsFolderImport)
            {
                throw new InvalidOperationException("Folder import is not available on this device.");
            }

            var picked = await _fileTransfer.PickFolderAsync().ConfigureAwait(true);
            if (picked is null)
            {
                return null;
            }

            var folder = picked.Value;
            var name = TableArchive.SanitizeProfileName(folder.FolderName);
            TableArchive.ValidateTables(folder.Files);
            return ResolveDestination(name, folder.Files);
        }

        public async Task<STableTransferResult> CommitImportAsync(SPreparedImport prepared)
        {
            try
            {
                if (_resourceCatalog.IsBuiltInProfile(prepared.ProfileName))
                {
                    return STableTransferResult.Fail(
                        $"{prepared.ProfileName} is a built-in profile and cannot be overwritten.");
                }

                TableArchive.ValidateTables(prepared.Files);
                _userStore.Replace(prepared.ProfileName, prepared.Files);
                _resourceCatalog.NotifyUserDataChanged();
                await _profileService.GetAsync().ConfigureAwait(false);
                var selected = await _profileService.SelectAsync(prepared.ProfileName).ConfigureAwait(false);
                if (!selected)
                {
                    _logger.LogWarning("Imported {Profile} but could not select it", prepared.ProfileName);
                }

                _logger.LogInformation(
                    "Imported profile {Profile} with {Count} tables",
                    prepared.ProfileName,
                    prepared.Files.Count);
                return STableTransferResult.Ok(
                    $"Imported {prepared.ProfileName} ({prepared.Files.Count} tables). It is now the selected profile.",
                    prepared.ProfileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import profile {Profile}", prepared.ProfileName);
                return STableTransferResult.Fail("Could not store the imported tables.");
            }
        }

        public async Task<STableTransferResult> RemoveImportedProfileAsync(string profileName)
        {
            if (_resourceCatalog.IsBuiltInProfile(profileName) || !_userStore.Exists(profileName))
            {
                return STableTransferResult.Fail($"{profileName} is not an imported profile.");
            }

            try
            {
                _userStore.Delete(profileName);
                _resourceCatalog.NotifyUserDataChanged();
                await _profileService.GetAsync().ConfigureAwait(false);
                return STableTransferResult.Ok($"Removed imported profile {profileName}.", profileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove imported profile {Profile}", profileName);
                return STableTransferResult.Fail("Could not remove the imported profile.");
            }
        }

        private SPreparedImport ResolveDestination(string sourceFolderName, IReadOnlyList<STableFile> files)
        {
            var source = TableArchive.SanitizeProfileName(sourceFolderName);
            if (_resourceCatalog.IsBuiltInProfile(source))
            {
                var destination = NameThatAvoidsBuiltIns(source);
                var collision = _userStore.Exists(destination)
                    ? ImportCollision.AvoidsBuiltInAndReplacesImported
                    : ImportCollision.AvoidsBuiltIn;
                return new SPreparedImport(destination, source, files, collision);
            }

            var importedCollision = _userStore.Exists(source)
                ? ImportCollision.ReplacesImported
                : ImportCollision.None;
            return new SPreparedImport(source, source, files, importedCollision);
        }

        private string NameThatAvoidsBuiltIns(string builtInName)
        {
            var candidate = TableArchive.ToImportedProfileName(builtInName);
            var n = 2;
            while (_resourceCatalog.IsBuiltInProfile(candidate))
            {
                candidate = $"{builtInName}_imported{n}";
                n++;
            }

            return TableArchive.SanitizeProfileName(candidate);
        }
    }
}

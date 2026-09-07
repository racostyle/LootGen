using GUI.Abstractions;
using GUI.Models;
using Microsoft.Extensions.Logging;

namespace GUI.Infrastructure
{
    public sealed class ShareExternalFileTransfer : IExternalFileTransfer
    {
        private readonly ILogger<ShareExternalFileTransfer> _logger;

        public ShareExternalFileTransfer(ILogger<ShareExternalFileTransfer> logger)
        {
            _logger = logger;
        }

        public bool SupportsFolderImport => false;

        public Task<SPickedArchive?> PickArchiveAsync()
        {
            return ArchiveFilePicker.PickAsync();
        }

        public Task<SPickedFolder?> PickFolderAsync()
        {
            return Task.FromResult<SPickedFolder?>(null);
        }

        public async Task<bool> ExportArchiveAsync(string fileName, byte[] contents)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ArgumentNullException.ThrowIfNull(contents);

            var path = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(path, contents);
            _logger.LogInformation("Sharing export archive {File}", fileName);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export loot tables",
                File = new ShareFile(path, "application/zip")
            });

            return true;
        }
    }
}

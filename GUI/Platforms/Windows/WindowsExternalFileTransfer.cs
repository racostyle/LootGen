using GUI.Abstractions;
using GUI.Models;
using GUI.Services;
using Microsoft.Extensions.Logging;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace GUI.Infrastructure
{
    public sealed class WindowsExternalFileTransfer : IExternalFileTransfer
    {
        private readonly ILogger<WindowsExternalFileTransfer> _logger;

        public WindowsExternalFileTransfer(ILogger<WindowsExternalFileTransfer> logger)
        {
            _logger = logger;
        }

        public bool SupportsFolderImport => true;

        public Task<SPickedArchive?> PickArchiveAsync()
        {
            return ArchiveFilePicker.PickAsync();
        }

        public async Task<SPickedFolder?> PickFolderAsync()
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add("*");
            AttachToWindow(picker);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                return null;
            }

            var files = new List<STableFile>();
            foreach (var path in Directory.GetFiles(folder.Path, "*.json", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(folder.Path, path).Replace('\\', '/');
                if (TableArchive.ShouldSkipTableFile(relative))
                {
                    continue;
                }

                files.Add(new STableFile(relative, File.ReadAllText(path)));
            }

            _logger.LogInformation("Picked import folder {Folder} with {Count} json files", folder.Name, files.Count);
            return new SPickedFolder(folder.Name, files);
        }

        public async Task<bool> ExportArchiveAsync(string fileName, byte[] contents)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ArgumentNullException.ThrowIfNull(contents);

            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = Path.GetFileNameWithoutExtension(fileName)
            };
            picker.FileTypeChoices.Add("ZIP archive", new List<string> { ".zip" });
            AttachToWindow(picker);

            var file = await picker.PickSaveFileAsync();
            if (file is null)
            {
                return false;
            }

            await FileIO.WriteBytesAsync(file, contents);
            _logger.LogInformation("Saved export archive to {Path}", file.Path);
            return true;
        }

        private static void AttachToWindow(object picker)
        {
            var nativeWindow = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window
                               ?? throw new InvalidOperationException("No window is available for the file picker.");
            var hwnd = WindowNative.GetWindowHandle(nativeWindow);
            InitializeWithWindow.Initialize(picker, hwnd);
        }
    }
}

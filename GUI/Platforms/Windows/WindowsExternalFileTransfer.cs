using System.Runtime.InteropServices;
using GUI.Abstractions;
using GUI.Models;
using GUI.Services;
using Microsoft.Extensions.Logging;
using WinForms = System.Windows.Forms;

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

        public async Task<SPickedArchive?> PickArchiveAsync()
        {
            using var dialog = new WinForms.OpenFileDialog
            {
                Title = "Select a loot table zip",
                Filter = "ZIP archive (*.zip)|*.zip|All files (*.*)|*.*",
                DefaultExt = "zip",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(Owner()) != WinForms.DialogResult.OK)
            {
                return null;
            }

            var contents = await File.ReadAllBytesAsync(dialog.FileName);
            _logger.LogInformation("Picked import archive {File}", dialog.FileName);
            return new SPickedArchive(Path.GetFileName(dialog.FileName), contents);
        }

        public Task<SPickedFolder?> PickFolderAsync()
        {
            using var dialog = new WinForms.FolderBrowserDialog
            {
                Description = "Select a folder of loot table JSON files",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog(Owner()) != WinForms.DialogResult.OK)
            {
                return Task.FromResult<SPickedFolder?>(null);
            }

            var files = new List<STableFile>();
            foreach (var path in Directory.GetFiles(dialog.SelectedPath, "*.json", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(dialog.SelectedPath, path).Replace('\\', '/');
                if (TableArchive.ShouldSkipTableFile(relative))
                {
                    continue;
                }

                files.Add(new STableFile(relative, File.ReadAllText(path)));
            }

            var folderName = Path.GetFileName(dialog.SelectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            _logger.LogInformation("Picked import folder {Folder} with {Count} json files", folderName, files.Count);
            return Task.FromResult<SPickedFolder?>(new SPickedFolder(folderName, files));
        }

        public async Task<bool> ExportArchiveAsync(string fileName, byte[] contents)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ArgumentNullException.ThrowIfNull(contents);

            using var dialog = new WinForms.SaveFileDialog
            {
                Title = "Export loot tables",
                Filter = "ZIP archive (*.zip)|*.zip",
                DefaultExt = "zip",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = Path.GetFileName(fileName)
            };

            if (dialog.ShowDialog(Owner()) != WinForms.DialogResult.OK)
            {
                return false;
            }

            await File.WriteAllBytesAsync(dialog.FileName, contents);
            _logger.LogInformation("Saved export archive to {Path}", dialog.FileName);
            return true;
        }

        private static WinForms.IWin32Window Owner()
        {
            return new NativeOwner(GetWindowHandle());
        }

        private static nint GetWindowHandle()
        {
            foreach (var window in Microsoft.Maui.Controls.Application.Current?.Windows ?? [])
            {
                if (window.Handler?.PlatformView is MauiWinUIWindow mauiWindow && mauiWindow.WindowHandle != 0)
                {
                    return mauiWindow.WindowHandle;
                }
            }

            var hwnd = GetActiveWindow();
            if (hwnd != 0)
            {
                return hwnd;
            }

            hwnd = GetForegroundWindow();
            if (hwnd != 0)
            {
                return hwnd;
            }

            throw new InvalidOperationException("No window is available for the file dialog.");
        }

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern nint GetActiveWindow();

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern nint GetForegroundWindow();

        private sealed class NativeOwner : WinForms.IWin32Window
        {
            public NativeOwner(nint handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }
        }
    }
}

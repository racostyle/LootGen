using GUI.Models;

namespace GUI.Infrastructure
{
    internal static class ArchiveFilePicker
    {
        private static readonly FilePickerFileType ZipType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.WinUI, new[] { ".zip" } },
            { DevicePlatform.Android, new[] { "application/zip", "application/x-zip-compressed", "application/octet-stream" } },
            { DevicePlatform.iOS, new[] { "public.zip-archive", "com.pkware.zip-archive" } },
            { DevicePlatform.MacCatalyst, new[] { "public.zip-archive", "zip" } }
        });

        public static async Task<SPickedArchive?> PickAsync()
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a loot table zip",
                FileTypes = ZipType
            });

            if (result is null)
            {
                return null;
            }

            using var stream = await result.OpenReadAsync();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            return new SPickedArchive(result.FileName, memory.ToArray());
        }
    }
}

using GUI.Models;

namespace GUI.Abstractions
{
    public interface IExternalFileTransfer
    {
        bool SupportsFolderImport { get; }

        Task<SPickedArchive?> PickArchiveAsync();

        Task<SPickedFolder?> PickFolderAsync();

        Task<bool> ExportArchiveAsync(string fileName, byte[] contents);
    }
}

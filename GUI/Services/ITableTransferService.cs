using GUI.Models;

namespace GUI.Services
{
    public interface ITableTransferService
    {
        bool SupportsFolderImport { get; }

        Task<STableTransferResult> ExportSelectedProfileAsync();

        Task<SPreparedImport?> PrepareArchiveImportAsync();

        Task<SPreparedImport?> PrepareFolderImportAsync();

        Task<STableTransferResult> CommitImportAsync(SPreparedImport prepared);

        Task<STableTransferResult> RemoveImportedProfileAsync(string profileName);
    }
}

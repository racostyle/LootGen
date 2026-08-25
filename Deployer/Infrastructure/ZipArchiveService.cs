using System.IO.Compression;
using Deployer.Abstractions;

namespace Deployer.Infrastructure
{
    public sealed class ZipArchiveService : IArchiveService
    {
        private readonly IFileSystem _fileSystem;

        public ZipArchiveService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void CreateZipFromDirectory(string sourceDirectory, string zipPath, string entryPrefix)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(zipPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(entryPrefix);

            _fileSystem.DeleteFile(zipPath);
            var directory = _fileSystem.GetDirectoryName(zipPath);
            if (!string.IsNullOrEmpty(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            foreach (var file in _fileSystem.EnumerateFiles(sourceDirectory, "*", recursive: true))
            {
                var relative = Path.GetRelativePath(sourceDirectory, file);
                var entryName = Path.Combine(entryPrefix, relative).Replace('\\', '/');
                archive.CreateEntryFromFile(file, entryName, CompressionLevel.SmallestSize);
            }
        }
    }
}

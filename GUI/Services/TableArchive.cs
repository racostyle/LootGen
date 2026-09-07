using System.IO.Compression;
using System.Text;
using System.Text.Json;
using GUI.Models;

namespace GUI.Services
{
    public static class TableArchive
    {
        public const string SettingsFileName = "settings.json";

        public const string ImportedNameSuffix = "_imported";

        public static string ToImportedProfileName(string builtInName)
        {
            return SanitizeProfileName($"{SanitizeProfileName(builtInName)}{ImportedNameSuffix}");
        }

        public static byte[] Create(string profileName, IReadOnlyList<STableFile> files)
        {
            var folder = SanitizeProfileName(profileName);
            using var stream = new MemoryStream();
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var file in files)
                {
                    var relative = NormalizeEntryPath(file.RelativePath);
                    var entry = zip.CreateEntry($"{folder}/{relative}", CompressionLevel.Fastest);
                    using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    writer.Write(file.Contents);
                }
            }

            return stream.ToArray();
        }

        public static SPreparedImport Parse(byte[] zipBytes, string fallbackProfileName, ImportCollision collision)
        {
            ArgumentNullException.ThrowIfNull(zipBytes);

            using var stream = new MemoryStream(zipBytes, writable: false);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

            var files = new List<STableFile>();
            string? folderName = null;
            var sawRootFile = false;

            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var full = NormalizeEntryPath(entry.FullName);
                var segments = full.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 0)
                {
                    continue;
                }

                if (ShouldSkipEntry(segments))
                {
                    continue;
                }

                if (segments.Length == 1)
                {
                    sawRootFile = true;
                    files.Add(ReadEntry(entry, segments[0]));
                    continue;
                }

                var top = segments[0];
                if (folderName is null)
                {
                    folderName = top;
                }
                else if (!string.Equals(folderName, top, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The zip must contain a single folder of tables. That folder name becomes the profile.");
                }

                var relative = string.Join('/', segments.Skip(1));
                files.Add(ReadEntry(entry, relative));
            }

            if (folderName is not null && sawRootFile)
            {
                throw new InvalidOperationException(
                    "The zip mixes a profile folder with files at the root. Put every JSON table inside one folder.");
            }

            var profileName = folderName ?? SanitizeProfileName(fallbackProfileName);
            ValidateTables(files);
            return new SPreparedImport(profileName, profileName, files, collision);
        }

        public static void ValidateTables(IReadOnlyList<STableFile> files)
        {
            if (files.Count == 0)
            {
                throw new InvalidOperationException("No loot table JSON files were found.");
            }

            foreach (var file in files)
            {
                if (!IsTableJson(file.Contents, out var error))
                {
                    throw new InvalidOperationException($"{file.RelativePath}: {error}");
                }
            }
        }

        public static string SanitizeProfileName(string name)
        {
            var trimmed = name.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                throw new InvalidOperationException("The profile folder name is empty.");
            }

            if (trimmed is "." or "..")
            {
                throw new InvalidOperationException("The profile folder name is not valid.");
            }

            if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidOperationException("The profile folder name is not a valid folder name.");
            }

            return trimmed;
        }

        public static bool IsTableJson(string json, out string error)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    error = "Table JSON must be an object.";
                    return false;
                }

                if (!document.RootElement.TryGetProperty("Type", out var type)
                    || type.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(type.GetString()))
                {
                    error = "Table JSON must include a Type.";
                    return false;
                }

                if (!document.RootElement.TryGetProperty("Data", out var data)
                    || data.ValueKind != JsonValueKind.Array)
                {
                    error = "Table JSON must include a Data array.";
                    return false;
                }

                error = string.Empty;
                return true;
            }
            catch (JsonException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool ShouldSkipTableFile(string relativePath)
        {
            var name = Path.GetFileName(relativePath);
            return string.Equals(name, SettingsFileName, StringComparison.OrdinalIgnoreCase)
                   || !relativePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
        }

        private static STableFile ReadEntry(ZipArchiveEntry entry, string relativePath)
        {
            using var reader = new StreamReader(entry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return new STableFile(relativePath, reader.ReadToEnd());
        }

        private static string NormalizeEntryPath(string path)
        {
            var normalized = path.Replace('\\', '/').Trim('/');
            if (string.IsNullOrEmpty(normalized)
                || Path.IsPathRooted(path)
                || normalized.Split('/').Any(segment => segment is ".." or "." or ""))
            {
                throw new InvalidOperationException("The zip contains an unsafe file path.");
            }

            return normalized;
        }

        private static bool ShouldSkipEntry(string[] segments)
        {
            if (segments[0].Equals("__MACOSX", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var fileName = segments[^1];
            if (fileName.StartsWith('.') || fileName.StartsWith("._", StringComparison.Ordinal))
            {
                return true;
            }

            return ShouldSkipTableFile(fileName);
        }
    }
}

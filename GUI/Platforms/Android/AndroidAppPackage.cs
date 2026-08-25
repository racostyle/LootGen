using Android.Content.Res;
using GUI.Abstractions;

namespace GUI
{
    public sealed class AndroidAppPackage : IAppPackage
    {
        public IReadOnlyList<string> ListFiles(string relativeDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativeDirectory);
            var assets = RequiredAssets();
            var start = relativeDirectory.Replace('\\', '/').Trim('/');
            var listed = Enumerate(assets, start).ToArray();
            if (listed.Length > 0)
            {
                return listed;
            }

            var prefix = start + "/";
            return Enumerate(assets, string.Empty)
                .Where(path => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                               || path.Equals(start, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        public string ReadAllText(string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
            var assets = RequiredAssets();
            var normalized = relativePath.Replace('\\', '/').TrimStart('/');
            using var stream = Open(assets, normalized);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static IEnumerable<string> Enumerate(AssetManager assets, string path)
        {
            var entries = List(assets, path);
            if (entries.Length == 0)
            {
                yield break;
            }

            foreach (var entry in entries)
            {
                var child = string.IsNullOrEmpty(path) ? entry : $"{path}/{entry}";
                var nested = List(assets, child);
                if (nested.Length > 0)
                {
                    foreach (var file in Enumerate(assets, child))
                    {
                        yield return file;
                    }

                    continue;
                }

                if (entry.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    yield return child;
                }
            }
        }

        private static Stream Open(AssetManager assets, string relativePath)
        {
            try
            {
                return assets.Open(relativePath);
            }
            catch (Java.IO.FileNotFoundException)
            {
                return assets.Open(relativePath.Replace('/', '\\'));
            }
        }

        private static string[] List(AssetManager assets, string path)
        {
            try
            {
                return assets.List(path) ?? [];
            }
            catch (Java.IO.FileNotFoundException)
            {
                return [];
            }
        }

        private static AssetManager RequiredAssets()
        {
            return Android.App.Application.Context.Assets
                   ?? throw new InvalidOperationException("Android assets are not available.");
        }
    }
}

using System.Text.Json;
using TableLib.DataHelpers;

namespace TableLib
{
    public sealed class FileLoader
    {
        public IReadOnlyList<IDataBatch> Batches { get; }

        public FileLoader(string pathToFiles)
        {
            Batches = Load(pathToFiles);
        }

        private static IDataBatch[] Load(string pathToFiles)
        {
            if (!Directory.Exists(pathToFiles))
            {
                return [];
            }

            var builder = new List<IDataBatch>();
            var files = Directory.GetFiles(pathToFiles, "*.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<DataFile>(json);
                if (data is null)
                {
                    continue;
                }

                builder.Add(new DataBatch(data));
            }

            return builder.ToArray();
        }
    }
}

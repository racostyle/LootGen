using System.Text.Json;
using TableLib.DataHelpers;

namespace TableLib
{
    public class FileLoader
    {
        public IDataBatch[] ParseTables(string pathToFiles)
        {
            var builder = new List<IDataBatch>();

            var files = Directory.GetFiles(pathToFiles, "*.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<DataFile>(json);

                var batch = new DataBatch(data);
                builder.Add(batch);
            }

            return builder.ToArray();
        }
    }
}


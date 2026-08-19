namespace TableLib.DataHelpers
{
    internal class DataFile
    {
        public string Type { get; set; }
        public string[] Categories { get; set; }
        public string[][] Data { get; set; }

        public DataFile(string type, string[] categories, string[][] data)
        {
            Type = type;
            Categories = categories;
            Data = data;
        }
    }
}

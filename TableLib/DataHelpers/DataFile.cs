namespace TableLib.DataHelpers
{
    internal class DataFile
    {
        public string Type { get; set; }
        public int Spotlight { get; set; }
        public string[][] Data { get; set; }

        public DataFile(string type, int spotlight, string[][] data)
        {
            Type = type;
            Spotlight = spotlight;
            Data = data;
        }
    }
}

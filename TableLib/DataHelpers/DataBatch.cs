namespace TableLib.DataHelpers
{
    public class DataBatch : IDataBatch
    {
        public string Category { get; set; }
        public int Spotlight { get; set; }

        public TableItem[] Table { get; set; }

        internal DataBatch(DataFile file)
        {
            Category = file.Type;
            Spotlight = file.Spotlight;

            var tmp = new List<TableItem>();

            foreach (var item in file.Data)
            {
                try
                {
                    var parsed = new TableItem(item, Category);
                    tmp.Add(parsed);
                }
                catch { }
            }

            Table = tmp.ToArray();
        }
    }
}

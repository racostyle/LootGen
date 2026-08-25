namespace TableLib.DataHelpers
{
    public class DataBatch : IDataBatch
    {
        public string Category { get; private set; }
        public int Spotlight { get; private set; }
        public TableItem[] Table { get; private set; }

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

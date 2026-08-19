namespace TableLib.DataHelpers
{
    public class DataBatch : IDataBatch
    {
        public string Category { get; set; }
        public TableItem[] Table { get; set; }

        internal DataBatch(DataFile file)
        {
            Category = file.Type;

            var tmp = new List<TableItem>();

            foreach (var item in file.Data)
            {
                try
                {
                    var parsed = new TableItem(item);
                    tmp.Add(parsed);
                }
                catch { }
            }

            Table = tmp.ToArray();
        }
    }
}

namespace TableLib
{
    public interface IDataBatch
    {
        string Category { get; set; }
        public int Spotlight { get; set; }
        TableItem[] Table { get; set; }
    }
}
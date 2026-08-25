namespace TableLib
{
    public interface IDataBatch
    {
        string Category { get; }
        public int Spotlight { get; }
        TableItem[] Table { get; }
    }
}
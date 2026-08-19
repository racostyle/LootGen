namespace TableLib
{
    public interface IDataBatch
    {
        string Category { get; set; }
        TableItem[] Table { get; set; }
    }
}
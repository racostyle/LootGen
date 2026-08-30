using TableLib;

namespace GUI.Services
{
    public interface IGenerateResultStore
    {
        TableItem[] Items { get; set; }

        string Title { get; set; }
    }
}

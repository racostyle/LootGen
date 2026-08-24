using TableLib;

namespace GUI.Services
{
    public sealed class GenerateResultStore : IGenerateResultStore
    {
        public TableItem[] Items { get; set; } = [];
    }
}

namespace TableLib
{
    public class TableGenerator
    {
        private readonly IDataBatch[] _dataBatches;
        private readonly int _maxSpotlight;

        public TableGenerator(IReadOnlyList<IDataBatch> dataBatches)
        {
            _dataBatches = dataBatches.ToArray();
            _maxSpotlight = _dataBatches.Length == 0
                ? 1
                : _dataBatches.Max(x => x.Spotlight) + 1;
        }

        public TableItem[] Fetch(int rarity, int size, params string[] categories)
        {
            var filtered = GetFilteredBatch(categories);
            if (filtered.Length == 0)
            {
                return [];
            }

            int item_count = (size * 5) + 5;
            var store = new List<TableItem>();
            var attempts = 0;

            while (item_count >= 0 && attempts < 10_000)
            {
                attempts++;
                var selectedDataBatch = filtered[Random.Shared.Next(0, filtered.Length)];

                bool isSpotlight = filtered.Length > 1
                    ? IsTableInSpotlight(selectedDataBatch)
                    : true;

                if (!isSpotlight)
                {
                    continue;
                }

                var availableItems = selectedDataBatch.Table.Where(x => x.Rarity <= rarity).ToArray();
                if (availableItems.Length == 0)
                {
                    continue;
                }

                store.Add(availableItems[Random.Shared.Next(0, availableItems.Length)]);
                item_count--;
            }

            return store
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Name)
                .ToArray();
        }

        private bool IsTableInSpotlight(IDataBatch filtered)
        {
            var spotlight = Random.Shared.Next(0, _maxSpotlight);
            return filtered.Spotlight >= spotlight;
        }

        public IDataBatch[] GetFilteredBatch(string[] categories)
        {
            if (categories.Length == 0)
            {
                return _dataBatches;
            }

            return _dataBatches
                .Where(x => categories.Contains(x.Category))
                .ToArray();
        }
    }
}

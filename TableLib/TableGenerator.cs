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

        public TableItem[] Fetch(int rarity, int size, bool isUniqueSelection, params string[] categories)
        {
            var filtered = GetFilteredBatch(categories);
            if (filtered.Length == 0)
            {
                return [];
            }

            int item_count = (size * 5) + 5;
            var store = new List<TableItem>();
            var attempts = 0;

            while (item_count >= 0 && attempts < 1_000)
            {
                attempts++;
                var selectedDataBatch = filtered[Random.Shared.Next(0, filtered.Length)];

                if (!IsInSpotlight(filtered, selectedDataBatch))
                    continue;

                var availableItems = selectedDataBatch.Table.Where(x => x.Rarity <= rarity).ToArray();
                if (availableItems.Length == 0)
                    continue;


                var selected = availableItems[Random.Shared.Next(0, availableItems.Length)];
                
                if (isUniqueSelection)
                {
                    bool hasAllSeedItems = availableItems
                        .Select(x => x.Hash)
                        .Distinct()
                        .All(hash => store.Any(x => x.Hash == hash));

                    if (hasAllSeedItems)
                        break;

                    if (store.Where(x => x.Hash == selected.Hash).Any())
                        continue;
                }

                store.Add(selected);
                item_count--;
            }

            return store
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Name)
                .ToArray();
        }

        public TableItem[] ListAll(int rarity, params string[] categories)
        {
            var filtered = GetFilteredBatch(categories);
            return filtered
                .SelectMany(batch => batch.Table.Where(x => x.Rarity <= rarity))
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Name)
                .ToArray();
        }

        private bool IsInSpotlight(IDataBatch[] filtered, IDataBatch selectedDataBatch)
        {
            return filtered.Length > 1
                   ? IsTableInSpotlight(selectedDataBatch)
                   : true;
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

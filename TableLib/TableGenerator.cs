using TableLib.DataHelpers;

namespace TableLib
{
    public class TableGenerator
    {
        private readonly DataBatch[] _dataBatches;
        private readonly int _maxSpotlight;
        public TableGenerator(DataBatch[] dataBatches)
        {
            _dataBatches = dataBatches;
            _maxSpotlight = _dataBatches.Select(x => x.Spotlight).Max() + 1;
        }

        internal TableItem[] Fetch(int rarity, int size, params string[] categories)
        {
            int item_count = (size * 5) + 5;

            var store = new List<TableItem>();

            var filtered = GetFilteredBatch(categories);

            while (item_count >= 0)
            {
                var selectedDataBatch = filtered[Random.Shared.Next(0, filtered.Length)];

                bool isSpotlight = filtered.Length > 1
                    ? IsTableInSpotlight(selectedDataBatch)
                    : true;

                if (!isSpotlight) continue;

                var availableItems = selectedDataBatch.Table.Where(x => x.Rarity <= rarity).ToArray();

                if (availableItems.Length == 0) continue;

                var item = availableItems[Random.Shared.Next(0, availableItems.Length)];
                store.Add(item);
                item_count--;
            }

            return store.OrderBy(x => x.Category)
                .ThenBy(x => x.Name)
                .ToArray();
        }

        private bool IsTableInSpotlight(DataBatch filtered)
        {
            var spotlight = Random.Shared.Next(0, _maxSpotlight);

            return filtered.Spotlight >= spotlight;
        }

        public DataBatch[] GetFilteredBatch(string[] categories)
        {
            if (categories.Length == 0)
                return _dataBatches;

            var filtered = _dataBatches.Where(x => categories.Contains(x.Category)).ToArray();

            return filtered.Length == 0
                ? _dataBatches
                : filtered;
        }
    }
}

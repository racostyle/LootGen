using GUI.Models;

namespace GUI.Services
{
    public static class LootScale
    {
        public const int DefaultRarityValue = 1;

        public static IReadOnlyList<SLabeledValue> Rarities { get; } =
        [
            new("Common", 0),
            new("Uncommon", 1),
            new("Rare", 2),
            new("Very Rare", 3),
            new("Legendary", 4),
            new("Artifact", 5)
        ];

        public static IReadOnlyList<SLabeledValue> Sizes { get; } =
        [
            new("Tiny", 0),
            new("Small", 1),
            new("Medium", 2),
            new("Big", 3),
            new("Large", 4)
        ];

        public static int IndexOfOrFirst(IReadOnlyList<SLabeledValue> items, int? value)
        {
            if (items.Count == 0)
            {
                return -1;
            }

            if (value is int target)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i].Value == target)
                    {
                        return i;
                    }
                }
            }

            return 0;
        }
    }
}

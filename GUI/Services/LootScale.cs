using GUI.Models;

namespace GUI.Services
{
    public static class LootScale
    {
        public static IReadOnlyList<SLabeledValue> Rarities { get; } =
        [
            new("Uncommon", 0),
            new("Common", 1),
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
    }
}

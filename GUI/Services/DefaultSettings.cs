using GUI.Models;

namespace GUI.Services
{
    public static class DefaultSettings
    {
        public static IReadOnlyList<string> Types { get; } =
        [
            "Weapons",
            "Armor",
            "Drinks",
            "Food",
            "Clothing",
            "Medical"
        ];

        public static IReadOnlyList<string> Rarities { get; } =
        [
            "Common",
            "Uncommon",
            "Rare",
            "Legendary"
        ];

        public static IReadOnlyList<string> Sizes { get; } =
        [
            "Tiny",
            "Small",
            "Medium",
            "Big",
            "Large"
        ];

        public static AppSettings Create()
        {
            return new AppSettings
            {
                Types = [.. Types],
                Rarities = [.. Rarities],
                Sizes = [.. Sizes]
            };
        }
    }
}

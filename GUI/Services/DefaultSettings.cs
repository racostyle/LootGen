using GUI.Models;

namespace GUI.Services
{
    public static class DefaultSettings
    {
        public static IReadOnlyList<string> Rarities { get; } =
        [
            "Uncommon",
            "Common",
            "Rare",
            "Very Rare",
            "Legendary",
            "Artifact"
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
                Rarities = [.. Rarities],
                Sizes = [.. Sizes]
            };
        }
    }
}

namespace GUI.Models
{
    public sealed class AppSettings
    {
        public List<string> Types { get; set; } = [];

        public List<string> Rarities { get; set; } = [];

        public List<string> Sizes { get; set; } = [];

        public AppSettings Clone()
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

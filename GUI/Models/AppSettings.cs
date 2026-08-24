namespace GUI.Models
{
    public sealed class AppSettings
    {
        public List<string> Rarities { get; set; } = [];

        public List<string> Sizes { get; set; } = [];

        public AppSettings Clone()
        {
            return new AppSettings
            {
                Rarities = [.. Rarities],
                Sizes = [.. Sizes]
            };
        }
    }
}

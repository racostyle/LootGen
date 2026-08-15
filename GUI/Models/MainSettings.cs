namespace GUI.Models
{
    public sealed class MainSettings
    {
        public List<string> Profiles { get; set; } = [];

        public string SelectedProfile { get; set; } = string.Empty;

        public MainSettings Clone()
        {
            return new MainSettings
            {
                Profiles = [.. Profiles],
                SelectedProfile = SelectedProfile
            };
        }
    }
}

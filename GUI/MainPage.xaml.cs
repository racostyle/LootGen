using GUI.Pages;

namespace GUI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnGenerateClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(GeneratePage));
        }

        private async void OnSettingsClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }
    }
}

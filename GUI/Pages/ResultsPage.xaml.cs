using GUI.Services;
using TableLib;

namespace GUI.Pages
{
    public partial class ResultsPage : ContentPage
    {
        private readonly IGenerateResultStore _resultStore;

        public ResultsPage(IGenerateResultStore resultStore)
        {
            InitializeComponent();
            _resultStore = resultStore;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Render(_resultStore.Items);
        }

        private void Render(IReadOnlyList<TableItem> items)
        {
            ResultsLayout.Children.Clear();
            if (items.Count == 0)
            {
                ResultsLayout.Children.Add(new Label
                {
                    Text = "No items generated."
                });
                return;
            }

            var resources = Application.Current?.Resources;
            var evenColor = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#252525")
                : Color.FromArgb("#F2F2F2");

            var oddColor = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#303030")
                : Color.FromArgb("#E6E6E6");

            for (var i = 0; i < items.Count; i++)
            {
                ResultsLayout.Children.Add(CreateRow(items[i], i % 2 == 0 ? evenColor : oddColor));
            }
        }

        private static View CreateRow(TableItem item, Color background)
        {
            var block = new VerticalStackLayout
            {
                Padding = 12,
                Spacing = 4,
                BackgroundColor = background
            };

            block.Children.Add(CreateLine($"Name: {item.Name.PadRight(25)}"));
            block.Children.Add(CreateLine($"Category: {item.Category.PadRight(15)}Cost: {item.Cost.PadRight(10)}Weight: {item.Weight.PadRight(6)}"));
            block.Children.Add(CreateLine($"{item.Description}"));
            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                block.Children.Add(CreateLine($"{item.Notes}"));
            }

            return block;
        }

        private static Label CreateLine(string text)
        {
            return new Label
            {
                Text = text,
                FontFamily = "Consolas",
                LineBreakMode = LineBreakMode.WordWrap
            };
        }
    }
}

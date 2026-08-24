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
            var evenColor = resources is null ? Colors.LightGray : (Color)resources["Gray100"];
            var oddColor = resources is null ? Colors.Silver : (Color)resources["Gray200"];

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

            block.Children.Add(CreateLine($"{item.Category.PadRight(15)}{item.Name.PadRight(25)}{item.Cost.PadRight(10)}{item.Weight.PadRight(6)}{item.Description}"));
            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                block.Children.Add(CreateLine($"Notes: {item.Notes}"));
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

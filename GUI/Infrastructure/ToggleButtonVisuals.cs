namespace GUI.Infrastructure
{
    public static class ToggleButtonVisuals
    {
        public static void Apply(Button button, bool isSelected)
        {
            var resources = Application.Current?.Resources;
            if (resources is null)
            {
                return;
            }

            button.BackgroundColor = isSelected
                ? (Color)resources["Primary"]
                : (Color)resources["Gray500"];

            if (Application.Current?.RequestedTheme == AppTheme.Dark)
            {
                button.TextColor = (Color)resources["White"];
            }
        }
    }
}

using Wpf.Ui.Appearance;

namespace EngMemAssist.Infrastructure;

public static class ThemeHelper
{
    public static readonly string[] Themes = ["Dark", "Light"];

    public static void Apply(string? theme) =>
        ApplicationThemeManager.Apply(
            string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase)
                ? ApplicationTheme.Light
                : ApplicationTheme.Dark);
}

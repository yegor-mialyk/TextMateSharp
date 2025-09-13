namespace TextMateSharp.Themes;

public interface IRawTheme
{
    string? GetInclude();

    ICollection<IRawThemeSetting>? GetSettings();

    ICollection<IRawThemeSetting>? GetTokenColors();

    ICollection<KeyValuePair<string, object>>? GetGuiColors();
}

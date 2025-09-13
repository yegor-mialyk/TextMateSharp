using System.Collections;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Themes;

public class ThemeRaw : Dictionary<string, object>, IRawTheme, IRawThemeSetting, IThemeSetting
{
    private const string NAME = "name";
    private const string INCLUDE = "include";
    private const string SETTINGS = "settings";
    private const string COLORS = "colors";
    private const string TOKEN_COLORS = "tokenColors";
    private const string SCOPE = "scope";
    private const string FONT_STYLE = "fontStyle";
    private const string BACKGROUND = "background";
    private const string FOREGROUND = "foreground";

    public string? GetName()
    {
        return TryGetObject<string>(NAME);
    }

    public string? GetInclude()
    {
        return TryGetObject<string>(INCLUDE);
    }

    public ICollection<IRawThemeSetting>? GetSettings()
    {
        var result = TryGetObject<ICollection>(SETTINGS);

        return result?.Cast<IRawThemeSetting>().ToList();
    }

    public ICollection<IRawThemeSetting>? GetTokenColors()
    {
        var result = TryGetObject<ICollection>(TOKEN_COLORS);

        return result?.Cast<IRawThemeSetting>().ToList();
    }

    public ICollection<KeyValuePair<string, object>>? GetGuiColors()
    {
        var result = TryGetObject<ICollection>(COLORS);

        return result?.Cast<KeyValuePair<string, object>>().ToList();
    }

    public object? GetScope()
    {
        return TryGetObject<object>(SCOPE);
    }

    public IThemeSetting? GetSetting()
    {
        return TryGetObject<IThemeSetting>(SETTINGS);
    }

    public object? GetFontStyle()
    {
        return TryGetObject<object>(FONT_STYLE);
    }

    public string? GetBackground()
    {
        return TryGetObject<string>(BACKGROUND);
    }

    public string? GetForeground()
    {
        return TryGetObject<string>(FOREGROUND);
    }

    private T? TryGetObject<T>(string key)
    {
        if (!TryGetValue(key, out var result))
            return default;

        return (T) result;
    }
}

namespace TextMateSharp.Themes;

public class ParsedThemeRule
{
    public string? Background { get; }
    public FontStyle FontStyle { get; }
    public string? Foreground { get; }
    public int Index { get; }
    public string? Name { get; }
    public List<string>? ParentScopes { get; }
    public string Scope { get; }

    public ParsedThemeRule(string? name, string scope, List<string>? parentScopes, int index, FontStyle fontStyle,
        string? foreground, string? background)
    {
        Name = name;
        Scope = scope;
        ParentScopes = parentScopes;
        Index = index;
        FontStyle = fontStyle;
        Foreground = foreground;
        Background = background;
    }
}

using System.Collections.ObjectModel;
using TextMateSharp.Internal.Utils;
using TextMateSharp.Registry;

namespace TextMateSharp.Themes;

public class Theme
{
    private readonly ColorMap _colorMap;
    private readonly Dictionary<string, string> _guiColorDictionary;
    private readonly ParsedTheme _include;
    private readonly ParsedTheme _theme;

    private Theme(ColorMap colorMap, ParsedTheme theme, ParsedTheme include,
        Dictionary<string, string> guiColorDictionary)
    {
        _colorMap = colorMap;
        _theme = theme;
        _include = include;
        _guiColorDictionary = guiColorDictionary;
    }

    public static Theme CreateFromRawTheme(
        IRawTheme source,
        IRegistryOptions registryOptions)
    {
        var colorMap = new ColorMap();
        var guiColorsDictionary = new Dictionary<string, string>();

        var themeRuleList = ParsedTheme.ParseTheme(source, 0);

        var theme = ParsedTheme.CreateFromParsedTheme(
            themeRuleList,
            colorMap);

        var include = ParsedTheme.CreateFromParsedTheme(
            ParsedTheme.ParseInclude(source, registryOptions, 0, out var themeInclude),
            colorMap);

        // First get colors from include, then try and overwrite with local colors
        // I don't see this happening currently, but here just in case that ever happens.
        if (themeInclude != null)
            ParsedTheme.ParsedGuiColors(themeInclude, guiColorsDictionary);
        ParsedTheme.ParsedGuiColors(source, guiColorsDictionary);

        return new(colorMap, theme, include, guiColorsDictionary);
    }

    public List<ThemeTrieElementRule> Match(IList<string> scopeNames)
    {
        var result = new List<ThemeTrieElementRule>();

        for (var i = scopeNames.Count - 1; i >= 0; i--)
            result.AddRange(_theme.Match(scopeNames[i]));

        for (var i = scopeNames.Count - 1; i >= 0; i--)
            result.AddRange(_include.Match(scopeNames[i]));

        return result;
    }

    public ReadOnlyDictionary<string, string> GetGuiColorDictionary()
    {
        return new(_guiColorDictionary);
    }

    public ICollection<string> GetColorMap()
    {
        return _colorMap.GetColorMap();
    }

    public int GetColorId(string color)
    {
        return _colorMap.GetId(color);
    }

    public string? GetColor(int id)
    {
        return _colorMap.GetColor(id);
    }

    internal ThemeTrieElementRule GetDefaults()
    {
        return _theme.GetDefaults();
    }
}

internal class ParsedTheme
{
    private readonly Dictionary<string /* scopeName */, List<ThemeTrieElementRule>> _cachedMatchRoot = new();
    private readonly ThemeTrieElementRule _defaults;
    private readonly ThemeTrieElement _root;

    private ParsedTheme(ThemeTrieElementRule defaults, ThemeTrieElement root)
    {
        _root = root;
        _defaults = defaults;
    }

    internal static List<ParsedThemeRule> ParseTheme(IRawTheme source, int priority)
    {
        var result = new List<ParsedThemeRule>();

        // process theme rules in vscode-textmate format:
        // see https://github.com/microsoft/vscode-textmate/tree/main/test-cases/themes
        LookupThemeRules(source.GetSettings(), result);

        // process theme rules in vscode format
        // see https://github.com/microsoft/vscode/tree/main/extensions/theme-defaults/themes
        LookupThemeRules(source.GetTokenColors(), result);

        return result;
    }

    internal static void ParsedGuiColors(IRawTheme source, Dictionary<string, string> colorDictionary)
    {
        var colors = source.GetGuiColors();
        if (colors == null)
            return;
        foreach (var kvp in colors)
            colorDictionary[kvp.Key] = (string) kvp.Value;
    }

    internal static List<ParsedThemeRule> ParseInclude(
        IRawTheme source,
        IRegistryOptions registryOptions,
        int priority,
        out IRawTheme? themeInclude)
    {
        var result = new List<ParsedThemeRule>();

        var include = source.GetInclude();

        if (string.IsNullOrEmpty(include))
        {
            themeInclude = null;
            return result;
        }

        themeInclude = registryOptions.LoadRawTheme(include);

        if (themeInclude == null)
            return result;

        return ParseTheme(themeInclude, priority);
    }

    private static void LookupThemeRules(
        ICollection<IRawThemeSetting>? settings,
        List<ParsedThemeRule> parsedThemeRules)
    {
        if (settings == null)
            return;

        var i = 0;
        foreach (var entry in settings)
        {
            if (entry.GetSetting() == null)
                continue;

            var settingScope = entry.GetScope();
            var scopes = new List<string>();
            if (settingScope is string s)
            {
                // remove leading and trailing commas
                s = s.Trim(',');
                scopes = new(s.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }
            else if (settingScope is IList<object> list)
            {
                scopes = new(list.Cast<string>());
            }
            else
            {
                scopes.Add("");
            }

            var fontStyle = FontStyle.NotSet;
            var settingsFontStyle = entry.GetSetting()?.GetFontStyle();
            if (settingsFontStyle is string style)
            {
                fontStyle = FontStyle.None;

                var segments = style.Split(' ');
                foreach (var segment in segments)
                    switch (segment)
                    {
                        case "italic":
                            fontStyle |= FontStyle.Italic;
                            break;
                        case "bold":
                            fontStyle |= FontStyle.Bold;
                            break;
                        case "underline":
                            fontStyle |= FontStyle.Underline;
                            break;
                        case "strikethrough":
                            fontStyle |= FontStyle.Strikethrough;
                            break;
                    }
            }

            string? foreground = null;
            object? settingsForeground = entry.GetSetting()?.GetForeground();
            if (settingsForeground is string foreground1 && StringUtils.IsValidHexColor(foreground1))
                foreground = foreground1;

            string? background = null;
            object? settingsBackground = entry.GetSetting()?.GetBackground();
            if (settingsBackground is string background1 && StringUtils.IsValidHexColor(background1))
                background = background1;

            for (int j = 0, lenJ = scopes.Count; j < lenJ; j++)
            {
                var scopeStr = scopes[j].Trim();

                var segments = new List<string>(scopeStr.Split(' '));

                var scope = segments[^1];
                List<string>? parentScopes = null;
                if (segments.Count > 1)
                {
                    parentScopes = new(segments);
                    parentScopes.Reverse();
                }

                var name = entry.GetName();

                var t = new ParsedThemeRule(name, scope, parentScopes, i, fontStyle, foreground, background);
                parsedThemeRules.Add(t);
            }

            i++;
        }
    }

    public static ParsedTheme CreateFromParsedTheme(
        List<ParsedThemeRule> source,
        ColorMap colorMap)
    {
        return ResolveParsedThemeRules(source, colorMap);
    }

    /**
     * Resolve rules (i.e. inheritance).
     */
    private static ParsedTheme ResolveParsedThemeRules(
        List<ParsedThemeRule> parsedThemeRules,
        ColorMap colorMap)
    {
        // Sort rules lexicographically, and then by index if necessary
        parsedThemeRules.Sort((a, b) =>
        {
            var r = StringUtils.StrCmp(a.Scope, b.Scope);
            if (r != 0)
                return r;
            r = StringUtils.StrArrCmp(a.ParentScopes, b.ParentScopes);
            if (r != 0)
                return r;
            return a.Index.CompareTo(b.Index);
        });

        // Determine defaults
        var defaultFontStyle = FontStyle.None;
        var defaultForeground = "#000000";
        var defaultBackground = "#ffffff";

        while (parsedThemeRules.Count >= 1 && "".Equals(parsedThemeRules[0].Scope))
        {
            var incomingDefaults = parsedThemeRules[0];
            parsedThemeRules.RemoveAt(0); // shift();
            if (incomingDefaults.FontStyle != FontStyle.NotSet)
                defaultFontStyle = incomingDefaults.FontStyle;
            if (incomingDefaults.Foreground != null)
                defaultForeground = incomingDefaults.Foreground;
            if (incomingDefaults.Background != null)
                defaultBackground = incomingDefaults.Background;
        }

        var defaults = new ThemeTrieElementRule(string.Empty, 0, null, defaultFontStyle,
            colorMap.GetId(defaultForeground), colorMap.GetId(defaultBackground));

        var root = new ThemeTrieElement(new(string.Empty, 0, null, FontStyle.NotSet, 0, 0), []);

        foreach (var rule in parsedThemeRules)
            root.Insert(rule.Name, 0, rule.Scope, rule.ParentScopes, rule.FontStyle, colorMap.GetId(rule.Foreground),
                colorMap.GetId(rule.Background));

        return new(defaults, root);
    }

    internal List<ThemeTrieElementRule> Match(string scopeName)
    {
        if (_cachedMatchRoot.TryGetValue(scopeName, out var value))
            return value;

        value = _root.Match(scopeName);
        _cachedMatchRoot[scopeName] = value;

        return value;
    }

    internal ThemeTrieElementRule GetDefaults()
    {
        return _defaults;
    }
}

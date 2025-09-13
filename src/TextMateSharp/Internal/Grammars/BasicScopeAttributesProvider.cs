using System.Text.RegularExpressions;
using TextMateSharp.Internal.Utils;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars;

public class BasicScopeAttributesProvider
{
    private static readonly BasicScopeAttributes _NULL_SCOPE_METADATA = new(0, 0, null);

    private static readonly Regex STANDARD_TOKEN_TYPE_REGEXP = new("\\b(comment|string|regex|meta\\.embedded)\\b");
    private readonly Dictionary<string, BasicScopeAttributes> _cache = new();
    private readonly Dictionary<string, int> _embeddedLanguages;
    private readonly Regex? _embeddedLanguagesRegex;

    private readonly int _initialLanguage;
    private readonly IThemeProvider _themeProvider;
    private BasicScopeAttributes _defaultAttributes;

    public BasicScopeAttributesProvider(int initialLanguage, IThemeProvider themeProvider,
        Dictionary<string, int>? embeddedLanguages)
    {
        _initialLanguage = initialLanguage;
        _themeProvider = themeProvider;
        _defaultAttributes = new(
            _initialLanguage,
            StandardTokenType.NotSet,
            [_themeProvider.GetDefaults()]);

        // embeddedLanguages handling
        _embeddedLanguages = new();
        if (embeddedLanguages != null)
            // If embeddedLanguages are configured, fill in `this.embeddedLanguages`
            foreach (var scope in embeddedLanguages.Keys)
            {
                var languageId = embeddedLanguages[scope];
                _embeddedLanguages[scope] = languageId;
            }

        // create the regex
        var escapedScopes = _embeddedLanguages.Keys.Select(RegexSource.EscapeRegExpCharacters).ToList();

        if (escapedScopes.Count == 0)
        {
            // no scopes registered
            _embeddedLanguagesRegex = null;
        }
        else
        {
            // TODO: !!! reversedScopes?
            /*var reversedScopes = new List<string>(escapedScopes);
            reversedScopes.Sort();
            reversedScopes.Reverse();*/
            _embeddedLanguagesRegex = new(
                "^((" +
                string.Join(")|(", escapedScopes) +
                "))($|\\.)");
        }
    }

    public void OnDidChangeTheme()
    {
        _cache.Clear();
        _defaultAttributes = new(
            _initialLanguage,
            StandardTokenType.NotSet,
            [_themeProvider.GetDefaults()]);
    }

    public BasicScopeAttributes GetDefaultAttributes()
    {
        return _defaultAttributes;
    }

    public BasicScopeAttributes GetBasicScopeAttributes(string? scopeName)
    {
        if (scopeName == null)
            return _NULL_SCOPE_METADATA;
        _cache.TryGetValue(scopeName, out var value);
        if (value != null)
            return value;
        value = DoGetMetadataForScope(scopeName);
        _cache[scopeName] = value;
        return value;
    }

    private BasicScopeAttributes DoGetMetadataForScope(string scopeName)
    {
        var languageId = ScopeToLanguage(scopeName);
        var standardTokenType = ToStandardTokenType(scopeName);
        var themeData = _themeProvider.ThemeMatch([scopeName]);

        return new(languageId, standardTokenType, themeData);
    }

    private int ScopeToLanguage(string? scope)
    {
        if (scope == null)
            return 0;

        if (_embeddedLanguagesRegex == null)
            // no scopes registered
            return 0;

        var m = _embeddedLanguagesRegex.Match(scope);
        if (!m.Success)
            // no scopes matched
            return 0;

        var scopeName = m.Groups[1].Value;
        return _embeddedLanguages.GetValueOrDefault(scopeName);
    }

    private static int ToStandardTokenType(string tokenType)
    {
        var m = STANDARD_TOKEN_TYPE_REGEXP.Match(tokenType);

        if (!m.Success)
            return StandardTokenType.NotSet;

        var group = m.Value;

        switch (group)
        {
            case "comment": return StandardTokenType.Comment;
            case "string": return StandardTokenType.String;
            case "regex": return StandardTokenType.RegEx;
            case "meta.embedded": return StandardTokenType.Other;
            default: throw new TextMateException("Unexpected match for standard token type!");
        }
    }
}

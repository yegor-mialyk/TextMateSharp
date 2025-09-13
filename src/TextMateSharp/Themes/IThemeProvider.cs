namespace TextMateSharp.Themes;

public interface IThemeProvider
{
    List<ThemeTrieElementRule> ThemeMatch(IList<string> scopeNames);

    ThemeTrieElementRule GetDefaults();
}
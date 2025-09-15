using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars;

public class BasicScopeAttributes(
    int languageId,
    int tokenType,
    List<ThemeTrieElementRule>? themeData)
{
    public int LanguageId { get; } = languageId;

    public int TokenType { get; } = tokenType; /* StandardTokenType */

    public List<ThemeTrieElementRule>? ThemeData { get; } = themeData;
}

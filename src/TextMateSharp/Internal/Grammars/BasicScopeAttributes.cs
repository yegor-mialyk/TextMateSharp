using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars;

public class BasicScopeAttributes
{
    public BasicScopeAttributes(
        int languageId,
        int tokenType,
        List<ThemeTrieElementRule>? themeData)
    {
        LanguageId = languageId;
        TokenType = tokenType;
        ThemeData = themeData;
    }

    public int LanguageId { get; }

    public int TokenType { get; } /* StandardTokenType */

    public List<ThemeTrieElementRule>? ThemeData { get; }
}

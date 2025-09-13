using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars;

public static class EncodedTokenAttributes
{
    public static string ToBinaryStr(int metadata)
    {
        var builder = new List<char>(Convert.ToString((uint) metadata, 2));

        while (builder.Count < 32)
            builder.Insert(0, '0');

        return new(builder.ToArray());
    }

    public static int GetLanguageId(int metadata)
    {
        var uintValue = (uint) metadata;
        return (int) ((uintValue & MetadataConsts.LANGUAGEID_MASK) >> MetadataConsts.LANGUAGEID_OFFSET);
    }

    public static int GetTokenType(int metadata)
    {
        var uintValue = (uint) metadata;
        return (int) ((uintValue & MetadataConsts.TOKEN_TYPE_MASK) >> MetadataConsts.TOKEN_TYPE_OFFSET);
    }

    public static bool ContainsBalancedBrackets(int metadata)
    {
        var uintValue = (uint) metadata;
        return (uintValue & MetadataConsts.BALANCED_BRACKETS_MASK) != 0;
    }

    public static FontStyle GetFontStyle(int metadata)
    {
        var uintValue = (uint) metadata;
        return (FontStyle) ((uintValue & MetadataConsts.FONT_STYLE_MASK) >> MetadataConsts.FONT_STYLE_OFFSET);
    }

    public static int GetForeground(int metadata)
    {
        var uintValue = (uint) metadata;
        return (int) ((uintValue & MetadataConsts.FOREGROUND_MASK) >> MetadataConsts.FOREGROUND_OFFSET);
    }

    public static int GetBackground(int metadata)
    {
        var unitValue = (ulong) metadata;
        return (int) ((unitValue & MetadataConsts.BACKGROUND_MASK) >> MetadataConsts.BACKGROUND_OFFSET);
    }

    public static int Set(
        int metadata,
        int languageId,
        /*StandardTokenType*/ int tokenType,
        bool? containsBalancedBrackets,
        FontStyle fontStyle,
        int foreground,
        int background)
    {
        languageId = languageId == 0 ? GetLanguageId(metadata) : languageId;
        tokenType = tokenType == StandardTokenType.NotSet ? GetTokenType(metadata) : tokenType;
        var containsBalancedBracketsBit = (containsBalancedBrackets == null
            ? ContainsBalancedBrackets(metadata)
            : containsBalancedBrackets.Value)
            ? 1
            : 0;
        fontStyle = fontStyle == FontStyle.NotSet ? GetFontStyle(metadata) : fontStyle;
        foreground = foreground == 0 ? GetForeground(metadata) : foreground;
        background = background == 0 ? GetBackground(metadata) : background;

        return ((languageId << MetadataConsts.LANGUAGEID_OFFSET) |
                (tokenType << MetadataConsts.TOKEN_TYPE_OFFSET) |
                (containsBalancedBracketsBit << MetadataConsts.BALANCED_BRACKETS_OFFSET) |
                ((int) fontStyle << MetadataConsts.FONT_STYLE_OFFSET) |
                (foreground << MetadataConsts.FOREGROUND_OFFSET) |
                (background << MetadataConsts.BACKGROUND_OFFSET)) >>
            0;
    }
}

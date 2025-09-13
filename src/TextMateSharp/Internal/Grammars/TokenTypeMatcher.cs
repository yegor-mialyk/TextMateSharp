namespace TextMateSharp.Internal.Grammars;

internal class TokenTypeMatcher
{
    public TokenTypeMatcher(int type, Predicate<List<string>> matcher)
    {
        Type = type;
        Matcher = matcher;
    }

    public int Type { get; }
    public Predicate<List<string>> Matcher { get; }
}
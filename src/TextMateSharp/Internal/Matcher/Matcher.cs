namespace TextMateSharp.Internal.Matcher;

public static class Matcher
{
    public static ICollection<MatcherWithPriority<List<string>>> CreateMatchers(string selector)
    {
        return new MatcherBuilder<List<string>>(selector, NameMatcher.Default).Results;
    }
}

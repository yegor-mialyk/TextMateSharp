namespace TextMateSharp.Internal.Matcher;

internal class Matcher
{
    internal static ICollection<MatcherWithPriority<List<string>>> CreateMatchers(string selector)
    {
        return new MatcherBuilder<List<string>>(selector, NameMatcher.Default).Results;
    }
}

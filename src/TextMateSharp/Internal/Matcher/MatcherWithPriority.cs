namespace TextMateSharp.Internal.Matcher;

public class MatcherWithPriority<T>(Predicate<T> matcher, int priority)
{
    public Predicate<T> Matcher { get; private set; } = matcher;

    public int Priority { get; private set; } = priority;
}

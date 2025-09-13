namespace TextMateSharp.Internal.Matcher;

public class MatcherWithPriority<T>
{
    public MatcherWithPriority(Predicate<T> matcher, int priority)
    {
        Matcher = matcher;
        Priority = priority;
    }

    public Predicate<T> Matcher { get; private set; }
    public int Priority { get; private set; }
}
namespace TextMateSharp.Internal.Rules;

public class CompilePatternsResult
{
    public CompilePatternsResult(IList<int> patterns, bool hasMissingPatterns)
    {
        HasMissingPatterns = hasMissingPatterns;
        Patterns = patterns;
    }

    public IList<int> Patterns { get; private set; }
    public bool HasMissingPatterns { get; private set; }
}

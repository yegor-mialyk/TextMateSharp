namespace TextMateSharp.Internal.Rules;

public class IncludeOnlyRule : Rule
{
    private RegExpSourceList? _cachedCompiledPatterns;

    public IncludeOnlyRule(int id, string? name, string? contentName, CompilePatternsResult patterns) : base(id, name,
        contentName)
    {
        Patterns = patterns.Patterns;
        HasMissingPatterns = patterns.HasMissingPatterns;

        _cachedCompiledPatterns = null;
    }

    public bool HasMissingPatterns { get; private set; }
    public IList<int> Patterns { get; }

    public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst)
    {
        foreach (var pattern in Patterns)
        {
            var rule = grammar.GetRule(pattern);
            rule.CollectPatternsRecursive(grammar, sourceList, false);
        }
    }

    public override CompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
    {
        if (_cachedCompiledPatterns == null)
        {
            _cachedCompiledPatterns = new();
            CollectPatternsRecursive(grammar, _cachedCompiledPatterns, true);
        }

        return _cachedCompiledPatterns.Compile(allowA, allowG);
    }
}

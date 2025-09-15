namespace TextMateSharp.Internal.Rules;

public class MatchRule : Rule
{
    private readonly RegExpSource _match;
    private RegExpSourceList? _cachedCompiledPatterns;

    public MatchRule(int id, string? name, string? match, List<CaptureRule> captures) : base(id, name, null)
    {
        _match = new(match, Id);
        Captures = captures;
    }

    public List<CaptureRule> Captures { get; }

    public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst)
    {
        sourceList.Push(_match);
    }

    public override CompiledRule? Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
    {
        if (_cachedCompiledPatterns == null)
        {
            _cachedCompiledPatterns = new();
            CollectPatternsRecursive(grammar, _cachedCompiledPatterns, true);
        }

        return _cachedCompiledPatterns.Compile(allowA, allowG);
    }
}

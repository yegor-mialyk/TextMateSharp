using Onigwrap;

namespace TextMateSharp.Internal.Rules;

public class BeginEndRule : Rule
{
    private readonly RegExpSource _begin;
    private readonly RegExpSource _end;
    private RegExpSourceList _cachedCompiledPatterns;

    public BeginEndRule(RuleId id, string name, string contentName, string begin, List<CaptureRule> beginCaptures,
        string end, List<CaptureRule> endCaptures, bool applyEndPatternLast, CompilePatternsResult patterns)
        : base(id, name, contentName)
    {
        _begin = new(begin, Id);
        _end = new(end, RuleId.END_RULE);

        BeginCaptures = beginCaptures;
        EndHasBackReferences = _end.HasBackReferences();
        EndCaptures = endCaptures;
        ApplyEndPatternLast = applyEndPatternLast;
        Patterns = patterns.Patterns;
        HasMissingPatterns = patterns.HasMissingPatterns;

        _cachedCompiledPatterns = null;
    }

    public List<CaptureRule> BeginCaptures { get; private set; }
    public bool EndHasBackReferences { get; private set; }
    public List<CaptureRule> EndCaptures { get; private set; }
    public bool ApplyEndPatternLast { get; }
    public bool HasMissingPatterns { get; private set; }
    public IList<RuleId> Patterns { get; }

    public string GetEndWithResolvedBackReferences(string lineText, IOnigCaptureIndex[] captureIndices)
    {
        return _end.ResolveBackReferences(lineText, captureIndices);
    }

    public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst)
    {
        if (isFirst)
            foreach (var pattern in Patterns)
            {
                var rule = grammar.GetRule(pattern);
                rule.CollectPatternsRecursive(grammar, sourceList, false);
            }
        else
            sourceList.Push(_begin);
    }

    public override CompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
    {
        var precompiled = Precompile(grammar);
        if (_end.HasBackReferences())
        {
            if (ApplyEndPatternLast)
                precompiled.SetSource(precompiled.Length() - 1, endRegexSource);
            else
                precompiled.SetSource(0, endRegexSource);
        }

        return _cachedCompiledPatterns.Compile(allowA, allowG);
    }

    private RegExpSourceList Precompile(IRuleRegistry grammar)
    {
        if (_cachedCompiledPatterns == null)
        {
            _cachedCompiledPatterns = new();

            CollectPatternsRecursive(grammar, _cachedCompiledPatterns, true);

            if (ApplyEndPatternLast)
                _cachedCompiledPatterns.Push(_end.HasBackReferences() ? _end.Clone() : _end);
            else
                _cachedCompiledPatterns.UnShift(_end.HasBackReferences() ? _end.Clone() : _end);
        }

        return _cachedCompiledPatterns;
    }
}
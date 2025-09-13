using Onigwrap;

namespace TextMateSharp.Internal.Rules;

public class BeginWhileRule : Rule
{
    private readonly RegExpSource _begin;
    private readonly RegExpSource _while;
    private RegExpSourceList _cachedCompiledPatterns;
    private RegExpSourceList _cachedCompiledWhilePatterns;

    public BeginWhileRule(RuleId id, string name, string contentName, string begin,
        List<CaptureRule> beginCaptures, string whileStr, List<CaptureRule> whileCaptures,
        CompilePatternsResult patterns) : base(id, name, contentName)
    {
        _begin = new(begin, Id);
        _while = new(whileStr, RuleId.WHILE_RULE);

        BeginCaptures = beginCaptures;
        WhileCaptures = whileCaptures;
        WhileHasBackReferences = _while.HasBackReferences();
        Patterns = patterns.Patterns;
        HasMissingPatterns = patterns.HasMissingPatterns;

        _cachedCompiledPatterns = null;
        _cachedCompiledWhilePatterns = null;
    }

    public List<CaptureRule> BeginCaptures { get; private set; }
    public List<CaptureRule> WhileCaptures { get; private set; }
    public bool WhileHasBackReferences { get; private set; }
    public bool HasMissingPatterns { get; private set; }
    public IList<RuleId> Patterns { get; }

    public string getWhileWithResolvedBackReferences(string lineText, IOnigCaptureIndex[] captureIndices)
    {
        return _while.ResolveBackReferences(lineText, captureIndices);
    }

    public override void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst)
    {
        if (isFirst)
        {
            Rule rule;
            foreach (var pattern in Patterns)
            {
                rule = grammar.GetRule(pattern);
                rule.CollectPatternsRecursive(grammar, sourceList, false);
            }
        }
        else
        {
            sourceList.Push(_begin);
        }
    }

    public override CompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG)
    {
        Precompile(grammar);
        return _cachedCompiledPatterns.Compile(allowA, allowG);
    }

    private void Precompile(IRuleRegistry grammar)
    {
        if (_cachedCompiledPatterns == null)
        {
            _cachedCompiledPatterns = new();
            CollectPatternsRecursive(grammar, _cachedCompiledPatterns, true);
        }
    }

    public CompiledRule CompileWhile(string endRegexSource, bool allowA, bool allowG)
    {
        PrecompileWhile();
        if (_while.HasBackReferences())
            _cachedCompiledWhilePatterns.SetSource(0, endRegexSource);
        return _cachedCompiledWhilePatterns.Compile(allowA, allowG);
    }

    private void PrecompileWhile()
    {
        if (_cachedCompiledWhilePatterns == null)
        {
            _cachedCompiledWhilePatterns = new();
            _cachedCompiledWhilePatterns.Push(_while.HasBackReferences() ? _while.Clone() : _while);
        }
    }
}
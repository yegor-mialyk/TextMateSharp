using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Types;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars;

public class Grammar : IGrammar, IRuleFactoryHelper
{
    private readonly BasicScopeAttributesProvider _basicScopeAttributesProvider;
    private readonly IGrammarRepository _grammarRepository;
    private readonly Dictionary<string, IRawGrammar> _includedGrammars = new();
    private readonly IRawGrammar _rawGrammar;
    private readonly string _rootScopeName;
    private readonly Dictionary<int, Rule> _ruleId2desc = new();
    private List<Injection>? _injections;
    private volatile bool _isCompiling;
    private int _lastRuleId;
    private int _rootId = Rule.NO_INIT;

    public Grammar(
        string scopeName,
        IRawGrammar grammar,
        int initialLanguage,
        Dictionary<string, int>? embeddedLanguages,
        IGrammarRepository grammarRepository,
        IThemeProvider themeProvider)
    {
        _rootScopeName = scopeName;
        _basicScopeAttributesProvider = new(initialLanguage, themeProvider, embeddedLanguages);
        _grammarRepository = grammarRepository;
        _rawGrammar = InitGrammar(grammar, null);
    }

    public bool IsCompiling => _isCompiling;

    public string? GetName()
    {
        return _rawGrammar.GetName();
    }

    public string GetScopeName()
    {
        return _rootScopeName;
    }

    public ICollection<string> GetFileTypes()
    {
        return _rawGrammar.GetFileTypes();
    }

    public Rule RegisterRule(Func<int, Rule> factory)
    {
        var id = ++_lastRuleId;
        var result = factory(id);
        _ruleId2desc[id] = result;
        return result;
    }

    public Rule? GetRule(int patternId)
    {
        _ruleId2desc.TryGetValue(patternId, out var result);
        return result;
    }

    public IRawGrammar? GetExternalGrammar(string scopeName, IRawRepository? repository)
    {
        if (_includedGrammars.TryGetValue(scopeName, out var value))
            return value;

        var rawIncludedGrammar = _grammarRepository.Lookup(scopeName);

        if (rawIncludedGrammar == null)
            return null;

        _includedGrammars[scopeName] =
            InitGrammar(rawIncludedGrammar, repository?.GetBase());

        return _includedGrammars[scopeName];
    }

    public void OnDidChangeTheme()
    {
        _basicScopeAttributesProvider.OnDidChangeTheme();
    }

    public BasicScopeAttributes GetMetadataForScope(string scope)
    {
        return _basicScopeAttributesProvider.GetBasicScopeAttributes(scope);
    }

    public List<Injection> GetInjections()
    {
        if (_injections != null)
            return _injections;

        _injections = [];

        var grammar = _rootScopeName.Equals(_rootScopeName) ? _rawGrammar : GetExternalGrammar(_rootScopeName, null);

        // add injections from the current grammar
        var rawInjections = grammar?.GetInjections();
        if (rawInjections != null)
            foreach (var expression in rawInjections.Keys)
            {
                var rule = rawInjections[expression];
                CollectInjections(_injections, expression, rule, this, grammar);
            }

        // add injection grammars contributed for the current scope
        var injectionScopeNames = _grammarRepository.GetInjections(_rootScopeName);

        if (injectionScopeNames != null)
            foreach (var injectionScopeName in injectionScopeNames)
            {
                var injectionGrammar = GetExternalGrammar(injectionScopeName, null);
                var selector = injectionGrammar?.GetInjectionSelector();
                if (selector != null)
                    CollectInjections(
                        _injections,
                        selector,
                        (IRawRule) injectionGrammar,
                        this,
                        injectionGrammar);
            }

        // sort by priority
        _injections.Sort((i1, i2) => i1.Priority - i2.Priority);

        return _injections;
    }

    private static void CollectInjections(List<Injection> result, string selector, IRawRule? rule,
        IRuleFactoryHelper ruleFactoryHelper, IRawGrammar grammar)
    {
        var matchers = Matcher.Matcher.CreateMatchers(selector);
        var ruleId = RuleFactory.GetCompiledRuleId(rule, ruleFactoryHelper, grammar.GetRepository());

        foreach (var matcher in matchers)
            result.Add(new(
                matcher.Matcher,
                ruleId,
                grammar,
                matcher.Priority));
    }

    private static IRawGrammar InitGrammar(IRawGrammar grammar, IRawRule? ruleBase)
    {
        grammar = grammar.Clone();
        if (grammar.GetRepository() == null)
            ((GrammarRaw) grammar).SetRepository(new GrammarRaw());

        var self = new GrammarRaw();
        self.SetPatterns(grammar.GetPatterns());
        self.SetName(grammar.GetScopeName());

        grammar.GetRepository()?.SetSelf(self);
        grammar.GetRepository()?.SetBase(ruleBase ?? grammar.GetRepository()?.GetSelf());

        return grammar;
    }

    public ITokenizeLineResult? TokenizeLine(string lineText, StateStack? prevState, TimeSpan timeLimit)
    {
        if (_rootId == Rule.NO_INIT)
            GenerateRootId();

        bool isFirstLine;

        if (prevState == null || prevState.Equals(StateStack.NULL))
        {
            isFirstLine = true;

            var rawDefaultMetadata = _basicScopeAttributesProvider.GetDefaultAttributes();
            var defaultTheme = rawDefaultMetadata.ThemeData[0];
            var defaultMetadata = EncodedTokenAttributes.Set(0, rawDefaultMetadata.LanguageId,
                rawDefaultMetadata.TokenType, null, defaultTheme.fontStyle, defaultTheme.foreground,
                defaultTheme.background);

            var rootScopeName = GetRule(_rootId)?.GetName(null, null);
            if (rootScopeName == null)
                return null;

            var rawRootMetadata = _basicScopeAttributesProvider.GetBasicScopeAttributes(rootScopeName);
            var rootMetadata = AttributedScopeStack.MergeAttributes(defaultMetadata, null, rawRootMetadata);

            var scopeList = new AttributedScopeStack(null, rootScopeName, rootMetadata);

            prevState = new(null, _rootId, -1, -1, false, null, scopeList, scopeList);
        }
        else
        {
            isFirstLine = false;
            prevState.Reset();
        }

        if (string.IsNullOrEmpty(lineText) || lineText[^1] != '\n')
            // Only add \n if the passed lineText didn't have it.
            lineText += '\n';

        var lineLength = lineText.Length;

        var lineTokens = new LineTokens();

        var tokenizeResult = LineTokenizer.TokenizeString(this, lineText, isFirstLine, 0, prevState,
            lineTokens, true, timeLimit);

        return new TokenizeLineResult(lineTokens.GetResult(tokenizeResult.Stack, lineLength),
            tokenizeResult.Stack, tokenizeResult.StoppedEarly);
    }

    private void GenerateRootId()
    {
        _isCompiling = true;

        try
        {
            _rootId = RuleFactory.GetCompiledRuleId(_rawGrammar.GetRepository()?.GetSelf(), this,
                _rawGrammar.GetRepository());
        }
        finally
        {
            _isCompiling = false;
        }
    }
}

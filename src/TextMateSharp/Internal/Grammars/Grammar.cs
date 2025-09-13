using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Types;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars;

public class Grammar : IGrammar, IRuleFactoryHelper
{
    private readonly BalancedBracketSelectors _balancedBracketSelectors;
    private readonly BasicScopeAttributesProvider _basicScopeAttributesProvider;
    private readonly IGrammarRepository _grammarRepository;
    private readonly Dictionary<string, IRawGrammar> _includedGrammars;
    private readonly IRawGrammar _rawGrammar;
    private readonly string _rootScopeName;
    private readonly Dictionary<RuleId, Rule> _ruleId2desc;
    private readonly List<TokenTypeMatcher> _tokenTypeMatchers;
    private List<Injection> _injections;
    private volatile bool _isCompiling;
    private int _lastRuleId;
    private RuleId _rootId;

    public Grammar(
        string scopeName,
        IRawGrammar grammar,
        int initialLanguage,
        Dictionary<string, int>? embeddedLanguages,
        Dictionary<string, int>? tokenTypes,
        BalancedBracketSelectors? balancedBracketSelectors,
        IGrammarRepository grammarRepository,
        IThemeProvider themeProvider)
    {
        _rootScopeName = scopeName;
        _basicScopeAttributesProvider = new(initialLanguage, themeProvider, embeddedLanguages);
        _balancedBracketSelectors = balancedBracketSelectors;
        _rootId = null;
        _lastRuleId = 0;
        _includedGrammars = new();
        _grammarRepository = grammarRepository;
        _rawGrammar = InitGrammar(grammar, null);
        _ruleId2desc = new();
        _injections = null;
        _tokenTypeMatchers = GenerateTokenTypeMatchers(tokenTypes);
    }

    public bool IsCompiling => _isCompiling;

    public ITokenizeLineResult TokenizeLine(string lineText)
    {
        return TokenizeLine(lineText, null, TimeSpan.MaxValue);
    }

    public ITokenizeLineResult TokenizeLine(string lineText, IStateStack prevState, TimeSpan timeLimit)
    {
        return (ITokenizeLineResult) Tokenize(lineText, (StateStack) prevState, false, timeLimit);
    }

    public ITokenizeLineResult2 TokenizeLine2(string lineText)
    {
        return TokenizeLine2(lineText, null, TimeSpan.MaxValue);
    }

    public ITokenizeLineResult2 TokenizeLine2(string lineText, IStateStack prevState, TimeSpan timeLimit)
    {
        return (ITokenizeLineResult2) Tokenize(lineText, (StateStack) prevState, true, timeLimit);
    }

    public string GetName()
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

    public Rule RegisterRule(Func<RuleId, Rule> factory)
    {
        var id = RuleId.Of(++_lastRuleId);
        var result = factory(id);
        _ruleId2desc[id] = result;
        return result;
    }

    public Rule GetRule(RuleId patternId)
    {
        Rule result;
        _ruleId2desc.TryGetValue(patternId, out result);
        return result;
    }

    public IRawGrammar GetExternalGrammar(string scopeName, IRawRepository repository)
    {
        if (_includedGrammars.TryGetValue(scopeName, out var value))
            return value;

        if (_grammarRepository != null)
        {
            var rawIncludedGrammar = _grammarRepository.Lookup(scopeName);
            if (rawIncludedGrammar != null)
            {
                _includedGrammars[scopeName] =
                    InitGrammar(rawIncludedGrammar, repository != null ? repository.GetBase() : null);
                return _includedGrammars[scopeName];
            }
        }

        return null;
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
        if (_injections == null)
        {
            _injections = new();

            var grammarRepository = new GrammarRepository(this);
            var scopeName = _rootScopeName;
            var grammar = grammarRepository.Lookup(scopeName);

            if (grammar != null)
            {
                // add injections from the current grammar
                var rawInjections = grammar.GetInjections();
                if (rawInjections != null)
                    foreach (var expression in rawInjections.Keys)
                    {
                        var rule = rawInjections[expression];
                        CollectInjections(_injections, expression, rule, this, grammar);
                    }
            }

            // add injection grammars contributed for the current scope
            var injectionScopeNames = _grammarRepository.Injections(scopeName);

            if (injectionScopeNames != null)
                foreach (var injectionScopeName in injectionScopeNames)
                {
                    var injectionGrammar = GetExternalGrammar(injectionScopeName);
                    if (injectionGrammar != null)
                    {
                        var selector = injectionGrammar.GetInjectionSelector();
                        if (selector != null)
                            CollectInjections(
                                _injections,
                                selector,
                                (IRawRule) injectionGrammar,
                                this,
                                injectionGrammar);
                    }
                }

            // sort by priority
            _injections.Sort((i1, i2) => { return i1.Priority - i2.Priority; });
        }

        return _injections;
    }

    private void CollectInjections(List<Injection> result, string selector, IRawRule rule,
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

    public IRawGrammar GetExternalGrammar(string scopeName)
    {
        return GetExternalGrammar(scopeName, null);
    }

    private IRawGrammar InitGrammar(IRawGrammar grammar, IRawRule ruleBase)
    {
        grammar = grammar.Clone();
        if (grammar.GetRepository() == null)
            ((Raw) grammar).SetRepository(new Raw());
        var self = new Raw();
        self.SetPatterns(grammar.GetPatterns());
        self.SetName(grammar.GetScopeName());
        grammar.GetRepository().SetSelf(self);
        if (ruleBase != null)
            grammar.GetRepository().SetBase(ruleBase);
        else
            grammar.GetRepository().SetBase(grammar.GetRepository().GetSelf());
        return grammar;
    }

    private IRawGrammar Clone(IRawGrammar grammar)
    {
        return (IRawGrammar) ((Raw) grammar).Clone();
    }

    private object Tokenize(string lineText, StateStack prevState, bool emitBinaryTokens, TimeSpan timeLimit)
    {
        if (_rootId == null)
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

        if (string.IsNullOrEmpty(lineText) || lineText[lineText.Length - 1] != '\n')
            // Only add \n if the passed lineText didn't have it.
            lineText += '\n';
        var lineLength = lineText.Length;
        var lineTokens = new LineTokens(emitBinaryTokens, lineText, _tokenTypeMatchers, _balancedBracketSelectors);
        var tokenizeResult = LineTokenizer.TokenizeString(this, lineText, isFirstLine, 0, prevState,
            lineTokens, true, timeLimit);

        if (emitBinaryTokens)
            return new TokenizeLineResult2(lineTokens.GetBinaryResult(tokenizeResult.Stack, lineLength),
                tokenizeResult.Stack, tokenizeResult.StoppedEarly);
        return new TokenizeLineResult(lineTokens.GetResult(tokenizeResult.Stack, lineLength),
            tokenizeResult.Stack, tokenizeResult.StoppedEarly);
    }

    private void GenerateRootId()
    {
        _isCompiling = true;
        try
        {
            _rootId = RuleFactory.GetCompiledRuleId(_rawGrammar.GetRepository().GetSelf(), this,
                _rawGrammar.GetRepository());
        }
        finally
        {
            _isCompiling = false;
        }
    }

    private List<TokenTypeMatcher> GenerateTokenTypeMatchers(Dictionary<string, int> tokenTypes)
    {
        var result = new List<TokenTypeMatcher>();

        if (tokenTypes == null)
            return result;

        foreach (var selector in tokenTypes.Keys)
        foreach (var matcher in Matcher.Matcher.CreateMatchers(selector))
            result.Add(new(tokenTypes[selector], matcher.Matcher));

        return result;
    }

    private class GrammarRepository : IGrammarRepository
    {
        private readonly Grammar _grammar;

        internal GrammarRepository(Grammar grammar)
        {
            _grammar = grammar;
        }

        public IRawGrammar Lookup(string scopeName)
        {
            if (scopeName.Equals(_grammar._rootScopeName))
                return _grammar._rawGrammar;

            return _grammar.GetExternalGrammar(scopeName, null);
        }

        public ICollection<string> Injections(string targetScope)
        {
            return _grammar._grammarRepository.Injections(targetScope);
        }
    }
}
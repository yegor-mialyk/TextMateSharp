using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Internal.Types;
using TextMateSharp.Internal.Utils;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars;

public class GrammarRepository : IGrammarRepository, IThemeProvider
{
    private readonly Dictionary<string, IGrammar> _grammars = new();
    private readonly Dictionary<string, ICollection<string>> _injectionGrammars = new();
    private readonly Dictionary<string, IRawGrammar> _rawGrammars = new();
    private readonly IRegistryOptions _locator;

    private Theme _theme;

    public GrammarRepository(IRawTheme rawTheme, IRegistryOptions locator)
    {
        _locator = locator;
        _theme = Theme.CreateFromRawTheme(rawTheme, locator);
    }

    public IRawGrammar? Lookup(string scopeName)
    {
        _rawGrammars.TryGetValue(scopeName, out var result);
        return result;
    }

    public ICollection<string>? Injections(string targetScope)
    {
        _injectionGrammars.TryGetValue(targetScope, out var result);
        return result;
    }

    public ThemeTrieElementRule GetDefaults()
    {
        return _theme.GetDefaults();
    }

    public List<ThemeTrieElementRule> ThemeMatch(IList<string> scopeNames)
    {
        return _theme.Match(scopeNames);
    }

    public Theme GetTheme()
    {
        return _theme;
    }

    public void SetTheme(Theme theme)
    {
        _theme = theme;

        foreach (var grammar in _grammars.Values)
            ((Grammar) grammar).OnDidChangeTheme();
    }

    public ICollection<string> GetColorMap()
    {
        return _theme.GetColorMap();
    }

    public IGrammar? LoadGrammar(string initialScopeName)
    {
        if (string.IsNullOrEmpty(initialScopeName))
            return null;

        if (_grammars.TryGetValue(initialScopeName, out var value))
            return value;

        var remainingScopeNames = new Queue<string>();
        remainingScopeNames.Enqueue(initialScopeName);

        var seenScopeNames = new HashSet<string> { initialScopeName };

        while (remainingScopeNames.Count != 0)
        {
            var scopeName = remainingScopeNames.Dequeue();

            if (Lookup(scopeName) != null)
                continue;

            try
            {
                var loadedRawGrammar = _locator.LoadRawGrammar(scopeName);

                if (loadedRawGrammar == null)
                    continue;

                var injections = _locator.GetInjections(scopeName);

                var scopes = AddGrammar(loadedRawGrammar, injections);

                foreach (var scope in scopes)
                    if (seenScopeNames.Add(scope))
                        remainingScopeNames.Enqueue(scope);
            }
            catch (Exception e)
            {
                if (scopeName.Equals(initialScopeName))
                    throw new TextMateException("Unknown location for grammar <" + initialScopeName + ">", e);
            }
        }

        var rawGrammar = Lookup(initialScopeName);
        if (rawGrammar is null)
            return null;

        value = new Grammar(
            initialScopeName,
            rawGrammar,
            0,
            null,
            null,
            null,
            this,
            this);

        _grammars.Add(initialScopeName, value);

        return value;
    }

    public ICollection<string> AddGrammar(IRawGrammar grammar, ICollection<string>? injectionScopeNames)
    {
        _rawGrammars.Add(grammar.GetScopeName(), grammar);

        var includedScopes = new HashSet<string>();

        CollectIncludedScopes(includedScopes, grammar);

        if (injectionScopeNames is null)
            return includedScopes;

        _injectionGrammars.Add(grammar.GetScopeName(), injectionScopeNames);

        foreach (var scopeName in injectionScopeNames)
            includedScopes.Add(scopeName);

        return includedScopes;
    }

    private static void CollectIncludedScopes(ISet<string> result, IRawGrammar grammar)
    {
        var patterns = grammar.GetPatterns();
        if (patterns != null /* && Array.isArray(grammar.patterns) */)
            ExtractIncludedScopesInPatterns(result, patterns);

        var repository = grammar.GetRepository();
        if (repository != null)
            ExtractIncludedScopesInRepository(result, repository);

        // remove references to own scope (avoid recursion)
        result.Remove(grammar.GetScopeName());
    }

    private static void ExtractIncludedScopesInPatterns(ISet<string> result, ICollection<IRawRule> patterns)
    {
        foreach (var pattern in patterns)
        {
            var p = pattern.GetPatterns();
            if (p != null)
                ExtractIncludedScopesInPatterns(result, p);

            var include = pattern.GetInclude();
            if (include == null)
                continue;

            if (include.Equals("$base") || include.Equals("$self"))
                // Special includes that can be resolved locally in this grammar
                continue;

            if (include[0] == '#')
                // Local include from this grammar
                continue;

            var sharpIndex = include.IndexOf('#');
            var s = sharpIndex >= 0 ? include.SubstringAtIndexes(0, sharpIndex) : include;
            result.Add(s);
        }
    }

    private static void ExtractIncludedScopesInRepository(
        ISet<string> result,
        IRawRepository repository)
    {
        if (repository is not GrammarRaw rawRepository)
            return;

        foreach (var key in rawRepository.Keys)
        {
            var rule = (IRawRule) rawRepository[key];

            var patterns = rule.GetPatterns();
            var repositoryRule = rule.GetRepository();

            if (patterns != null)
                ExtractIncludedScopesInPatterns(result, patterns);

            if (repositoryRule != null)
                ExtractIncludedScopesInRepository(result, repositoryRule);
        }
    }
}

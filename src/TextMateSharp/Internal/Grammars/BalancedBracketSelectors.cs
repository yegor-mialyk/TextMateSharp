namespace TextMateSharp.Internal.Grammars;

public class BalancedBracketSelectors
{
    private readonly Predicate<List<string>>[] _balancedBracketScopes;
    private readonly Predicate<List<string>>[] _unbalancedBracketScopes;

    private bool _allowAny;

    public BalancedBracketSelectors(
        List<string> balancedBracketScopes,
        List<string> unbalancedBracketScopes)
    {
        _balancedBracketScopes = CreateBalancedBracketScopes(balancedBracketScopes);
        _unbalancedBracketScopes = CreateUnbalancedBracketScopes(unbalancedBracketScopes);
    }

    public bool MatchesAlways()
    {
        return _allowAny && _unbalancedBracketScopes.Length == 0;
    }

    public bool MatchesNever()
    {
        return !_allowAny && _balancedBracketScopes.Length == 0;
    }

    public bool Match(List<string> scopes)
    {
        foreach (var excluder in _unbalancedBracketScopes)
            if (excluder.Invoke(scopes))
                return false;

        foreach (var includer in _balancedBracketScopes)
            if (includer.Invoke(scopes))
                return true;

        return _allowAny;
    }

    private Predicate<List<string>>[] CreateBalancedBracketScopes(List<string> balancedBracketScopes)
    {
        var result = new List<Predicate<List<string>>>();

        foreach (var selector in balancedBracketScopes)
        {
            if ("*".Equals(selector))
            {
                _allowAny = true;
                return new Predicate<List<string>>[0];
            }

            var matcher = Matcher.Matcher.CreateMatchers(selector);

            foreach (var matches in matcher)
                result.Add(matches.Matcher);
        }

        return result.ToArray();
    }

    private Predicate<List<string>>[] CreateUnbalancedBracketScopes(List<string> unbalancedBracketScopes)
    {
        var result = new List<Predicate<List<string>>>();

        foreach (var selector in unbalancedBracketScopes)
        {
            var matcher = Matcher.Matcher.CreateMatchers(selector);

            foreach (var matches in matcher)
                result.Add(matches.Matcher);
        }

        return result.ToArray();
    }
}
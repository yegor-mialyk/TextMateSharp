using TextMateSharp.Grammars;

namespace TextMateSharp.Model;

public class Tokenizer : ITokenizationSupport
{
    private readonly DecodeMap _decodeMap;
    private readonly IGrammar _grammar;

    public Tokenizer(IGrammar grammar)
    {
        _grammar = grammar;
        _decodeMap = new();
    }

    public TMState GetInitialState()
    {
        return new(null, null);
    }

    public LineTokens Tokenize(string line, TMState state, TimeSpan timeLimit)
    {
        return Tokenize(line, state, 0, 0, timeLimit);
    }

    public LineTokens Tokenize(string line, TMState state, int offsetDelta, int maxLen, TimeSpan timeLimit)
    {
        if (_grammar == null)
            return null;

        var freshState = state != null ? state.Clone() : GetInitialState();

        if (line.Length > 0 && line.Length > maxLen)
            line = line.Substring(0, maxLen);

        var textMateResult = _grammar.TokenizeLine(line, freshState.GetRuleStack(), timeLimit);
        freshState.SetRuleStack(textMateResult.RuleStack);

        // Create the result early and fill in the tokens later
        var tokens = new List<TMToken>();
        string lastTokenType = null;
        var tmResultTokens = textMateResult.Tokens;
        for (int tokenIndex = 0, len = tmResultTokens.Length; tokenIndex < len; tokenIndex++)
        {
            var token = tmResultTokens[tokenIndex];
            var tokenStartIndex = token.StartIndex;
            var tokenType = DecodeTextMateToken(_decodeMap, token.Scopes);

            // do not push a new token if the type is exactly the same (also
            // helps with ligatures)
            if (!tokenType.Equals(lastTokenType))
            {
                tokens.Add(new(tokenStartIndex + offsetDelta, token.Scopes));
                lastTokenType = tokenType;
            }
        }

        return new(tokens, offsetDelta + line.Length, freshState);
    }

    private string DecodeTextMateToken(DecodeMap decodeMap, List<string> scopes)
    {
        var prevTokenScopes = decodeMap.PrevToken.Scopes;
        var prevTokenScopesLength = prevTokenScopes.Length;
        var prevTokenScopeTokensMaps = decodeMap.PrevToken.ScopeTokensMaps;

        var scopeTokensMaps = new Dictionary<int, Dictionary<int, bool>>();
        var prevScopeTokensMaps = new Dictionary<int, bool>();
        var sameAsPrev = true;
        for (var level = 1 /* deliberately skip scope 0 */; level < scopes.Count; level++)
        {
            var scope = scopes[level];

            if (sameAsPrev)
            {
                if (level < prevTokenScopesLength && prevTokenScopes[level].Equals(scope))
                {
                    prevScopeTokensMaps = prevTokenScopeTokensMaps[level];
                    scopeTokensMaps[level] = prevScopeTokensMaps;
                    continue;
                }

                sameAsPrev = false;
            }

            var tokens = decodeMap.getTokenIds(scope);
            prevScopeTokensMaps = new(prevScopeTokensMaps);
            foreach (var token in tokens)
                prevScopeTokensMaps[token] = true;
            scopeTokensMaps[level] = prevScopeTokensMaps;
        }

        decodeMap.PrevToken = new(scopes.ToArray(), scopeTokensMaps);
        return decodeMap.GetToken(prevScopeTokensMaps);
    }
}
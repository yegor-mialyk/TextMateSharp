using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars;

internal class LineTokens
{
    private readonly BalancedBracketSelectors _balancedBracketSelectors;

    private readonly bool _emitBinaryTokens;

    // used only if `_emitBinaryTokens` is false.
    private readonly List<IToken> _tokens;
    private readonly List<TokenTypeMatcher> _tokenTypeOverrides;

    // used only if `_emitBinaryTokens` is true.
    private readonly List<int> binaryTokens;

    private int _lastTokenEndIndex;
    private string _lineText;

    internal LineTokens(
        bool emitBinaryTokens,
        string lineText,
        List<TokenTypeMatcher> tokenTypeOverrides,
        BalancedBracketSelectors? balancedBracketSelectors)
    {
        _emitBinaryTokens = emitBinaryTokens;
        _lineText = lineText;
        if (_emitBinaryTokens)
        {
            _tokens = null;
            binaryTokens = new();
        }
        else
        {
            _tokens = new();
            binaryTokens = null;
        }

        _tokenTypeOverrides = tokenTypeOverrides;
        _balancedBracketSelectors = balancedBracketSelectors;
    }

    public void Produce(StateStack stack, int endIndex)
    {
        ProduceFromScopes(stack.ContentNameScopesList, endIndex);
    }

    public void ProduceFromScopes(AttributedScopeStack scopesList, int endIndex)
    {
        if (_lastTokenEndIndex >= endIndex)
            return;

        if (_emitBinaryTokens)
        {
            var metadata = scopesList.TokenAttributes;

            var containsBalancedBrackets = false;
            var balancedBracketSelectors = _balancedBracketSelectors;
            if (balancedBracketSelectors != null && balancedBracketSelectors.MatchesAlways())
                containsBalancedBrackets = true;

            if (_tokenTypeOverrides.Count > 0 ||
                (balancedBracketSelectors != null &&
                    !balancedBracketSelectors.MatchesAlways() &&
                    !balancedBracketSelectors.MatchesNever()))
            {
                // Only generate scope array when required to improve performance
                var scopes2 = scopesList.GetScopeNames();
                foreach (var tokenType in _tokenTypeOverrides)
                    if (tokenType.Matcher.Invoke(scopes2))
                        metadata = EncodedTokenAttributes.Set(
                            metadata,
                            0,
                            tokenType.Type, // toOptionalTokenType(tokenType.type),
                            null,
                            FontStyle.NotSet,
                            0,
                            0);

                if (balancedBracketSelectors != null)
                    containsBalancedBrackets = balancedBracketSelectors.Match(scopes2);
            }

            if (containsBalancedBrackets)
                metadata = EncodedTokenAttributes.Set(
                    metadata,
                    0,
                    StandardTokenType.NotSet,
                    containsBalancedBrackets,
                    FontStyle.NotSet,
                    0,
                    0);

            if (binaryTokens.Count != 0 && binaryTokens[binaryTokens.Count - 1] == metadata)
            {
                // no need to push a token with the same metadata
                _lastTokenEndIndex = endIndex;
                return;
            }

            binaryTokens.Add(_lastTokenEndIndex);
            binaryTokens.Add(metadata);

            _lastTokenEndIndex = endIndex;
            return;
        }

        var scopes = scopesList.GetScopeNames();

        _tokens.Add(new Token(
            _lastTokenEndIndex >= 0 ? _lastTokenEndIndex : 0,
            endIndex,
            scopes));

        _lastTokenEndIndex = endIndex;
    }


    public IToken[] GetResult(StateStack stack, int lineLength)
    {
        if (_tokens.Count != 0 && _tokens[_tokens.Count - 1].StartIndex == lineLength - 1)
            // pop produced token for newline
            _tokens.RemoveAt(_tokens.Count - 1);

        if (_tokens.Count == 0)
        {
            _lastTokenEndIndex = -1;
            Produce(stack, lineLength);
            _tokens[_tokens.Count - 1].StartIndex = 0;
        }

        return _tokens.ToArray();
    }

    public int[] GetBinaryResult(StateStack stack, int lineLength)
    {
        if (binaryTokens.Count != 0 && binaryTokens[binaryTokens.Count - 2] == lineLength - 1)
        {
            // pop produced token for newline
            binaryTokens.RemoveAt(binaryTokens.Count - 1);
            binaryTokens.RemoveAt(binaryTokens.Count - 1);
        }

        if (binaryTokens.Count == 0)
        {
            _lastTokenEndIndex = -1;
            Produce(stack, lineLength);
            binaryTokens[binaryTokens.Count - 2] = 0;
        }

        var result = new int[binaryTokens.Count];
        for (int i = 0, len = binaryTokens.Count; i < len; i++)
            result[i] = binaryTokens[i];

        return result;
    }
}

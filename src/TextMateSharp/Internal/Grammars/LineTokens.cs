using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars;

public class LineTokens
{
    private readonly List<IToken> _tokens = [];

    private int _lastTokenEndIndex;

    public void Produce(StateStack stack, int endIndex)
    {
        ProduceFromScopes(stack.ContentNameScopesList, endIndex);
    }

    public void ProduceFromScopes(AttributedScopeStack scopesList, int endIndex)
    {
        if (_lastTokenEndIndex >= endIndex)
            return;

        var scopes = scopesList.GetScopeNames();

        _tokens.Add(new Token(
            _lastTokenEndIndex >= 0 ? _lastTokenEndIndex : 0,
            endIndex,
            scopes));

        _lastTokenEndIndex = endIndex;
    }

    public List<IToken> GetResult(StateStack stack, int lineLength)
    {
        if (_tokens.Count != 0 && _tokens[^1].StartIndex == lineLength - 1)
            // pop produced token for newline
            _tokens.RemoveAt(_tokens.Count - 1);

        if (_tokens.Count == 0)
        {
            _lastTokenEndIndex = -1;
            Produce(stack, lineLength);
            _tokens[^1].StartIndex = 0;
        }

        return _tokens;
    }
}

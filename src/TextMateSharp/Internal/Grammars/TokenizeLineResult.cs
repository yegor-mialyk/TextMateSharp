using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars;

public class TokenizeLineResult : ITokenizeLineResult
{
    public TokenizeLineResult(IToken[] tokens, IStateStack ruleStack, bool stoppedEarly)
    {
        Tokens = tokens;
        RuleStack = ruleStack;
        StoppedEarly = stoppedEarly;
    }

    public bool StoppedEarly { get; }

    public IToken[] Tokens { get; }

    public IStateStack RuleStack { get; }
}

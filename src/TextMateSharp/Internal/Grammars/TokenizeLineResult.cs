using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars;

public class TokenizeLineResult(IToken[] tokens, StateStack ruleStack, bool stoppedEarly) : ITokenizeLineResult
{
    public bool StoppedEarly { get; } = stoppedEarly;

    public IToken[] Tokens { get; } = tokens;

    public StateStack RuleStack { get; } = ruleStack;
}

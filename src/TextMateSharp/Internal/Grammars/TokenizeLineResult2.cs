using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars;

public class TokenizeLineResult2 : ITokenizeLineResult2
{
    public TokenizeLineResult2(int[] tokens, IStateStack ruleStack, bool stoppedEarly)
    {
        Tokens = tokens;
        RuleStack = ruleStack;
        StoppedEarly = stoppedEarly;
    }

    public bool StoppedEarly { get; }

    public int[] Tokens { get; }

    public IStateStack RuleStack { get; }
}

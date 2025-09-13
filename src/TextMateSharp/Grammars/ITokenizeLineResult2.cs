namespace TextMateSharp.Grammars;

public interface ITokenizeLineResult2
{
    int[] Tokens { get; }

    bool StoppedEarly { get; }

    IStateStack RuleStack { get; }
}

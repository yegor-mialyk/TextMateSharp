namespace TextMateSharp.Grammars;

public interface ITokenizeLineResult
{
    IToken[] Tokens { get; }

    bool StoppedEarly { get; }

    IStateStack RuleStack { get; }
}

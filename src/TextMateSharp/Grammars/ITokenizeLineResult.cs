namespace TextMateSharp.Grammars;

public interface ITokenizeLineResult
{
    IToken[] Tokens { get; }

    bool StoppedEarly { get; }

    StateStack RuleStack { get; }
}

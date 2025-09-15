namespace TextMateSharp.Grammars;

public interface ITokenizeLineResult
{
    List<IToken> Tokens { get; }

    bool StoppedEarly { get; }

    StateStack RuleStack { get; }
}

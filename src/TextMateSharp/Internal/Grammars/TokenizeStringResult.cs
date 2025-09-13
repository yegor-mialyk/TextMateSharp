using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars;

public class TokenizeStringResult
{
    public TokenizeStringResult(StateStack stack, bool stoppedEarly)
    {
        Stack = stack;
        StoppedEarly = stoppedEarly;
    }

    public StateStack Stack { get; }

    public bool StoppedEarly { get; }
}

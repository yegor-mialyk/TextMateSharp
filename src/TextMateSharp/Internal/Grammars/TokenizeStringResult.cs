using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars;

public class TokenizeStringResult
{
    public TokenizeStringResult(StateStack stack, bool stoppedEarly)
    {
        Stack = stack;
        StoppedEarly = stoppedEarly;
    }

    public StateStack Stack { get; private set; }
    public bool StoppedEarly { get; private set; }
}
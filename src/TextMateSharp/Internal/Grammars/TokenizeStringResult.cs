using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars;

public class TokenizeStringResult(StateStack stack, bool stoppedEarly)
{
    public StateStack Stack { get; } = stack;

    public bool StoppedEarly { get; } = stoppedEarly;
}

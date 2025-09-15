namespace TextMateSharp.Model;

public class LineTokens
{
    public LineTokens(List<TMToken> tokens, int actualStopOffset, TMState endState)
    {
        Tokens = tokens;
        ActualStopOffset = actualStopOffset;
        EndState = endState;
    }

    public List<TMToken> Tokens { get; set; }

    public int ActualStopOffset { get; set; }

    public TMState EndState { get; set; }
}

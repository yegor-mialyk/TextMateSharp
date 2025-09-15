namespace TextMateSharp.Internal.Grammars;

public class LocalStackElement
{
    public LocalStackElement(AttributedScopeStack scopes, int endPos)
    {
        Scopes = scopes;
        EndPos = endPos;
    }

    public AttributedScopeStack Scopes { get; private set; }

    public int EndPos { get; private set; }
}

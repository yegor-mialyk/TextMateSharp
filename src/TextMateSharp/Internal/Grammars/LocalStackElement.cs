namespace TextMateSharp.Internal.Grammars;

internal class LocalStackElement
{
    public LocalStackElement(AttributedScopeStack scopes, int endPos)
    {
        Scopes = scopes;
        EndPos = endPos;
    }

    public AttributedScopeStack Scopes { get; private set; }

    public int EndPos { get; private set; }
}

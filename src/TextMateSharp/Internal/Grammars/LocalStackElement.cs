namespace TextMateSharp.Internal.Grammars;

public class LocalStackElement(AttributedScopeStack scopes, int endPos)
{
    public AttributedScopeStack Scopes { get; private set; } = scopes;

    public int EndPos { get; private set; } = endPos;
}

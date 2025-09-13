using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars;

internal class Token : IToken
{
    public Token(int startIndex, int endIndex, List<string> scopes)
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
        Scopes = scopes;
    }

    public int StartIndex { get; set; }

    public int EndIndex { get; }

    public int Length => EndIndex - StartIndex;

    public List<string> Scopes { get; }
}

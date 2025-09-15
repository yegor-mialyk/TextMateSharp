using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars;

public class Token(int startIndex, int endIndex, List<string> scopes) : IToken
{
    public int StartIndex { get; set; } = startIndex;

    public int EndIndex { get; } = endIndex;

    public int Length => EndIndex - StartIndex;

    public List<string> Scopes { get; } = scopes;
}

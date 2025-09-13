using System.Text;
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

    public override string ToString()
    {
        var s = new StringBuilder();
        s.Append("{startIndex: ");
        s.Append(StartIndex);
        s.Append(", endIndex: ");
        s.Append(EndIndex);
        s.Append(", scopes: ");
        s.Append(string.Join(", ", Scopes));
        s.Append('}');
        return s.ToString();
    }
}
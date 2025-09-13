namespace TextMateSharp.Model;

public class TMToken
{
    public TMToken(int startIndex, List<string> scopes)
    {
        StartIndex = startIndex;
        Scopes = scopes;
    }

    public int StartIndex { get; private set; }
    public List<string> Scopes { get; private set; }
}
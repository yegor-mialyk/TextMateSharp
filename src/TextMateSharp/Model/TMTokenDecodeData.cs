namespace TextMateSharp.Model;

public class TMTokenDecodeData
{
    public TMTokenDecodeData(string[] scopes, Dictionary<int, Dictionary<int, bool>> scopeTokensMaps)
    {
        Scopes = scopes;
        ScopeTokensMaps = scopeTokensMaps;
    }

    public string[] Scopes { get; private set; }
    public Dictionary<int, Dictionary<int, bool>> ScopeTokensMaps { get; private set; }
}
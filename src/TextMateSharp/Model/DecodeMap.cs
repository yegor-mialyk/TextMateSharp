using System.Text;

namespace TextMateSharp.Model;

internal class DecodeMap
{
    private readonly Dictionary<string /* scope */, int[] /* ids */> _scopeToTokenIds = new();
    private readonly Dictionary<int /* id */, string /* id */> _tokenIdToToken = new();
    private readonly Dictionary<string /* token */, int? /* id */> _tokenToTokenId = new();

    private int _lastAssignedId;

    public TMTokenDecodeData PrevToken { get; set; } = new([], new());

    public int[] GetTokenIds(string scope)
    {
        _scopeToTokenIds.TryGetValue(scope, out var tokens);
        if (tokens != null)
            return tokens;

        var tmpTokens = scope.Split(["[.]"], StringSplitOptions.None);

        tokens = new int[tmpTokens.Length];
        for (var i = 0; i < tmpTokens.Length; i++)
        {
            var token = tmpTokens[i];
            _tokenToTokenId.TryGetValue(token, out var tokenId);
            if (tokenId == null)
            {
                tokenId = ++_lastAssignedId;
                _tokenToTokenId[token] = tokenId.Value;
                _tokenIdToToken[tokenId.Value] = token;
            }

            tokens[i] = tokenId.Value;
        }

        _scopeToTokenIds[scope] = tokens;
        return tokens;
    }

    public string GetToken(Dictionary<int, bool> tokenMap)
    {
        var result = new StringBuilder();
        var isFirst = true;
        for (var i = 1; i <= _lastAssignedId; i++)
            if (tokenMap.ContainsKey(i))
            {
                if (isFirst)
                {
                    isFirst = false;
                    result.Append(_tokenIdToToken[i]);
                }
                else
                {
                    result.Append('.');
                    result.Append(_tokenIdToToken[i]);
                }
            }

        return result.ToString();
    }
}

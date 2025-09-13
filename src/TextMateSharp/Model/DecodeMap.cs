using System.Text;

namespace TextMateSharp.Model;

internal class DecodeMap
{
    private readonly Dictionary<string /* scope */, int[] /* ids */> _scopeToTokenIds;
    private readonly Dictionary<int /* id */, string /* id */> _tokenIdToToken;
    private readonly Dictionary<string /* token */, int? /* id */> _tokenToTokenId;

    private int lastAssignedId;

    public DecodeMap()
    {
        PrevToken = new(new string[0], new());

        lastAssignedId = 0;
        _scopeToTokenIds = new();
        _tokenToTokenId = new();
        _tokenIdToToken = new();
    }

    public TMTokenDecodeData PrevToken { get; set; }

    public int[] getTokenIds(string scope)
    {
        int[] tokens;
        _scopeToTokenIds.TryGetValue(scope, out tokens);
        if (tokens != null)
            return tokens;

        var tmpTokens = scope.Split(new[] { "[.]" }, StringSplitOptions.None);

        tokens = new int[tmpTokens.Length];
        for (var i = 0; i < tmpTokens.Length; i++)
        {
            var token = tmpTokens[i];
            int? tokenId;
            _tokenToTokenId.TryGetValue(token, out tokenId);
            if (tokenId == null)
            {
                tokenId = ++lastAssignedId;
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
        for (var i = 1; i <= lastAssignedId; i++)
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
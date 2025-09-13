using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Matcher;

public interface IMatchesName<in T>
{
    bool Match(ICollection<string?> names, T scopes);
}

public class NameMatcher : IMatchesName<List<string>>
{
    public static readonly IMatchesName<List<string>> Default = new NameMatcher();

    public bool Match(ICollection<string?> identifers, List<string> scopes)
    {
        if (scopes.Count < identifers.Count)
            return false;

        var lastIndex = 0;
        return identifers.All(identifier =>
        {
            for (var i = lastIndex; i < scopes.Count; i++)
                if (ScopesAreMatching(scopes[i], identifier))
                {
                    lastIndex++;
                    return true;
                }

            return false;
        });
    }

    private static bool ScopesAreMatching(string? thisScopeName, string scopeName)
    {
        if (thisScopeName == null)
            return false;
        if (thisScopeName.Equals(scopeName))
            return true;
        var len = scopeName.Length;
        return thisScopeName.Length > len &&
            thisScopeName.SubstringAtIndexes(0, len).Equals(scopeName) &&
            thisScopeName[len] == '.';
    }
}

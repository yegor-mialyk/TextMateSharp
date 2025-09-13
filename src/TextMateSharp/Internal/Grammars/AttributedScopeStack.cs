using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars;

public class AttributedScopeStack
{
    public AttributedScopeStack(AttributedScopeStack parent, string scopePath, int tokenAttributes)
    {
        Parent = parent;
        ScopePath = scopePath;
        TokenAttributes = tokenAttributes;
    }

    public AttributedScopeStack Parent { get; }
    public string ScopePath { get; }
    public int TokenAttributes { get; }

    private static bool StructuralEquals(AttributedScopeStack a, AttributedScopeStack b)
    {
        do
        {
            if (a == b)
                return true;

            if (a == null && b == null)
                // End of list reached for both
                return true;

            if (a == null || b == null)
                // End of list reached only for one
                return false;

            if (a.ScopePath != b.ScopePath || a.TokenAttributes != b.TokenAttributes)
                return false;

            // Go to previous pair
            a = a.Parent;
            b = b.Parent;
        } while (true);
    }

    private static bool Equals(AttributedScopeStack a, AttributedScopeStack b)
    {
        if (a == b)
            return true;
        if (a == null || b == null)
            return false;
        return StructuralEquals(a, b);
    }

    public override bool Equals(object other)
    {
        if (other == null || other is AttributedScopeStack)
            return false;

        return Equals(this, (AttributedScopeStack) other);
    }

    public override int GetHashCode()
    {
        return Parent.GetHashCode() +
            ScopePath.GetHashCode() +
            TokenAttributes.GetHashCode();
    }


    private static bool MatchesScope(string scope, string selector, string selectorWithDot)
    {
        return selector.Equals(scope) || scope.StartsWith(selectorWithDot);
    }

    private static bool Matches(AttributedScopeStack target, List<string> parentScopes)
    {
        if (parentScopes == null)
            return true;

        var len = parentScopes.Count;
        var index = 0;
        var selector = parentScopes[index];
        var selectorWithDot = selector + ".";

        while (target != null)
        {
            if (MatchesScope(target.ScopePath, selector, selectorWithDot))
            {
                index++;
                if (index == len)
                    return true;
                selector = parentScopes[index];
                selectorWithDot = selector + '.';
            }

            target = target.Parent;
        }

        return false;
    }

    public static int MergeAttributes(
        int existingTokenAttributes,
        AttributedScopeStack scopesList,
        BasicScopeAttributes basicScopeAttributes)
    {
        if (basicScopeAttributes == null)
            return existingTokenAttributes;

        var fontStyle = FontStyle.NotSet;
        var foreground = 0;
        var background = 0;

        if (basicScopeAttributes.ThemeData != null)
            // Find the first themeData that matches
            foreach (var themeData in basicScopeAttributes.ThemeData)
                if (Matches(scopesList, themeData.parentScopes))
                {
                    fontStyle = themeData.fontStyle;
                    foreground = themeData.foreground;
                    background = themeData.background;
                    break;
                }

        return EncodedTokenAttributes.Set(
            existingTokenAttributes,
            basicScopeAttributes.LanguageId,
            basicScopeAttributes.TokenType,
            null,
            fontStyle,
            foreground,
            background);
    }

    private static AttributedScopeStack Push(AttributedScopeStack target, Grammar grammar, List<string> scopes)
    {
        foreach (var scope in scopes)
        {
            var rawMetadata = grammar.GetMetadataForScope(scope);
            var metadata = MergeAttributes(target.TokenAttributes, target, rawMetadata);
            target = new(target, scope, metadata);
        }

        return target;
    }

    public AttributedScopeStack PushAtributed(string scopePath, Grammar grammar)
    {
        if (scopePath == null)
            return this;
        if (scopePath.IndexOf(' ') >= 0)
            // there are multiple scopes to push
            return Push(this, grammar, new(scopePath.Split(new[] { " " }, StringSplitOptions.None)));
        // there is a single scope to push
        return Push(this, grammar, new() { scopePath });
    }

    public List<string> GetScopeNames()
    {
        return GenerateScopes(this);
    }

    private static List<string> GenerateScopes(AttributedScopeStack scopesList)
    {
        var result = new List<string>();
        while (scopesList != null)
        {
            result.Add(scopesList.ScopePath);
            scopesList = scopesList.Parent;
        }

        result.Reverse();
        return result;
    }
}
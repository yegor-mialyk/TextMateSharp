using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Themes;

public class ThemeTrieElement
{
    private readonly Dictionary<string /* segment */, ThemeTrieElement> children;

    // _themeTrieElementBrand: void;

    private readonly ThemeTrieElementRule mainRule;
    private readonly List<ThemeTrieElementRule> rulesWithParentScopes;

    public ThemeTrieElement(ThemeTrieElementRule mainRule) :
        this(mainRule, new(), new())
    {
    }

    public ThemeTrieElement(ThemeTrieElementRule mainRule, List<ThemeTrieElementRule> rulesWithParentScopes) :
        this(mainRule, rulesWithParentScopes, new())
    {
    }

    public ThemeTrieElement(ThemeTrieElementRule mainRule, List<ThemeTrieElementRule> rulesWithParentScopes,
        Dictionary<string /* segment */, ThemeTrieElement> children)
    {
        this.mainRule = mainRule;
        this.rulesWithParentScopes = rulesWithParentScopes;
        this.children = children;
    }

    private static List<ThemeTrieElementRule> SortBySpecificity(List<ThemeTrieElementRule> arr)
    {
        if (arr.Count == 1)
            return arr;
        arr.Sort((a, b) => CmpBySpecificity(a, b));
        return arr;
    }

    private static int CmpBySpecificity(ThemeTrieElementRule a, ThemeTrieElementRule b)
    {
        if (a.scopeDepth == b.scopeDepth)
        {
            var aParentScopes = a.parentScopes;
            var bParentScopes = b.parentScopes;
            var aParentScopesLen = aParentScopes == null ? 0 : aParentScopes.Count;
            var bParentScopesLen = bParentScopes == null ? 0 : bParentScopes.Count;
            if (aParentScopesLen == bParentScopesLen)
                for (var i = 0; i < aParentScopesLen; i++)
                {
                    var aLen = aParentScopes[i].Length;
                    var bLen = bParentScopes[i].Length;
                    if (aLen != bLen)
                        return bLen - aLen;
                }

            return bParentScopesLen - aParentScopesLen;
        }

        return b.scopeDepth - a.scopeDepth;
    }

    public List<ThemeTrieElementRule> Match(string scope)
    {
        List<ThemeTrieElementRule> arr;
        if ("".Equals(scope))
        {
            arr = new();
            arr.Add(mainRule);
            arr.AddRange(rulesWithParentScopes);
            return SortBySpecificity(arr);
        }

        var dotIndex = scope.IndexOf('.');
        string head;
        string tail;
        if (dotIndex == -1)
        {
            head = scope;
            tail = "";
        }
        else
        {
            head = scope.SubstringAtIndexes(0, dotIndex);
            tail = scope.Substring(dotIndex + 1);
        }

        if (children.TryGetValue(head, out var value))
            return value.Match(tail);

        arr = new();
        if (mainRule.foreground > 0)
            arr.Add(mainRule);
        arr.AddRange(rulesWithParentScopes);
        return SortBySpecificity(arr);
    }

    public void Insert(string name, int scopeDepth, string scope, List<string> parentScopes, FontStyle fontStyle,
        int foreground,
        int background)
    {
        if ("".Equals(scope))
        {
            DoInsertHere(name, scopeDepth, parentScopes, fontStyle, foreground, background);
            return;
        }

        var dotIndex = scope.IndexOf('.');
        string head;
        string tail;
        if (dotIndex == -1)
        {
            head = scope;
            tail = "";
        }
        else
        {
            head = scope.SubstringAtIndexes(0, dotIndex);
            tail = scope.Substring(dotIndex + 1);
        }

        ThemeTrieElement child;
        if (children.TryGetValue(head, out var value))
        {
            child = value;
        }
        else
        {
            child = new(mainRule.Clone(),
                ThemeTrieElementRule.cloneArr(rulesWithParentScopes));
            children[head] = child;
        }

        child.Insert(name, scopeDepth + 1, tail, parentScopes, fontStyle, foreground, background);
    }

    private void DoInsertHere(string name, int scopeDepth, List<string> parentScopes, FontStyle fontStyle,
        int foreground,
        int background)
    {
        if (parentScopes == null)
        {
            // Merge into the main rule
            mainRule.AcceptOverwrite(name, scopeDepth, fontStyle, foreground, background);
            return;
        }

        // Try to merge into existing rule
        foreach (var rule in rulesWithParentScopes)
            if (StringUtils.StrArrCmp(rule.parentScopes, parentScopes) == 0)
            {
                // bingo! => we get to merge this into an existing one
                rule.AcceptOverwrite(rule.name, scopeDepth, fontStyle, foreground, background);
                return;
            }

        // Must add a new rule

        // Inherit from main rule
        if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(mainRule.name))
            name = mainRule.name;
        if (fontStyle == FontStyle.NotSet)
            fontStyle = mainRule.fontStyle;
        if (foreground == 0)
            foreground = mainRule.foreground;
        if (background == 0)
            background = mainRule.background;

        rulesWithParentScopes.Add(
            new(name, scopeDepth, parentScopes, fontStyle, foreground, background));
    }

    public override int GetHashCode()
    {
        return children.GetHashCode() +
            mainRule.GetHashCode() +
            rulesWithParentScopes.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (this == obj)
            return true;
        if (obj == null)
            return false;
        if (GetType() != obj.GetType())
            return false;
        var other = (ThemeTrieElement) obj;
        return Equals(children, other.children) &&
            Equals(mainRule, other.mainRule) &&
            Equals(rulesWithParentScopes, other.rulesWithParentScopes);
    }
}
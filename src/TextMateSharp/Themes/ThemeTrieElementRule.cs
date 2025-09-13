namespace TextMateSharp.Themes;

public class ThemeTrieElementRule
{
    public int background;
    public FontStyle fontStyle;
    public int foreground;
    public string name;

    public List<string>? ParentScopes { get; }

    public int scopeDepth;

    public ThemeTrieElementRule(string name, int scopeDepth, List<string>? parentScopes, FontStyle fontStyle,
        int foreground,
        int background)
    {
        this.name = name;
        this.scopeDepth = scopeDepth;
        ParentScopes = parentScopes;
        this.fontStyle = fontStyle;
        this.foreground = foreground;
        this.background = background;
    }

    public ThemeTrieElementRule Clone()
    {
        return new(name, scopeDepth, ParentScopes, fontStyle, foreground, background);
    }

    public static List<ThemeTrieElementRule> cloneArr(List<ThemeTrieElementRule> arr)
    {
        var r = new List<ThemeTrieElementRule>();
        for (int i = 0, len = arr.Count; i < len; i++)
            r.Add(arr[i].Clone());
        return r;
    }

    public void AcceptOverwrite(string name, int scopeDepth, FontStyle fontStyle, int foreground, int background)
    {
        if (this.scopeDepth > scopeDepth)
        {
            // console.log('how did this happen?');
        }
        else
        {
            this.scopeDepth = scopeDepth;
        }

        // console.log('TODO -> my depth: ' + this.scopeDepth + ', overwriting depth: ' + scopeDepth);
        if (fontStyle != FontStyle.NotSet)
            this.fontStyle = fontStyle;
        if (foreground != 0)
            this.foreground = foreground;
        if (background != 0)
            this.background = background;
        if (!string.IsNullOrEmpty(name))
            this.name = name;
    }

    public override bool Equals(object? obj)
    {
        if (this == obj)
            return true;
        if (obj == null)
            return false;
        if (GetType() != obj.GetType())
            return false;
        var other = (ThemeTrieElementRule) obj;
        return background == other.background &&
            fontStyle == other.fontStyle &&
            foreground == other.foreground &&
            Equals(ParentScopes, other.ParentScopes) &&
            scopeDepth == other.scopeDepth;
    }
}

namespace TextMateSharp.Themes;

public class ParsedThemeRule
{
    public string background;

    // -1 if not set.An or mask of `FontStyle` otherwise.
    public FontStyle fontStyle;
    public string foreground;
    public int index;
    public string name;
    public List<string> parentScopes;
    public string scope;

    public ParsedThemeRule(string name, string scope, List<string> parentScopes, int index, FontStyle fontStyle,
        string foreground, string background)
    {
        this.name = name;
        this.scope = scope;
        this.parentScopes = parentScopes;
        this.index = index;
        this.fontStyle = fontStyle;
        this.foreground = foreground;
        this.background = background;
    }

    public override int GetHashCode()
    {
        var prime = 31;
        var result = 1;
        result = prime * result + (background == null ? 0 : background.GetHashCode());
        result = prime * result + (int) fontStyle;
        result = prime * result + (foreground == null ? 0 : foreground.GetHashCode());
        result = prime * result + index;
        result = prime * result + (parentScopes == null ? 0 : parentScopes.GetHashCode());
        result = prime * result + (scope == null ? 0 : scope.GetHashCode());
        return result;
    }

    public override string ToString()
    {
        return "ParsedThemeRule [scope=" +
            scope +
            ", parentScopes=" +
            string.Join(", ", parentScopes) +
            ", index=" +
            index +
            ", fontStyle=" +
            fontStyle +
            ", foreground=" +
            foreground +
            ", background=" +
            background +
            "]";
    }

    public override bool Equals(object? obj)
    {
        if (this == obj)
            return true;
        if (obj == null)
            return false;
        if (GetType() != obj.GetType())
            return false;
        var other = (ParsedThemeRule) obj;
        if (background == null)
        {
            if (other.background != null)
                return false;
        }
        else if (!background.Equals(other.background))
        {
            return false;
        }

        if (fontStyle != other.fontStyle)
            return false;
        if (foreground == null)
        {
            if (other.foreground != null)
                return false;
        }
        else if (!foreground.Equals(other.foreground))
        {
            return false;
        }

        if (index != other.index)
            return false;
        if (parentScopes == null)
        {
            if (other.parentScopes != null)
                return false;
        }
        else if (!parentScopes.SequenceEqual(other.parentScopes))
        {
            return false;
        }

        if (scope == null)
        {
            if (other.scope != null)
                return false;
        }
        else if (!scope.Equals(other.scope))
        {
            return false;
        }

        return true;
    }
}

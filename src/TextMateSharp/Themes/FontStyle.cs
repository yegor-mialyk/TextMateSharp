namespace TextMateSharp.Themes;

[Flags]
public enum FontStyle
{
    NotSet = -1,

    None = 0,
    Italic = 1,
    Bold = 2,
    Underline = 4,
    Strikethrough = 8
}

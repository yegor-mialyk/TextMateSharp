using System.Text.RegularExpressions;

namespace TextMateSharp.Internal.Utils;

internal static partial class StringUtils
{
    private static readonly Regex rrggbb = MyRegex();
    private static readonly Regex rrggbbaa = MyRegex1();
    private static readonly Regex rgb = MyRegex2();
    private static readonly Regex rgba = MyRegex3();

    internal static string SubstringAtIndexes(this string str, int startIndex, int endIndex)
    {
        return str.Substring(startIndex, endIndex - startIndex);
    }

    internal static bool IsValidHexColor(string hex)
    {
        if (hex == null || hex.Length < 1)
            return false;

        if (rrggbb.Match(hex).Success)
            // #rrggbb
            return true;

        if (rrggbbaa.Match(hex).Success)
            // #rrggbbaa
            return true;

        if (rgb.Match(hex).Success)
            // #rgb
            return true;

        if (rgba.Match(hex).Success)
            // #rgba
            return true;

        return false;
    }

    public static int StrCmp(string a, string b)
    {
        if (a == null && b == null)
            return 0;
        if (a == null)
            return -1;
        if (b == null)
            return 1;
        var result = a.CompareTo(b);
        if (result < 0)
            return -1;

        if (result > 0)
            return 1;
        return 0;
    }

    public static int StrArrCmp(List<string> a, List<string> b)
    {
        if (a == null && b == null)
            return 0;
        if (a == null)
            return -1;
        if (b == null)
            return 1;
        var len1 = a.Count;
        var len2 = b.Count;
        if (len1 == len2)
        {
            for (var i = 0; i < len1; i++)
            {
                var res = StrCmp(a[i], b[i]);
                if (res != 0)
                    return res;
            }

            return 0;
        }

        return len1 - len2;
    }

    [GeneratedRegex("^#[0-9a-f]{6}", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex MyRegex();
    [GeneratedRegex("^#[0-9a-f]{8}", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex MyRegex1();
    [GeneratedRegex("^#[0-9a-f]{3}", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex MyRegex2();
    [GeneratedRegex("^#[0-9a-f]{4}", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex MyRegex3();
}

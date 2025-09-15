using System.Text;
using System.Text.RegularExpressions;
using Onigwrap;

namespace TextMateSharp.Internal.Utils;

public partial class RegexSource
{
    private static readonly Regex CAPTURING_REGEX_SOURCE = MyRegex();

    public static string EscapeRegExpCharacters(string value)
    {
        var valueLen = value.Length;

        var sb = new StringBuilder(valueLen);

        for (var i = 0; i < valueLen; i++)
        {
            var ch = value[i];

            if (ch is '-' or '\\' or '{' or '}' or '*' or '+' or '?' or '|' or '^' or '$' or '.' or ',' or '[' or ']'
                or '(' or ')' or '#')
                sb.Append('\\');

            sb.Append(ch);
        }

        return sb.ToString();
    }

    public static bool HasCaptures(string? regexSource)
    {
        return regexSource != null && CAPTURING_REGEX_SOURCE.Match(regexSource).Success;
    }

    public static string? ReplaceCaptures(string? regexSource, string? captureSource, IOnigCaptureIndex[]? captureIndices)
    {
        if (regexSource == null || captureSource == null || captureIndices == null)
            return regexSource;

        return CAPTURING_REGEX_SOURCE.Replace(
            regexSource, m => GetReplacement(m.Value, captureSource, captureIndices));
    }

    private static string GetReplacement(string match, string captureSource, IOnigCaptureIndex[] captureIndices)
    {
        int index;

        string? command = null;

        var doublePointIndex = match.IndexOf(':');

        if (doublePointIndex != -1)
        {
            index = int.Parse(match.SubstringAtIndexes(2, doublePointIndex));
            command = match.SubstringAtIndexes(doublePointIndex + 2, match.Length - 1);
        }
        else
            index = int.Parse(match.SubstringAtIndexes(1, match.Length));

        var capture = captureIndices.Length > index ? captureIndices[index] : null;

        if (capture == null)
            return match;

        var result = captureSource.SubstringAtIndexes(capture.Start, capture.End);

        // Remove leading dots that would make the selector invalid
        while (result.Length > 0 && result[0] == '.')
            result = result[1..];

        return command switch
        {
            "downcase" => result.ToLower(),
            "upcase" => result.ToUpper(),
            _ => result
        };
    }

    [GeneratedRegex("\\$(\\d+)|\\$\\{(\\d+):\\/(downcase|upcase)}")]
    private static partial Regex MyRegex();
}

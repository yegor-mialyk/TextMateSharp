using System.Text;
using System.Text.RegularExpressions;
using Onigwrap;

namespace TextMateSharp.Internal.Utils;

public class RegexSource
{
    private static readonly Regex CAPTURING_REGEX_SOURCE = new(
        "\\$(\\d+)|\\$\\{(\\d+):\\/(downcase|upcase)}");

    public static string EscapeRegExpCharacters(string value)
    {
        var valueLen = value.Length;
        var sb = new StringBuilder(valueLen);
        for (var i = 0; i < valueLen; i++)
        {
            var ch = value[i];
            switch (ch)
            {
                case '-':
                case '\\':
                case '{':
                case '}':
                case '*':
                case '+':
                case '?':
                case '|':
                case '^':
                case '$':
                case '.':
                case ',':
                case '[':
                case ']':
                case '(':
                case ')':
                case '#':
                    /* escaping white space chars is actually not necessary:
                    case ' ':
                    case '\t':
                    case '\n':
                    case '\f':
                    case '\r':
                    case 0x0B: // vertical tab \v
                    */
                    sb.Append('\\');
                    break;
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }

    public static bool HasCaptures(string regexSource)
    {
        if (regexSource == null)
            return false;
        return CAPTURING_REGEX_SOURCE.Match(regexSource).Success;
    }

    public static string ReplaceCaptures(string regexSource, string captureSource, IOnigCaptureIndex[] captureIndices)
    {
        return CAPTURING_REGEX_SOURCE.Replace(
            regexSource, m => GetReplacement(m.Value, captureSource, captureIndices));
    }

    private static string GetReplacement(string match, string captureSource, IOnigCaptureIndex[] captureIndices)
    {
        var index = -1;
        string command = null;
        var doublePointIndex = match.IndexOf(':');
        if (doublePointIndex != -1)
        {
            index = int.Parse(match.SubstringAtIndexes(2, doublePointIndex));
            command = match.SubstringAtIndexes(doublePointIndex + 2, match.Length - 1);
        }
        else
        {
            index = int.Parse(match.SubstringAtIndexes(1, match.Length));
        }

        var capture = captureIndices.Length > index ? captureIndices[index] : null;
        if (capture != null)
        {
            var result = captureSource.SubstringAtIndexes(capture.Start, capture.End);
            // Remove leading dots that would make the selector invalid
            while (result.Length > 0 && result[0] == '.')
                result = result.Substring(1);
            if ("downcase".Equals(command))
                return result.ToLower();

            if ("upcase".Equals(command))
                return result.ToUpper();

            return result;
        }

        return match;
    }
}
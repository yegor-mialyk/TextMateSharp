using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Onigwrap;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Rules;

public partial class RegExpSource
{
    private static readonly Regex HAS_BACK_REFERENCES = MyRegex();
    private static readonly Regex BACK_REFERENCING_END = MyRegex1();

    private readonly bool _hasBackReferences;

    private readonly int _ruleId;
    private RegExpSourceAnchorCache? _anchorCache;
    private bool _hasAnchor;
    private string? _source;

    public RegExpSource(string? regExpSource, int ruleId, bool handleAnchors = true)
    {
        if (handleAnchors)
        {
            HandleAnchors(regExpSource);
        }
        else
        {
            _source = regExpSource;
            _hasAnchor = false;
        }

        if (_hasAnchor)
            _anchorCache = BuildAnchorCache();

        _ruleId = ruleId;
        _hasBackReferences = HAS_BACK_REFERENCES.Match(_source ?? string.Empty).Success;
    }

    public RegExpSource Clone()
    {
        return new(_source, _ruleId);
    }

    public void SetSource(string newSource)
    {
        if (_source == newSource)
            return;

        _source = newSource;

        if (_hasAnchor)
            _anchorCache = BuildAnchorCache();
    }

    private void HandleAnchors(string? regExpSource)
    {
        if (regExpSource != null)
        {
            var len = regExpSource.Length;
            var lastPushedPos = 0;
            var output = new StringBuilder();

            var hasAnchor = false;
            for (var pos = 0; pos < len; pos++)
            {
                var ch = regExpSource[pos];

                if (ch == '\\')
                    if (pos + 1 < len)
                    {
                        var nextCh = regExpSource[pos + 1];
                        if (nextCh == 'z')
                        {
                            output.Append(regExpSource.SubstringAtIndexes(lastPushedPos, pos));
                            output.Append("$(?!\\n)(?<!\\n)");
                            lastPushedPos = pos + 2;
                        }
                        else if (nextCh == 'A' || nextCh == 'G')
                        {
                            hasAnchor = true;
                        }

                        pos++;
                    }
            }

            _hasAnchor = hasAnchor;
            if (lastPushedPos == 0)
            {
                // No \z hit
                _source = regExpSource;
            }
            else
            {
                output.Append(regExpSource.SubstringAtIndexes(lastPushedPos, len));
                _source = output.ToString();
            }
        }
        else
        {
            _hasAnchor = false;
            _source = regExpSource;
        }
    }

    public string ResolveBackReferences(string lineText, IOnigCaptureIndex[] captureIndices)
    {
        var capturedValues = new List<string>();

        try
        {
            foreach (var captureIndex in captureIndices)
                capturedValues.Add(lineText.SubstringAtIndexes(
                    captureIndex.Start,
                    captureIndex.End));

            return BACK_REFERENCING_END.Replace(_source ?? string.Empty, m =>
            {
                try
                {
                    var value = m.Value;
                    var index = int.Parse(m.Value.SubstringAtIndexes(1, value.Length));
                    return EscapeRegExpCharacters(capturedValues.Count > index ? capturedValues[index] : "");
                }
                catch (Exception)
                {
                    return "";
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

        return lineText;
    }

    private static string EscapeRegExpCharacters(string value)
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

    private RegExpSourceAnchorCache BuildAnchorCache()
    {
        var source = _source;
        var sourceLen = source?.Length ?? 0;

        var A0_G0_result = new StringBuilder(sourceLen);
        var A0_G1_result = new StringBuilder(sourceLen);
        var A1_G0_result = new StringBuilder(sourceLen);
        var A1_G1_result = new StringBuilder(sourceLen);

        int pos;
        int len;

        for (pos = 0, len = sourceLen; pos < len; pos++)
        {
            var ch = source![pos];
            A0_G0_result.Append(ch);
            A0_G1_result.Append(ch);
            A1_G0_result.Append(ch);
            A1_G1_result.Append(ch);

            if (ch == '\\')
                if (pos + 1 < len)
                {
                    var nextCh = source[pos + 1];
                    if (nextCh == 'A')
                    {
                        A0_G0_result.Append('\uFFFF');
                        A0_G1_result.Append('\uFFFF');
                        A1_G0_result.Append('A');
                        A1_G1_result.Append('A');
                    }
                    else if (nextCh == 'G')
                    {
                        A0_G0_result.Append('\uFFFF');
                        A0_G1_result.Append('G');
                        A1_G0_result.Append('\uFFFF');
                        A1_G1_result.Append('G');
                    }
                    else
                    {
                        A0_G0_result.Append(nextCh);
                        A0_G1_result.Append(nextCh);
                        A1_G0_result.Append(nextCh);
                        A1_G1_result.Append(nextCh);
                    }

                    pos++;
                }
        }

        return new(
            A0_G0_result.ToString(),
            A0_G1_result.ToString(),
            A1_G0_result.ToString(),
            A1_G1_result.ToString());
    }

    public string? ResolveAnchors(bool allowA, bool allowG)
    {
        if (!_hasAnchor)
            return _source;

        if (allowA)
        {
            if (allowG)
                return _anchorCache?.A1_G1;

            return _anchorCache?.A1_G0;
        }

        if (allowG)
            return _anchorCache?.A0_G1;

        return _anchorCache?.A0_G0;
    }

    public bool HasAnchor()
    {
        return _hasAnchor;
    }

    public string? GetSource()
    {
        return _source;
    }

    public int GetRuleId()
    {
        return _ruleId;
    }

    public bool HasBackReferences()
    {
        return _hasBackReferences;
    }

    [GeneratedRegex("\\\\(\\d+)")]
    private static partial Regex MyRegex();
    [GeneratedRegex("\\\\(\\d+)")]
    private static partial Regex MyRegex1();
}

using Onigwrap;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Rules;

public abstract class Rule
{
    public const int NO_INIT = -3;

    public const int NO_RULE = 0;

    /**
     * This is a special constant to indicate that the end regexp matched.
     */
    public const int END_RULE = -1;

    /**
     * This is a special constant to indicate that the while regexp matched.
     */
    public const int WHILE_RULE = -2;

    private readonly string? _contentName;

    private readonly bool _contentNameIsCapturing;
    private readonly string? _name;

    private readonly bool _nameIsCapturing;

    protected Rule(int id, string? name, string? contentName)
    {
        Id = id;

        _name = name;
        _nameIsCapturing = RegexSource.HasCaptures(_name);
        _contentName = contentName;
        _contentNameIsCapturing = RegexSource.HasCaptures(_contentName);
    }

    public int Id { get; }

    public string? GetName(string? lineText, IOnigCaptureIndex[]? captureIndices)
    {
        return _nameIsCapturing ? RegexSource.ReplaceCaptures(_name, lineText, captureIndices) : _name;
    }

    public string? GetContentName(string lineText, IOnigCaptureIndex[] captureIndices)
    {
        return _contentNameIsCapturing
            ? RegexSource.ReplaceCaptures(_contentName, lineText, captureIndices)
            : _contentName;
    }

    public abstract void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst);

    public abstract CompiledRule? Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG);
}

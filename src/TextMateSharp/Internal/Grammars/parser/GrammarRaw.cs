using System.Collections;
using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Types;

namespace TextMateSharp.Internal.Grammars.Parser;

public class GrammarRaw : Dictionary<string, object>, IRawRepository, IRawRule, IRawGrammar, IRawCaptures
{
    private const string FIRST_LINE_MATCH = "firstLineMatch";
    private const string FILE_TYPES = "fileTypes";
    private const string SCOPE_NAME = "scopeName";
    private const string APPLY_END_PATTERN_LAST = "applyEndPatternLast";
    private const string REPOSITORY = "repository";
    private const string INJECTION_SELECTOR = "injectionSelector";
    private const string INJECTIONS = "injections";
    private const string PATTERNS = "patterns";
    private const string WHILE_CAPTURES = "whileCaptures";
    private const string END_CAPTURES = "endCaptures";
    private const string INCLUDE = "include";
    private const string WHILE = "while";
    private const string END = "end";
    private const string BEGIN = "begin";
    private const string CAPTURES = "captures";
    private const string MATCH = "match";
    private const string BEGIN_CAPTURES = "beginCaptures";
    private const string CONTENT_NAME = "contentName";
    private const string NAME = "name";
    private const string ID = "id";
    private const string DOLLAR_SELF = "$self";
    private const string DOLLAR_BASE = "$base";

    private List<string>? fileTypes;

    public IRawRule? GetCapture(string captureId)
    {
        return GetProp(captureId);
    }

    IEnumerator<string> IEnumerable<string>.GetEnumerator()
    {
        return Keys.GetEnumerator();
    }

    public Dictionary<string, IRawRule>? GetInjections()
    {
        var result = TryGetObject<GrammarRaw>(INJECTIONS);

        if (result == null)
            return null;

        return ConvertToDictionary<IRawRule>(result);
    }

    public string GetInjectionSelector()
    {
        return (string) this[INJECTION_SELECTOR];
    }

    public string? GetScopeName()
    {
        return TryGetObject<string>(SCOPE_NAME);
    }

    public ICollection<string> GetFileTypes()
    {
        if (fileTypes != null)
            return fileTypes;

        var list = new List<string>();
        var unparsedFileTypes = TryGetObject<ICollection>(FILE_TYPES);
        if (unparsedFileTypes != null)
            foreach (var o in unparsedFileTypes)
            {
                var str = o.ToString();
                // #202
                if (str == null)
                    continue;

                if (str.StartsWith("."))
                    str = str.Substring(1);

                list.Add(str);
            }

        fileTypes = list;

        return fileTypes;
    }

    public string? GetFirstLineMatch()
    {
        return TryGetObject<string>(FIRST_LINE_MATCH);
    }

    public IRawGrammar Clone()
    {
        return (IRawGrammar) Clone(this);
    }

    public IRawRepository Merge(params IRawRepository[] sources)
    {
        var target = new GrammarRaw();
        foreach (var source in sources)
        {
            var sourceRaw = (GrammarRaw) source;
            foreach (var key in sourceRaw.Keys)
                target[key] = sourceRaw[key];
        }

        return target;
    }

    public IRawRule? GetProp(string name)
    {
        return TryGetObject<IRawRule>(name);
    }

    public IRawRule? GetBase()
    {
        return TryGetObject<IRawRule>(DOLLAR_BASE);
    }

    public void SetBase(IRawRule ruleBase)
    {
        this[DOLLAR_BASE] = ruleBase;
    }

    public IRawRule? GetSelf()
    {
        return TryGetObject<IRawRule>(DOLLAR_SELF);
    }

    public void SetSelf(IRawRule self)
    {
        this[DOLLAR_SELF] = self;
    }

    public int GetId()
    {
        if (!TryGetValue(ID, out var result))
            return RuleId.NO_INIT;

        return (int) result;
    }

    public void SetId(int id)
    {
        this[ID] = id;
    }

    public string? GetName()
    {
        return TryGetObject<string>(NAME);
    }

    public void SetName(string name)
    {
        this[NAME] = name;
    }

    public string? GetContentName()
    {
        return TryGetObject<string>(CONTENT_NAME);
    }

    public string? GetMatch()
    {
        return TryGetObject<string>(MATCH);
    }

    public IRawCaptures? GetCaptures()
    {
        UpdateCaptures(CAPTURES);
        return TryGetObject<IRawCaptures>(CAPTURES);
    }

    public string? GetBegin()
    {
        return TryGetObject<string>(BEGIN);
    }

    public string? GetWhile()
    {
        return TryGetObject<string>(WHILE);
    }

    public string? GetInclude()
    {
        return TryGetObject<string>(INCLUDE);
    }

    public void SetInclude(string include)
    {
        this[INCLUDE] = include;
    }

    public IRawCaptures? GetBeginCaptures()
    {
        UpdateCaptures(BEGIN_CAPTURES);
        return TryGetObject<IRawCaptures>(BEGIN_CAPTURES);
    }

    public void SetBeginCaptures(IRawCaptures beginCaptures)
    {
        this[BEGIN_CAPTURES] = beginCaptures;
    }

    public string? GetEnd()
    {
        return TryGetObject<string>(END);
    }

    public IRawCaptures? GetEndCaptures()
    {
        UpdateCaptures(END_CAPTURES);
        return TryGetObject<IRawCaptures>(END_CAPTURES);
    }

    public IRawCaptures? GetWhileCaptures()
    {
        UpdateCaptures(WHILE_CAPTURES);
        return TryGetObject<IRawCaptures>(WHILE_CAPTURES);
    }

    public ICollection<IRawRule>? GetPatterns()
    {
        var result = TryGetObject<ICollection>(PATTERNS);

        return result?.Cast<IRawRule>().ToList();
    }

    public void SetPatterns(ICollection<IRawRule> patterns)
    {
        this[PATTERNS] = patterns;
    }

    public IRawRepository? GetRepository()
    {
        return TryGetObject<IRawRepository>(REPOSITORY);
    }

    public void SetRepository(IRawRepository repository)
    {
        this[REPOSITORY] = repository;
    }

    public bool IsApplyEndPatternLast()
    {
        return TryGetObject<object>(APPLY_END_PATTERN_LAST) switch
        {
            null => false,
            bool last => last,
            int patternLast => patternLast == 1,
            _ => false
        };
    }

    private void UpdateCaptures(string name)
    {
        var captures = TryGetObject<object>(name);
        if (captures is IList list)
        {
            var rawCaptures = new GrammarRaw();
            var i = 0;
            foreach (var capture in list)
            {
                i++;
                rawCaptures[i + ""] = capture;
            }

            this[name] = rawCaptures;
        }
    }

    public void SetApplyEndPatternLast(bool applyEndPatternLast)
    {
        this[APPLY_END_PATTERN_LAST] = applyEndPatternLast;
    }

    public object Clone(object value)
    {
        if (value is GrammarRaw rawToClone)
        {
            var raw = new GrammarRaw();

            foreach (var key in rawToClone.Keys)
                raw[key] = Clone(rawToClone[key]);
            return raw;
        }

        if (value is IList list)
        {
            var result = new List<object>();
            foreach (var obj in list)
                result.Add(Clone(obj));
            return result;
        }

        /*if (value is string)
            return value;

        if (value is int)
            return value;

        if (value is bool)
            return value;*/
        return value;
    }

    private static Dictionary<string, T> ConvertToDictionary<T>(GrammarRaw grammarRaw)
    {
        return grammarRaw.Keys.ToDictionary(key => key, key => (T) grammarRaw[key]);
    }

    private T? TryGetObject<T>(string key)
    {
        if (!TryGetValue(key, out var result))
            return default;

        return (T) result;
    }
}

using System.Collections;
using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Types;

namespace TextMateSharp.Internal.Grammars.Parser;

public class Raw : Dictionary<string, object>, IRawRepository, IRawRule, IRawGrammar, IRawCaptures
{
    private static readonly string FIRST_LINE_MATCH = "firstLineMatch";
    private static readonly string FILE_TYPES = "fileTypes";
    private static readonly string SCOPE_NAME = "scopeName";
    private static readonly string APPLY_END_PATTERN_LAST = "applyEndPatternLast";
    private static readonly string REPOSITORY = "repository";
    private static readonly string INJECTION_SELECTOR = "injectionSelector";
    private static readonly string INJECTIONS = "injections";
    private static readonly string PATTERNS = "patterns";
    private static readonly string WHILE_CAPTURES = "whileCaptures";
    private static readonly string END_CAPTURES = "endCaptures";
    private static readonly string INCLUDE = "include";
    private static readonly string WHILE = "while";
    private static readonly string END = "end";
    private static readonly string BEGIN = "begin";
    private static readonly string CAPTURES = "captures";
    private static readonly string MATCH = "match";
    private static readonly string BEGIN_CAPTURES = "beginCaptures";
    private static readonly string CONTENT_NAME = "contentName";
    private static readonly string NAME = "name";
    private static readonly string ID = "id";
    private static readonly string DOLLAR_SELF = "$self";
    private static readonly string DOLLAR_BASE = "$base";
    private List<string> fileTypes;

    public IRawRule GetCapture(string captureId)
    {
        return GetProp(captureId);
    }

    IEnumerator<string> IEnumerable<string>.GetEnumerator()
    {
        return Keys.GetEnumerator();
    }

    public Dictionary<string, IRawRule> GetInjections()
    {
        var result = TryGetObject<Raw>(INJECTIONS);

        if (result == null)
            return null;

        return ConvertToDictionary<IRawRule>(result);
    }

    public string GetInjectionSelector()
    {
        return (string) this[INJECTION_SELECTOR];
    }

    public string GetScopeName()
    {
        return TryGetObject<string>(SCOPE_NAME);
    }

    public ICollection<string> GetFileTypes()
    {
        if (fileTypes == null)
        {
            var list = new List<string>();
            var unparsedFileTypes = TryGetObject<ICollection>(FILE_TYPES);
            if (unparsedFileTypes != null)
                foreach (var o in unparsedFileTypes)
                {
                    var str = o.ToString();
                    // #202
                    if (str.StartsWith("."))
                        str = str.Substring(1);
                    list.Add(str);
                }

            fileTypes = list;
        }

        return fileTypes;
    }

    public string GetFirstLineMatch()
    {
        return TryGetObject<string>(FIRST_LINE_MATCH);
    }

    public IRawGrammar Clone()
    {
        return (IRawGrammar) Clone(this);
    }

    public IRawRepository Merge(params IRawRepository[] sources)
    {
        var target = new Raw();
        foreach (var source in sources)
        {
            var sourceRaw = (Raw) source;
            foreach (var key in sourceRaw.Keys)
                target[key] = sourceRaw[key];
        }

        return target;
    }

    public IRawRule GetProp(string name)
    {
        return TryGetObject<IRawRule>(name);
    }

    public IRawRule GetBase()
    {
        return TryGetObject<IRawRule>(DOLLAR_BASE);
    }

    public void SetBase(IRawRule ruleBase)
    {
        this[DOLLAR_BASE] = ruleBase;
    }

    public IRawRule GetSelf()
    {
        return TryGetObject<IRawRule>(DOLLAR_SELF);
    }

    public void SetSelf(IRawRule self)
    {
        this[DOLLAR_SELF] = self;
    }

    public RuleId GetId()
    {
        return TryGetObject<RuleId>(ID);
    }

    public void SetId(RuleId id)
    {
        this[ID] = id;
    }

    public string GetName()
    {
        return TryGetObject<string>(NAME);
    }

    public void SetName(string name)
    {
        this[NAME] = name;
    }

    public string GetContentName()
    {
        return TryGetObject<string>(CONTENT_NAME);
    }

    public string GetMatch()
    {
        return TryGetObject<string>(MATCH);
    }

    public IRawCaptures GetCaptures()
    {
        UpdateCaptures(CAPTURES);
        return TryGetObject<IRawCaptures>(CAPTURES);
    }

    public string GetBegin()
    {
        return TryGetObject<string>(BEGIN);
    }

    public string GetWhile()
    {
        return TryGetObject<string>(WHILE);
    }

    public string GetInclude()
    {
        return TryGetObject<string>(INCLUDE);
    }

    public void SetInclude(string include)
    {
        this[INCLUDE] = include;
    }

    public IRawCaptures GetBeginCaptures()
    {
        UpdateCaptures(BEGIN_CAPTURES);
        return TryGetObject<IRawCaptures>(BEGIN_CAPTURES);
    }

    public void SetBeginCaptures(IRawCaptures beginCaptures)
    {
        this[BEGIN_CAPTURES] = beginCaptures;
    }

    public string GetEnd()
    {
        return TryGetObject<string>(END);
    }

    public IRawCaptures GetEndCaptures()
    {
        UpdateCaptures(END_CAPTURES);
        return TryGetObject<IRawCaptures>(END_CAPTURES);
    }

    public IRawCaptures GetWhileCaptures()
    {
        UpdateCaptures(WHILE_CAPTURES);
        return TryGetObject<IRawCaptures>(WHILE_CAPTURES);
    }

    public ICollection<IRawRule> GetPatterns()
    {
        var result = TryGetObject<ICollection>(PATTERNS);

        if (result == null)
            return null;

        return result.Cast<IRawRule>().ToList();
    }

    public void SetPatterns(ICollection<IRawRule> patterns)
    {
        this[PATTERNS] = patterns;
    }

    public IRawRepository GetRepository()
    {
        return TryGetObject<IRawRepository>(REPOSITORY);
    }

    public void SetRepository(IRawRepository repository)
    {
        this[REPOSITORY] = repository;
    }

    public bool IsApplyEndPatternLast()
    {
        var applyEndPatternLast = TryGetObject<object>(APPLY_END_PATTERN_LAST);
        if (applyEndPatternLast == null)
            return false;
        if (applyEndPatternLast is bool)
            return (bool) applyEndPatternLast;
        if (applyEndPatternLast is int)
            return (int) applyEndPatternLast == 1;
        return false;
    }

    private void UpdateCaptures(string name)
    {
        var captures = TryGetObject<object>(name);
        if (captures is IList)
        {
            var rawCaptures = new Raw();
            var i = 0;
            foreach (var capture in (IList) captures)
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
        if (value is Raw)
        {
            var rawToClone = (Raw) value;
            var raw = new Raw();

            foreach (var key in rawToClone.Keys)
                raw[key] = Clone(rawToClone[key]);
            return raw;
        }

        if (value is IList)
        {
            var result = new List<object>();
            foreach (var obj in (IList) value)
                result.Add(Clone(obj));
            return result;
        }

        if (value is string)
            return value;

        if (value is int)
            return value;

        if (value is bool)
            return value;
        return value;
    }

    private Dictionary<string, T> ConvertToDictionary<T>(Raw raw)
    {
        var result = new Dictionary<string, T>();

        foreach (var key in raw.Keys)
            result.Add(key, (T) raw[key]);

        return result;
    }

    private T TryGetObject<T>(string key)
    {
        object result;
        if (!TryGetValue(key, out result))
            return default;

        return (T) result;
    }
}
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TextMateSharp.Definitions;

public class LanguageConfiguration
{
    [JsonPropertyName("autoCloseBefore")]
    public string? AutoCloseBefore { get; set; }

    [JsonPropertyName("folding")]
    public Folding? Folding { get; set; }

    [JsonPropertyName("brackets")]
    public List<string>[]? Brackets { get; set; }

    [JsonPropertyName("comments")]
    public Comments? Comments { get; set; }

/*    [JsonPropertyName("autoClosingPairs")]
    public List<AutoPair>? AutoClosingPairs { get; set; }

    [JsonPropertyName("surroundingPairs")]
    public List<SurroundingPair>? SurroundingPairs { get; set; }

    [JsonPropertyName("indentationRules")]
    public Indentation? IndentationRules { get; set; }

    [JsonPropertyName("onEnterRules")]
    public List<EnterRule>? OnEnterRules { get; set; }*/

    public static LanguageConfiguration? LoadFromFile(string configurationFile)
    {
        var fileInfo = new FileInfo(configurationFile);

        if (!fileInfo.Exists)
            return null;

        using var fileStream = fileInfo.OpenRead();

        return JsonSerializer.Deserialize(fileStream, GrammarDefinition.JsonContext.LanguageConfiguration);
    }
}

public class Markers
{
    [JsonPropertyName("start")]
    public string? Start { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }
}

public class Folding
{
    [JsonPropertyName("offSide")]
    public bool OffSide { get; set; }

    [JsonPropertyName("markers")]
    public Markers? Markers { get; set; }
}

public class Comments
{
    [JsonPropertyName("lineComment")]
    public string? LineComment { get; set; }

    [JsonPropertyName("blockComment")]
    public List<string>? BlockComment { get; set; }
}

public class Indentation
{
    [JsonPropertyName("increaseIndentPattern")]
    public string? IncreaseIndentPattern { get; set; }

    [JsonPropertyName("decreaseIndentPattern")]
    public string? DecreaseIndentPattern { get; set; }

    [JsonPropertyName("unIndentedLinePattern")]
    public string? UnindentedLinePattern { get; set; }
}

public class EnterRule
{
    [JsonPropertyName("beforeText")]
    public TextPattern? BeforeText { get; set; }

    [JsonPropertyName("afterText")]
    public TextPattern? AfterText { get; set; }

    [JsonPropertyName("action")]
    public ActionIndent? Action { get; set; }
}

public class ActionIndent
{
    [JsonPropertyName("indent")]
    public string? Indent { get; set; }
}

public class TextPattern
{
    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    [JsonPropertyName("flags")]
    public string? Flags { get; set; }
}

public class AutoPair
{
    [JsonPropertyName("open")]
    public string? Open { get; set; }

    [JsonPropertyName("close")]
    public string? Close { get; set; }

    [JsonPropertyName("notIn")]
    public List<string>? NotIn { get; set; }
}

public class SurroundingPair
{
    [JsonPropertyName("open")]
    public string? Open { get; set; }

    [JsonPropertyName("close")]
    public string? Close { get; set; }
}

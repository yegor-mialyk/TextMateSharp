using System.Text.Json.Serialization;

namespace TextMateSharp.Definitions;

[JsonSerializable(typeof(GrammarDefinition))]
[JsonSerializable(typeof(GrammarNlsPackage))]
[JsonSerializable(typeof(LanguageConfiguration))]
[JsonSerializable(typeof(EnterRule))]
[JsonSerializable(typeof(AutoPair))]
public sealed partial class JsonSerializationContext : JsonSerializerContext;

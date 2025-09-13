using TextMateSharp.Internal.Types;
using TextMateSharp.Themes;

namespace TextMateSharp.Registry;

public interface IRegistryOptions
{
    IRawTheme? LoadRawTheme(string filePath);

    IRawGrammar? LoadRawGrammar(string scopeName);

    ICollection<string>? GetInjections(string scopeName);
}

using TextMateSharp.Internal.Types;

namespace TextMateSharp.Grammars;

public interface IGrammarRepository
{
    IRawGrammar? Lookup(string scopeName);

    ICollection<string>? GetInjections(string targetScope);
}

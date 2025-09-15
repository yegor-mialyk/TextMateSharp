using Onigwrap;

namespace TextMateSharp.Internal.Rules;

public class CompiledRule(OnigScanner scanner, IList<int> rules)
{
    public OnigScanner Scanner { get; } = scanner;

    public IList<int> Rules { get; } = rules;
}

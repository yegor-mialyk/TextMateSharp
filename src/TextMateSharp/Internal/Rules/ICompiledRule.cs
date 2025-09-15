using Onigwrap;

namespace TextMateSharp.Internal.Rules;

public class CompiledRule
{
    public CompiledRule(OnigScanner scanner, IList<int> rules)
    {
        Scanner = scanner;
        Rules = rules;
    }

    public OnigScanner Scanner { get; }
    
    public IList<int> Rules { get; }
}

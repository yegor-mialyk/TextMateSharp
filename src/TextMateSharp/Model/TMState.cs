using TextMateSharp.Grammars;

namespace TextMateSharp.Model;

public class TMState
{
    private readonly TMState? _parentEmbedderState;
    private IStateStack? _ruleStack;

    public TMState(TMState? parentEmbedderState, IStateStack? ruleStack)
    {
        _parentEmbedderState = parentEmbedderState;
        _ruleStack = ruleStack;
    }

    public void SetRuleStack(IStateStack? ruleStack)
    {
        _ruleStack = ruleStack;
    }

    public IStateStack? GetRuleStack()
    {
        return _ruleStack;
    }

    public TMState Clone()
    {
        return new(_parentEmbedderState?.Clone(), _ruleStack);
    }

    public override bool Equals(object? other)
    {
        if (other is not TMState otherState)
            return false;

        return Equals(_parentEmbedderState, otherState._parentEmbedderState) &&
            Equals(_ruleStack, otherState._ruleStack);
    }

    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return (_parentEmbedderState?.GetHashCode() ?? 0) + (_ruleStack?.GetHashCode() ?? 0);
    }
}

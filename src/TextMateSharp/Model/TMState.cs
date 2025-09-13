using TextMateSharp.Grammars;

namespace TextMateSharp.Model;

public class TMState
{
    private readonly TMState _parentEmbedderState;
    private IStateStack _ruleStack;

    public TMState(TMState parentEmbedderState, IStateStack ruleStatck)
    {
        _parentEmbedderState = parentEmbedderState;
        _ruleStack = ruleStatck;
    }

    public void SetRuleStack(IStateStack ruleStack)
    {
        _ruleStack = ruleStack;
    }

    public IStateStack GetRuleStack()
    {
        return _ruleStack;
    }

    public TMState Clone()
    {
        var parentEmbedderStateClone = _parentEmbedderState != null ? _parentEmbedderState.Clone() : null;

        return new(parentEmbedderStateClone, _ruleStack);
    }

    public override bool Equals(object other)
    {
        if (!(other is TMState))
            return false;

        var otherState = (TMState) other;

        return Equals(_parentEmbedderState, otherState._parentEmbedderState) &&
            Equals(_ruleStack, otherState._ruleStack);
    }

    public override int GetHashCode()
    {
        return _parentEmbedderState.GetHashCode() + _ruleStack.GetHashCode();
    }
}
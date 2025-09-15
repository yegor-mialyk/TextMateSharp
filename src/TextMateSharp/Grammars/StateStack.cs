using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Grammars;

public class StateStack
{
    public static readonly StateStack NULL = new(
        null,
        Rule.NO_RULE,
        0,
        0,
        false,
        null,
        null,
        null);

    private int _anchorPos;

    private int _enterPos;

    public StateStack(
        StateStack? parent,
        int ruleId,
        int enterPos,
        int anchorPos,
        bool beginRuleCapturedEOL,
        string? endRule,
        AttributedScopeStack? nameScopesList,
        AttributedScopeStack? contentNameScopesList)
    {
        Parent = parent;
        Depth = Parent != null ? Parent.Depth + 1 : 1;
        RuleId = ruleId;
        BeginRuleCapturedEol = beginRuleCapturedEOL;
        EndRule = endRule;
        NameScopesList = nameScopesList;
        ContentNameScopesList = contentNameScopesList;

        _enterPos = enterPos;
        _anchorPos = anchorPos;
    }

    public StateStack? Parent { get; }
    public AttributedScopeStack? NameScopesList { get; }
    public AttributedScopeStack? ContentNameScopesList { get; }
    public bool BeginRuleCapturedEol { get; }
    public int Depth { get; }
    public int RuleId { get; }
    public string? EndRule { get; }

    private static bool StructuralEquals(StateStack? a, StateStack? b)
    {
        // ReSharper disable once PossibleUnintendedReferenceComparison
        if (a == b)
            return true;

        if (a == null || b == null)
            return false;

        return a.Depth == b.Depth &&
            a.RuleId == b.RuleId &&
            Equals(a.EndRule, b.EndRule) &&
            StructuralEquals(a.Parent, b.Parent);
    }

    public override bool Equals(object? other)
    {
        if (other is not StateStack stackElement)
            return false;

        if (ContentNameScopesList == null)
            return stackElement.ContentNameScopesList == null;

        return StructuralEquals(this, stackElement) && ContentNameScopesList.Equals(stackElement.ContentNameScopesList);
    }

    public override int GetHashCode()
    {
        return Depth.GetHashCode() +
            RuleId.GetHashCode() +
            (EndRule?.GetHashCode() ?? 0) +
            (Parent?.GetHashCode() ?? 0) +
            (ContentNameScopesList?.GetHashCode() ?? 0);
    }

    public void Reset()
    {
        var el = this;
        while (el != null)
        {
            el._enterPos = -1;
            el._anchorPos = -1;
            el = el.Parent;
        }
    }

    public StateStack? Pop()
    {
        return Parent;
    }

    public StateStack SafePop()
    {
        return Parent ?? this;
    }

    public StateStack Push(
        int ruleId,
        int enterPos,
        int anchorPos,
        bool beginRuleCapturedEOL,
        string? endRule,
        AttributedScopeStack? nameScopesList,
        AttributedScopeStack? contentNameScopesList)
    {
        return new(
            this,
            ruleId,
            enterPos,
            anchorPos,
            beginRuleCapturedEOL,
            endRule,
            nameScopesList,
            contentNameScopesList);
    }

    public int GetEnterPos()
    {
        return _enterPos;
    }

    public int GetAnchorPos()
    {
        return _anchorPos;
    }

    public Rule? GetRule(IRuleRegistry grammar)
    {
        return grammar.GetRule(RuleId);
    }

    private void AppendString(List<string> res)
    {
        Parent?.AppendString(res);

        res.Add('(' + RuleId.ToString() + ')'); //TODO-${this.nameScopesList}, TODO-${this.contentNameScopesList})`;
    }

    public override string ToString()
    {
        var r = new List<string>();

        AppendString(r);

        return '[' + string.Join(", ", r) + ']';
    }

    public StateStack? WithContentNameScopesList(AttributedScopeStack? contentNameScopesList)
    {
        if (ContentNameScopesList != null)
        {
            if (ContentNameScopesList.Equals(contentNameScopesList))
                return this;
        }
        else
        {
            if (contentNameScopesList == null)
                return this;
        }

        return Parent?.Push(
            RuleId,
            _enterPos,
            _anchorPos,
            BeginRuleCapturedEol,
            EndRule,
            NameScopesList,
            contentNameScopesList);
    }

    public StateStack WithEndRule(string endRule)
    {
        if (EndRule != null && EndRule.Equals(endRule))
            return this;

        return new(
            Parent,
            RuleId,
            _enterPos,
            _anchorPos,
            BeginRuleCapturedEol,
            endRule,
            NameScopesList,
            ContentNameScopesList);
    }

    public bool HasSameRuleAs(StateStack other)
    {
        return RuleId == other.RuleId;
    }
}

using Onigwrap;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Internal.Matcher;

internal class MatchInjectionsResult : MatchResult
{
    internal MatchInjectionsResult(
        IOnigCaptureIndex[] captureIndexes,
        RuleId matchedRuleId,
        bool isPriorityMatch) : base(captureIndexes, matchedRuleId)
    {
        IsPriorityMatch = isPriorityMatch;
    }

    public bool IsPriorityMatch { get; private set; }
}
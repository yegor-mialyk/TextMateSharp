using Onigwrap;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Internal.Matcher;

public class MatchInjectionsResult(IOnigCaptureIndex[] captureIndexes, int matchedRuleId, bool isPriorityMatch)
    : MatchResult(captureIndexes, matchedRuleId)
{
    public bool IsPriorityMatch { get; private set; } = isPriorityMatch;
}

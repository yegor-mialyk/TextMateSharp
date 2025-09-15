using Onigwrap;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Internal.Matcher;

public class MatchResult(IOnigCaptureIndex[] captureIndexes, int matchedRuleId)
{
    public IOnigCaptureIndex[] CaptureIndexes { get; private set; } = captureIndexes;

    public int MatchedRuleId { get; private set; } = matchedRuleId;
}

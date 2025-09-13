using Onigwrap;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Internal.Matcher;

internal class MatchResult
{
    internal MatchResult(IOnigCaptureIndex[] captureIndexes, int matchedRuleId)
    {
        CaptureIndexes = captureIndexes;
        MatchedRuleId = matchedRuleId;
    }

    public IOnigCaptureIndex[] CaptureIndexes { get; private set; }

    public int MatchedRuleId { get; private set; }
}

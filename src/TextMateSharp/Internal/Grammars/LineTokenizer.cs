using System.Diagnostics;
using Onigwrap;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Matcher;
using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Grammars;

public class LineTokenizer
{
    private readonly Grammar _grammar;
    private readonly int _lineLength;
    private readonly string _lineText;
    private readonly LineTokens _lineTokens;
    private int _anchorPosition = -1;
    private bool _isFirstLine;
    private int _linePos;
    private StateStack _stack;
    private bool _stop;

    public LineTokenizer(Grammar grammar, string lineText, bool isFirstLine, int linePos, StateStack stack,
        LineTokens lineTokens)
    {
        _grammar = grammar;
        _lineText = lineText;
        _lineLength = lineText.Length;
        _isFirstLine = isFirstLine;
        _linePos = linePos;
        _stack = stack;
        _lineTokens = lineTokens;
    }

    public TokenizeStringResult Scan(bool checkWhileConditions, TimeSpan timeLimit)
    {
        _stop = false;

        if (checkWhileConditions)
        {
            var whileCheckResult = CheckWhileConditions(_grammar, _lineText, _isFirstLine, _linePos, _stack,
                _lineTokens);
            _stack = whileCheckResult.Stack;
            _linePos = whileCheckResult.LinePos;
            _isFirstLine = whileCheckResult.IsFirstLine;
            _anchorPosition = whileCheckResult.AnchorPosition;
        }

        var stopWatch = new Stopwatch();
        stopWatch.Start();
        while (!_stop)
        {
            if (stopWatch.Elapsed > timeLimit)
                return new(_stack, true);
            ScanNext(); // potentially modifies linePos && anchorPosition
        }

        return new(_stack, false);
    }

    private void ScanNext()
    {
        var r = MatchRuleOrInjections(_grammar, _lineText, _isFirstLine, _linePos, _stack, _anchorPosition);

        if (r == null)
        {
            // No match
            _lineTokens.Produce(_stack, _lineLength);
            _stop = true;
            return;
        }

        var captureIndices = r.CaptureIndexes;
        var matchedRuleId = r.MatchedRuleId;

        var hasAdvanced = captureIndices.Length > 0 && captureIndices[0].End > _linePos;

        if (matchedRuleId.Equals(Rule.END_RULE))
        {
            // We matched the `end` for this rule => pop it
            var poppedRule = (BeginEndRule) _stack.GetRule(_grammar);

            /*
             * if (logger.isEnabled()) { logger.log("  popping " + poppedRule.debugName +
             * " - " + poppedRule.debugEndRegExp); }
             */

            _lineTokens.Produce(_stack, captureIndices[0].Start);
            _stack = _stack.WithContentNameScopesList(_stack.NameScopesList);
            HandleCaptures(_grammar, _lineText, _isFirstLine, _stack, _lineTokens, poppedRule.EndCaptures,
                captureIndices);
            _lineTokens.Produce(_stack, captureIndices[0].End);

            // pop
            var popped = _stack;
            _stack = _stack.Pop();
            _anchorPosition = popped.GetAnchorPos();

            if (!hasAdvanced && popped.GetEnterPos() == _linePos)
            {
                // Grammar pushed & popped a rule without advancing
                Debug.WriteLine(
                    "[1] - Grammar is in an endless loop - Grammar pushed & popped a rule without advancing");
                // See https://github.com/Microsoft/vscode-textmate/issues/12
                // Let's assume this was a mistake by the grammar author and the
                // intent was to continue in this state
                _stack = popped;

                _lineTokens.Produce(_stack, _lineLength);
                _stop = true;
                return;
            }
        }
        else if (captureIndices is { Length: > 0 })
        {
            // We matched a rule!
            var rule = _grammar.GetRule(matchedRuleId);

            _lineTokens.Produce(_stack, captureIndices[0].Start);

            var beforePush = _stack;
            // push it on the stack rule
            var scopeName = rule.GetName(_lineText, captureIndices);
            var nameScopesList = _stack.ContentNameScopesList.PushAttributed(
                scopeName,
                _grammar);

            _stack = _stack.Push(
                matchedRuleId,
                _linePos,
                _anchorPosition,
                captureIndices[0].End == _lineText.Length,
                null,
                nameScopesList,
                nameScopesList);

            if (rule is BeginEndRule endRule)
            {
                HandleCaptures(
                    _grammar,
                    _lineText,
                    _isFirstLine,
                    _stack,
                    _lineTokens,
                    endRule.BeginCaptures,
                    captureIndices);
                _lineTokens.Produce(_stack, captureIndices[0].End);
                _anchorPosition = captureIndices[0].End;

                var contentName = endRule.GetContentName(
                    _lineText,
                    captureIndices);

                var contentNameScopesList = nameScopesList.PushAttributed(
                    contentName,
                    _grammar);

                _stack = _stack.WithContentNameScopesList(contentNameScopesList);

                if (endRule.EndHasBackReferences)
                    _stack = _stack.WithEndRule(
                        endRule.GetEndWithResolvedBackReferences(
                            _lineText,
                            captureIndices));

                if (!hasAdvanced && beforePush.HasSameRuleAs(_stack))
                {
                    // Grammar pushed the same rule without advancing
                    Debug.WriteLine(
                        "[2] - Grammar is in an endless loop - Grammar pushed the same rule without advancing");
                    _stack = _stack.Pop();
                    _lineTokens.Produce(_stack, _lineLength);
                    _stop = true;
                    return;
                }
            }
            else if (rule is BeginWhileRule pushedRule)
            {
                // if (IN_DEBUG_MODE) {
                // console.log(' pushing ' + pushedRule.debugName);
                // }

                HandleCaptures(
                    _grammar,
                    _lineText,
                    _isFirstLine,
                    _stack,
                    _lineTokens,
                    pushedRule.BeginCaptures,
                    captureIndices);
                _lineTokens.Produce(_stack, captureIndices[0].End);
                _anchorPosition = captureIndices[0].End;

                var contentName = pushedRule.GetContentName(_lineText, captureIndices);
                var contentNameScopesList = nameScopesList.PushAttributed(contentName, _grammar);
                _stack = _stack.WithContentNameScopesList(contentNameScopesList);

                if (pushedRule.WhileHasBackReferences)
                    _stack = _stack.WithEndRule(
                        pushedRule.getWhileWithResolvedBackReferences(_lineText, captureIndices));

                if (!hasAdvanced && beforePush.HasSameRuleAs(_stack))
                {
                    // Grammar pushed the same rule without advancing
                    Debug.WriteLine(
                        "[3] - Grammar is in an endless loop - Grammar pushed the same rule without advancing");
                    _stack = _stack.Pop();
                    _lineTokens.Produce(_stack, _lineLength);
                    _stop = true;
                    return;
                }
            }
            else
            {
                var matchingRule = (MatchRule) rule;
                // if (IN_DEBUG_MODE) {
                // console.log(' matched ' + matchingRule.debugName + ' - ' +
                // matchingRule.debugMatchRegExp);
                // }

                HandleCaptures(_grammar, _lineText, _isFirstLine, _stack, _lineTokens, matchingRule.Captures,
                    captureIndices);
                _lineTokens.Produce(_stack, captureIndices[0].End);

                // pop rule immediately since it is a MatchRule
                _stack = _stack.Pop();

                if (!hasAdvanced)
                {
                    // Grammar is not advancing, nor is it pushing/popping
                    Debug.WriteLine(
                        "[4] - Grammar is in an endless loop - Grammar is not advancing, nor is it pushing/popping");
                    _stack = _stack.SafePop();
                    _lineTokens.Produce(_stack, _lineLength);
                    _stop = true;
                    return;
                }
            }
        }

        if (captureIndices is { Length: > 0 } && captureIndices[0].End > _linePos)
        {
            // Advance stream
            _linePos = captureIndices[0].End;
            _isFirstLine = false;
        }
    }

    private static MatchResult? MatchRule(Grammar grammar, string lineText, in bool isFirstLine, in int linePos,
        StateStack stack, in int anchorPosition)
    {
        var rule = stack.GetRule(grammar);

        var ruleScanner = rule?.Compile(grammar, stack.EndRule, isFirstLine, linePos == anchorPosition);

        var r = ruleScanner?.Scanner.FindNextMatchSync(lineText, linePos);

        return r != null
            ? new(
                r.GetCaptureIndices(),
                ruleScanner!.Rules[r.GetIndex()])
            : null;
    }

    private static MatchResult? MatchRuleOrInjections(Grammar grammar, string lineText, bool isFirstLine,
        in int linePos, StateStack stack, in int anchorPosition)
    {
        // Look for normal grammar rule
        var matchResult = MatchRule(grammar, lineText, isFirstLine, linePos, stack, anchorPosition);

        // Look for injected rules
        var injections = grammar.GetInjections();
        if (injections.Count == 0)
            // No injections whatsoever => early return
            return matchResult;

        var injectionResult = MatchInjections(injections, grammar, lineText, isFirstLine, linePos,
            stack, anchorPosition);
        if (injectionResult == null)
            // No injections matched => early return
            return matchResult;

        if (matchResult == null)
            // Only injections matched => early return
            return injectionResult;

        // Decide if `matchResult` or `injectionResult` should win
        var matchResultScore = matchResult.CaptureIndexes[0].Start;
        var injectionResultScore = injectionResult.CaptureIndexes[0].Start;

        if (injectionResultScore < matchResultScore ||
            (injectionResult.IsPriorityMatch && injectionResultScore == matchResultScore))
            // injection won!
            return injectionResult;

        return matchResult;
    }

    private static MatchInjectionsResult? MatchInjections(List<Injection> injections, Grammar grammar, string lineText,
        bool isFirstLine, in int linePos, StateStack stack, in int anchorPosition)
    {
        // The lower the better
        var bestMatchRating = int.MaxValue;
        IOnigCaptureIndex[] bestMatchCaptureIndices = null;
        var bestMatchRuleId = Rule.NO_INIT;
        var bestMatchResultPriority = 0;

        var scopes = stack.ContentNameScopesList.GetScopeNames();

        foreach (var injection in injections)
        {
            if (!injection.Match(scopes))
                // injection selector doesn't match stack
                continue;

            var ruleScanner = grammar.GetRule(injection.RuleId).Compile(grammar, null, isFirstLine,
                linePos == anchorPosition);
            var matchResult = ruleScanner.Scanner.FindNextMatchSync(lineText, linePos);

            if (matchResult == null)
                continue;

            var matchRating = matchResult.GetCaptureIndices()[0].Start;

            if (matchRating > bestMatchRating)
                // Injections are sorted by priority, so the previous injection had a better or
                // equal priority
                continue;

            bestMatchRating = matchRating;
            bestMatchCaptureIndices = matchResult.GetCaptureIndices();
            bestMatchRuleId = ruleScanner.Rules[matchResult.GetIndex()];
            bestMatchResultPriority = injection.Priority;

            if (bestMatchRating == linePos)
                // No more need to look at the rest of the injections
                break;
        }

        if (bestMatchCaptureIndices != null)
        {
            var matchedRuleId = bestMatchRuleId;
            var matchCaptureIndices = bestMatchCaptureIndices;
            var isPriorityMatch = bestMatchResultPriority == -1;

            return new(
                matchCaptureIndices,
                matchedRuleId,
                isPriorityMatch);
        }

        return null;
    }

    private static void HandleCaptures(Grammar grammar, string lineText, bool isFirstLine, StateStack stack,
        LineTokens lineTokens, List<CaptureRule> captures, IOnigCaptureIndex[] captureIndices)
    {
        if (captures.Count == 0)
            return;

        var len = Math.Min(captures.Count, captureIndices.Length);
        var localStack = new List<LocalStackElement>();
        var maxEnd = captureIndices[0].End;

        for (var i = 0; i < len; i++)
        {
            var captureRule = captures[i];
            if (captureRule == null)
                // Not interested
                continue;

            var captureIndex = captureIndices[i];

            if (captureIndex.Length == 0)
                // Nothing really captured
                continue;

            if (captureIndex.Start > maxEnd)
                // Capture going beyond consumed string
                break;

            // pop captures while needed
            while (localStack.Count > 0 && localStack[^1].EndPos <= captureIndex.Start)
            {
                // pop!
                lineTokens.ProduceFromScopes(localStack[^1].Scopes,
                    localStack[^1].EndPos);
                localStack.RemoveAt(localStack.Count - 1);
            }

            if (localStack.Count > 0)
                lineTokens.ProduceFromScopes(localStack[^1].Scopes,
                    captureIndex.Start);
            else
                lineTokens.Produce(stack, captureIndex.Start);

            if (captureRule.RetokenizeCapturedWithRuleId != Rule.NO_INIT)
            {
                // the capture requires additional matching
                var scopeName = captureRule.GetName(lineText, captureIndices);
                var nameScopesList = stack.ContentNameScopesList.PushAttributed(scopeName, grammar);
                var contentName = captureRule.GetContentName(lineText, captureIndices);
                var contentNameScopesList = nameScopesList.PushAttributed(contentName, grammar);

                // the capture requires additional matching
                var stackClone = stack.Push(
                    captureRule.RetokenizeCapturedWithRuleId,
                    captureIndex.Start,
                    -1,
                    false,
                    null,
                    nameScopesList,
                    contentNameScopesList);

                TokenizeString(grammar,
                    lineText.SubstringAtIndexes(0, captureIndex.End),
                    isFirstLine && captureIndex.Start == 0, captureIndex.Start, stackClone, lineTokens, false,
                    TimeSpan.MaxValue);
                continue;
            }

            // push
            var captureRuleScopeName = captureRule.GetName(lineText, captureIndices);
            if (captureRuleScopeName != null)
            {
                // push
                var baseElement = localStack.Count == 0
                    ? stack.ContentNameScopesList
                    : localStack[^1].Scopes;
                var captureRuleScopesList = baseElement.PushAttributed(captureRuleScopeName, grammar);
                localStack.Add(new(captureRuleScopesList, captureIndex.End));
            }
        }

        while (localStack.Count > 0)
        {
            // pop!
            lineTokens.ProduceFromScopes(localStack[^1].Scopes, localStack[^1].EndPos);
            localStack.RemoveAt(localStack.Count - 1);
        }
    }

    /**
     * * Walk the stack from bottom to top, and check each while condition in this
     * * order. If any fails, cut off the entire stack above the failed while
     * * condition. While conditions may also advance the linePosition.
     */
    private static WhileCheckResult CheckWhileConditions(Grammar grammar, string lineText, bool isFirstLine,
        int linePos, StateStack stack, LineTokens lineTokens)
    {
        var anchorPosition = stack.BeginRuleCapturedEol ? 0 : -1;
        var whileRules = new List<WhileStack>();
        for (var node = stack; node != null; node = node.Pop())
        {
            var nodeRule = node.GetRule(grammar);
            if (nodeRule is BeginWhileRule rule)
                whileRules.Add(new(node, rule));
        }

        for (var i = whileRules.Count - 1; i >= 0; i--)
        {
            var whileRule = whileRules[i];
            var ruleScanner = whileRule.Rule.CompileWhile(whileRule.Stack.EndRule, isFirstLine,
                anchorPosition == linePos);
            var r = ruleScanner?.Scanner.FindNextMatchSync(lineText, linePos);

            if (r != null)
            {
                var matchedRuleId = ruleScanner!.Rules[r.GetIndex()];
                if (matchedRuleId != Rule.WHILE_RULE)
                {
                    // we shouldn't end up here
                    stack = whileRule.Stack.Pop();
                    break;
                }

                if (r.GetCaptureIndices() != null && r.GetCaptureIndices().Length > 0)
                {
                    lineTokens.Produce(whileRule.Stack, r.GetCaptureIndices()[0].Start);
                    HandleCaptures(grammar, lineText, isFirstLine, whileRule.Stack, lineTokens,
                        whileRule.Rule.WhileCaptures, r.GetCaptureIndices());
                    lineTokens.Produce(whileRule.Stack, r.GetCaptureIndices()[0].End);
                    anchorPosition = r.GetCaptureIndices()[0].End;
                    if (r.GetCaptureIndices()[0].End > linePos)
                    {
                        linePos = r.GetCaptureIndices()[0].End;
                        isFirstLine = false;
                    }
                }
            }
            else
            {
                stack = whileRule.Stack.Pop();
                break;
            }
        }

        return new(stack, linePos, anchorPosition, isFirstLine);
    }

    public static TokenizeStringResult TokenizeString(Grammar grammar, string lineText, bool isFirstLine, int linePos,
        StateStack stack, LineTokens lineTokens, bool checkWhileConditions, TimeSpan timeLimit)
    {
        return new LineTokenizer(grammar, lineText, isFirstLine, linePos, stack, lineTokens).Scan(checkWhileConditions,
            timeLimit);
    }

    private class WhileStack(StateStack stack, BeginWhileRule rule)
    {
        public StateStack Stack { get; } = stack;
        public BeginWhileRule Rule { get; } = rule;
    }

    private class WhileCheckResult(StateStack stack, int linePos, int anchorPosition, bool isFirstLine)
    {
        public StateStack Stack { get; } = stack;
        public int LinePos { get; } = linePos;
        public int AnchorPosition { get; } = anchorPosition;
        public bool IsFirstLine { get; } = isFirstLine;
    }

    private class LocalStackElement(AttributedScopeStack scopes, int endPos)
    {
        public AttributedScopeStack Scopes { get; private set; } = scopes;

        public int EndPos { get; private set; } = endPos;
    }
}

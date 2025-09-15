using TextMateSharp.Internal.Types;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Rules;

public static class RuleFactory
{
    private static CaptureRule CreateCaptureRule(IRuleFactoryHelper helper, string? name, string? contentName,
        int retokenizeCapturedWithRuleId)
    {
        return (CaptureRule) helper.RegisterRule(id =>
            new CaptureRule(id, name, contentName, retokenizeCapturedWithRuleId));
    }

    public static int GetCompiledRuleId(IRawRule? desc, IRuleFactoryHelper helper, IRawRepository? repository)
    {
        if (desc == null)
            return Rule.NO_INIT;

        if (desc.GetId() == Rule.NO_INIT)
            helper.RegisterRule(id =>
            {
                desc.SetId(id);

                if (desc.GetMatch() != null)
                    return new MatchRule(desc.GetId(), desc.GetName(), desc.GetMatch(),
                        CompileCaptures(desc.GetCaptures(), helper, repository));

                if (desc.GetBegin() == null)
                {
                    var r = repository;

                    if (desc.GetRepository() != null)
                        r = repository?.Merge(desc.GetRepository());

                    return new IncludeOnlyRule(desc.GetId(), desc.GetName(), desc.GetContentName(),
                        CompilePatterns(desc.GetPatterns(), helper, r));
                }

                var ruleWhile = desc.GetWhile();

                return ruleWhile != null
                    ? new BeginWhileRule(
                        desc.GetId(), desc.GetName(), desc.GetContentName(), desc.GetBegin(),
                        CompileCaptures(desc.GetBeginCaptures() != null ? desc.GetBeginCaptures() : desc.GetCaptures(),
                            helper, repository),
                        ruleWhile,
                        CompileCaptures(desc.GetWhileCaptures() != null ? desc.GetWhileCaptures() : desc.GetCaptures(),
                            helper, repository),
                        CompilePatterns(desc.GetPatterns(), helper, repository))
                    : new BeginEndRule(desc.GetId(), desc.GetName(), desc.GetContentName(), desc.GetBegin(),
                        CompileCaptures(desc.GetBeginCaptures() != null ? desc.GetBeginCaptures() : desc.GetCaptures(),
                            helper, repository),
                        desc.GetEnd(),
                        CompileCaptures(desc.GetEndCaptures() != null ? desc.GetEndCaptures() : desc.GetCaptures(),
                            helper,
                            repository),
                        desc.IsApplyEndPatternLast(),
                        CompilePatterns(desc.GetPatterns(), helper, repository));
            });

        return desc.GetId();
    }

    private static List<CaptureRule> CompileCaptures(IRawCaptures? captures, IRuleFactoryHelper helper,
        IRawRepository? repository)
    {
        if (captures == null)
            return [];

        var r = new List<CaptureRule>();

        // Find the maximum capture id
        var maximumCaptureId = 0;

        foreach (var captureId in captures)
        {
            var numericCaptureId = ParseInt(captureId);

            if (numericCaptureId > maximumCaptureId)
                maximumCaptureId = numericCaptureId;
        }

        // Initialize result
        for (var i = 0; i <= maximumCaptureId; i++)
            r.Add(null!); //TODO !!!

        // Fill out result
        foreach (var captureId in captures)
        {
            var numericCaptureId = ParseInt(captureId);

            var retokenizeCapturedWithRuleId = Rule.NO_INIT;

            var rule = captures.GetCapture(captureId);
            if (rule?.GetPatterns() != null)
                retokenizeCapturedWithRuleId = GetCompiledRuleId(captures.GetCapture(captureId), helper, repository);

            r[numericCaptureId] = CreateCaptureRule(
                helper, rule?.GetName(), rule?.GetContentName(),
                retokenizeCapturedWithRuleId);
        }

        return r;
    }

    private static int ParseInt(string str)
    {
        int.TryParse(str, out var result);
        return result;
    }

    private static CompilePatternsResult CompilePatterns(ICollection<IRawRule>? patterns, IRuleFactoryHelper helper,
        IRawRepository? repository)
    {
        var r = new List<int>();

        if (patterns != null)
            foreach (var pattern in patterns)
            {
                var patternId = Rule.NO_INIT;

                var include = pattern.GetInclude();

                if (include != null)
                {
                    if (include[0] == '#')
                    {
                        // Local include found in `repository`
                        var localIncludedRule = repository?.GetProp(include.Substring(1));
                        if (localIncludedRule != null)
                            patternId = GetCompiledRuleId(localIncludedRule, helper, repository);
                        // console.warn('CANNOT find rule for scopeName: ' +
                        // pattern.include + ', I am: ',
                        // repository['$base'].name);
                    }
                    else if (include.Equals("$base") || include.Equals("$self"))
                    {
                        // Special include also found in `repository`
                        patternId = GetCompiledRuleId(repository?.GetProp(include), helper,
                            repository);
                    }
                    else
                    {
                        string? externalGrammarName;
                        string? externalGrammarInclude = null;

                        var sharpIndex = include.IndexOf('#');
                        if (sharpIndex >= 0)
                        {
                            externalGrammarName = include.SubstringAtIndexes(0, sharpIndex);
                            externalGrammarInclude = include.Substring(sharpIndex + 1);
                        }
                        else
                        {
                            externalGrammarName = include;
                        }

                        // External include
                        var externalGrammar = helper.GetExternalGrammar(externalGrammarName, repository);

                        if (externalGrammar != null)
                        {
                            if (externalGrammarInclude != null)
                            {
                                var externalIncludedRule = externalGrammar.GetRepository()?
                                    .GetProp(externalGrammarInclude);
                                if (externalIncludedRule != null)
                                    patternId = GetCompiledRuleId(externalIncludedRule, helper,
                                        externalGrammar.GetRepository());
                                // console.warn('CANNOT find rule for
                                // scopeName: ' + pattern.include + ', I am:
                                // ', repository['$base'].name);
                            }
                            else
                            {
                                patternId = GetCompiledRuleId(externalGrammar.GetRepository()?.GetSelf(),
                                    helper, externalGrammar.GetRepository());
                            }
                        }
                        // console.warn('CANNOT find grammar for scopeName:
                        // ' + pattern.include + ', I am: ',
                        // repository['$base'].name);
                    }
                }
                else
                {
                    patternId = GetCompiledRuleId(pattern, helper, repository);
                }

                if (patternId != Rule.NO_INIT)
                {
                    var rule = helper.GetRule(patternId);

                    var skipRule = false;

                    if (rule is IncludeOnlyRule ior)
                    {
                        if (ior is { HasMissingPatterns: true, Patterns.Count: 0 })
                            skipRule = true;
                    }
                    else if (rule is BeginEndRule ber)
                    {
                        if (ber is { HasMissingPatterns: true, Patterns.Count: 0 })
                            skipRule = true;
                    }
                    else if (rule is BeginWhileRule bwr)
                    {
                        if (bwr is { HasMissingPatterns: true, Patterns.Count: 0 })
                            skipRule = true;
                    }

                    if (skipRule)
                        // console.log('REMOVING RULE ENTIRELY DUE TO EMPTY
                        // PATTERNS THAT ARE MISSING');
                        continue;

                    r.Add(patternId);
                }
            }

        return new(r, (patterns?.Count ?? 0) != r.Count);
    }
}

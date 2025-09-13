using Onigwrap;

namespace TextMateSharp.Internal.Rules;

public class RegExpSourceList
{
    private readonly RegExpSourceListAnchorCache _anchorCache;

    private readonly List<RegExpSource> _items;
    private CompiledRule _cached;
    private bool _hasAnchors;

    public RegExpSourceList()
    {
        _items = new();
        _hasAnchors = false;
        _cached = null;
        _anchorCache = new();
    }

    public void Push(RegExpSource item)
    {
        _items.Add(item);
        _hasAnchors = _hasAnchors ? _hasAnchors : item.HasAnchor();
    }

    public void UnShift(RegExpSource item)
    {
        _items.Insert(0, item);
        _hasAnchors = _hasAnchors ? _hasAnchors : item.HasAnchor();
    }

    public int Length()
    {
        return _items.Count;
    }

    public void SetSource(int index, string newSource)
    {
        var r = _items[index];
        if (!r.GetSource().Equals(newSource))
        {
            // bust the cache
            _cached = null;
            _anchorCache.A0_G0 = null;
            _anchorCache.A0_G1 = null;
            _anchorCache.A1_G0 = null;
            _anchorCache.A1_G1 = null;

            r.SetSource(newSource);
        }
    }

    public CompiledRule Compile(bool allowA, bool allowG)
    {
        if (!_hasAnchors)
        {
            if (_cached == null)
            {
                var regexps = new List<string>();
                foreach (var regExpSource in _items)
                    regexps.Add(regExpSource.GetSource());
                _cached = new(CreateOnigScanner(regexps.ToArray()), GetRules());
            }

            return _cached;
        }

        if (_anchorCache.A0_G0 == null)
            _anchorCache.A0_G0 = !allowA && !allowG
                ? ResolveAnchors(allowA, allowG)
                : null;
        if (_anchorCache.A0_G1 == null)
            _anchorCache.A0_G1 = !allowA && allowG
                ? ResolveAnchors(allowA, allowG)
                : null;
        if (_anchorCache.A1_G0 == null)
            _anchorCache.A1_G0 = allowA && !allowG
                ? ResolveAnchors(allowA, allowG)
                : null;
        if (_anchorCache.A1_G1 == null)
            _anchorCache.A1_G1 = allowA && allowG
                ? ResolveAnchors(allowA, allowG)
                : null;
        if (allowA)
        {
            if (allowG)
                return _anchorCache.A1_G1;

            return _anchorCache.A1_G0;
        }

        if (allowG)
            return _anchorCache.A0_G1;

        return _anchorCache.A0_G0;
    }

    private CompiledRule ResolveAnchors(bool allowA, bool allowG)
    {
        var regexps = new List<string>();
        foreach (var regExpSource in _items)
            regexps.Add(regExpSource.ResolveAnchors(allowA, allowG));
        return new(CreateOnigScanner(regexps.ToArray()), GetRules());
    }

    private OnigScanner CreateOnigScanner(string[] regexps)
    {
        return new(regexps);
    }

    private IList<RuleId> GetRules()
    {
        var ruleIds = new List<RuleId>();
        foreach (var item in _items)
            ruleIds.Add(item.GetRuleId());
        return ruleIds.ToArray();
    }

    private class RegExpSourceListAnchorCache
    {
        public CompiledRule A0_G0;
        public CompiledRule A0_G1;
        public CompiledRule A1_G0;
        public CompiledRule A1_G1;
    }
}
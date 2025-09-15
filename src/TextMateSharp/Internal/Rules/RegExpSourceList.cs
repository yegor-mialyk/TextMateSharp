namespace TextMateSharp.Internal.Rules;

public class RegExpSourceList
{
    private readonly RegExpSourceListAnchorCache _anchorCache = new();

    private readonly List<RegExpSource> _items = [];
    private CompiledRule? _cached;
    private bool _hasAnchors;

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

        if (newSource.Equals(r.GetSource()))
            return;

        // bust the cache
        _cached = null;
        _anchorCache.A0_G0 = null;
        _anchorCache.A0_G1 = null;
        _anchorCache.A1_G0 = null;
        _anchorCache.A1_G1 = null;

        r.SetSource(newSource);
    }

    public CompiledRule? Compile(bool allowA, bool allowG)
    {
        if (!_hasAnchors)
        {
            if (_cached == null)
            {
                var regexps = _items.Select(regExpSource => regExpSource.GetSource()).ToArray();
                _cached = new(new(regexps), GetRules());
            }

            return _cached;
        }

        _anchorCache.A0_G0 ??= !allowA && !allowG
            ? ResolveAnchors(allowA, allowG)
            : null;
        _anchorCache.A0_G1 ??= !allowA && allowG
            ? ResolveAnchors(allowA, allowG)
            : null;
        _anchorCache.A1_G0 ??= allowA && !allowG
            ? ResolveAnchors(allowA, allowG)
            : null;
        _anchorCache.A1_G1 ??= allowA && allowG
            ? ResolveAnchors(allowA, allowG)
            : null;

        if (allowA)
            return allowG ? _anchorCache.A1_G1 : _anchorCache.A1_G0;

        return allowG ? _anchorCache.A0_G1 : _anchorCache.A0_G0;
    }

    private CompiledRule ResolveAnchors(bool allowA, bool allowG)
    {
        var regexps = _items.Select(regExpSource => regExpSource.ResolveAnchors(allowA, allowG)).ToArray();

        return new(new(regexps), GetRules());
    }

    private IList<int> GetRules()
    {
        return _items.Select(regExpSource => regExpSource.GetRuleId()).ToList();
    }

    private class RegExpSourceListAnchorCache
    {
        public CompiledRule? A0_G0;
        public CompiledRule? A0_G1;
        public CompiledRule? A1_G0;
        public CompiledRule? A1_G1;
    }
}

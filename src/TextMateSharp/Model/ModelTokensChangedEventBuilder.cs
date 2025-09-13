namespace TextMateSharp.Model;

internal class ModelTokensChangedEventBuilder
{
    private readonly ITMModel _model;
    private readonly List<Range> _ranges;

    public ModelTokensChangedEventBuilder(ITMModel model)
    {
        _model = model;
        _ranges = new();
    }

    public void registerChangedTokens(int lineNumber)
    {
        var previousRange = _ranges.Count == 0 ? null : _ranges[_ranges.Count - 1];

        if (previousRange != null && previousRange.ToLineNumber == lineNumber - 1)
            // extend previous range
            previousRange.ToLineNumber++;
        else
            // insert new range
            _ranges.Add(new(lineNumber));
    }

    public ModelTokensChangedEvent Build()
    {
        if (_ranges.Count == 0)
            return null;
        return new(_ranges, _model);
    }
}
namespace TextMateSharp.Model;

public class ModelTokensChangedEventBuilder
{
    private readonly ITMModel _model;
    private readonly List<Range> _ranges = [];

    public ModelTokensChangedEventBuilder(ITMModel model)
    {
        _model = model;
    }

    public void RegisterChangedTokens(int lineNumber)
    {
        var previousRange = _ranges.Count == 0 ? null : _ranges[^1];

        if (previousRange != null && previousRange.ToLineNumber == lineNumber - 1)
            // extend previous range
            previousRange.ToLineNumber++;
        else
            // insert new range
            _ranges.Add(new(lineNumber));
    }

    public ModelTokensChangedEvent? Build()
    {
        return _ranges.Count == 0 ? null : new(_ranges, _model);
    }
}

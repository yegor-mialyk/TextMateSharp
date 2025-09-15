namespace TextMateSharp.Model;

public class ModelTokensChangedEvent
{
    public ModelTokensChangedEvent(Range range, ITMModel model) :
        this([range], model)
    {
    }

    public ModelTokensChangedEvent(List<Range> ranges, ITMModel model)
    {
        Ranges = ranges;
        Model = model;
    }

    public List<Range> Ranges { get; }

    public ITMModel Model { get; }
}

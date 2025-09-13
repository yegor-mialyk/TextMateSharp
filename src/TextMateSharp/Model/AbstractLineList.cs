namespace TextMateSharp.Model;

public abstract class AbstractLineList : IModelLines
{
    private readonly List<ModelLine> _list = [];
    private readonly Lock _lock = new();

    private TMModel? _model;

    public void AddLine(int line)
    {
        lock (_lock)
        {
            _list.Insert(line, new());
        }
    }

    public void RemoveLine(int line)
    {
        lock (_lock)
        {
            _list.RemoveAt(line);
        }
    }

    public ModelLine? Get(int index)
    {
        lock (_lock)
        {
            if (index < 0 || index >= _list.Count)
                return null;

            return _list[index];
        }
    }

    public void ForEach(Action<ModelLine> action)
    {
        lock (_lock)
        {
            foreach (var modelLine in _list)
                action(modelLine);
        }
    }

    public int GetSize()
    {
        return GetNumberOfLines();
    }

    public abstract void UpdateLine(int lineIndex);

    public abstract int GetNumberOfLines();

    public abstract string? GetLineText(int lineIndex);

    public abstract int GetLineLength(int lineIndex);

    public abstract void Dispose();

    public void SetModel(TMModel model)
    {
        _model = model;

        lock (_lock)
        {
            foreach (var line in _list)
                line.IsInvalid = true;
        }
    }

    protected void InvalidateLine(int lineIndex)
    {
        _model?.InvalidateLine(lineIndex);
    }

    protected void InvalidateLineRange(int iniLineIndex, int endLineIndex)
    {
        _model?.InvalidateLineRange(iniLineIndex, endLineIndex);
    }

    protected void ForceTokenization(int startLineIndex, int endLineIndex)
    {
        _model?.ForceTokenization(startLineIndex, endLineIndex);
    }
}

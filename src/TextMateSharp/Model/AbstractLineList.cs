namespace TextMateSharp.Model;

public abstract class AbstractLineList : IModelLines
{
    private readonly IList<ModelLine> _list = new List<ModelLine>();

    private readonly object mLock = new();

    private TMModel _model;

    public void AddLine(int line)
    {
        lock (mLock)
        {
            _list.Insert(line, new());
        }
    }

    public void RemoveLine(int line)
    {
        lock (mLock)
        {
            _list.RemoveAt(line);
        }
    }

    public ModelLine Get(int index)
    {
        lock (mLock)
        {
            if (index < 0 || index >= _list.Count)
                return null;

            return _list[index];
        }
    }

    public void ForEach(Action<ModelLine> action)
    {
        lock (mLock)
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

    public abstract string GetLineText(int lineIndex);

    public abstract int GetLineLength(int lineIndex);

    public abstract void Dispose();

    public void SetModel(TMModel model)
    {
        _model = model;
        lock (mLock)
        {
            foreach (var line in _list)
                line.IsInvalid = true;
        }
    }

    protected void InvalidateLine(int lineIndex)
    {
        if (_model != null)
            _model.InvalidateLine(lineIndex);
    }

    protected void InvalidateLineRange(int iniLineIndex, int endLineIndex)
    {
        if (_model != null)
            _model.InvalidateLineRange(iniLineIndex, endLineIndex);
    }

    protected void ForceTokenization(int startLineIndex, int endLineIndex)
    {
        if (_model != null)
            _model.ForceTokenization(startLineIndex, endLineIndex);
    }
}
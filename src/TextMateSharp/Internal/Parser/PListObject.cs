namespace TextMateSharp.Internal.Parser;

public abstract class PListObject
{
    private readonly List<object>? _arrayValues;
    private readonly Dictionary<string, object>? _mapValues;

    private string? _lastKey;

    public readonly PListObject parent;

    protected PListObject(PListObject parent, bool valueAsArray)
    {
        this.parent = parent;

        if (valueAsArray)
        {
            _arrayValues = [];
            _mapValues = null;
        }
        else
        {
            _arrayValues = null;
            _mapValues = CreateRaw();
        }
    }

    public string? GetLastKey()
    {
        return _lastKey;
    }

    public void SetLastKey(string? lastKey)
    {
        _lastKey = lastKey;
    }

    public void AddValue(object value)
    {
        if (IsValueAsArray())
            _arrayValues!.Add(value);
        else
            _mapValues![GetLastKey() ?? throw new InvalidOperationException("GetLastKey() => null")] = value;
    }

    public bool IsValueAsArray()
    {
        return _arrayValues is not null;
    }

    public object GetValue()
    {
        if (IsValueAsArray())
            return _arrayValues!;
        return _mapValues!;
    }

    protected abstract Dictionary<string, object> CreateRaw();
}

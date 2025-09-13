namespace TextMateSharp.Themes;

public class ColorMap
{
    private readonly Dictionary<string, int> _colorToId = new(StringComparer.OrdinalIgnoreCase);
    private int _lastColorId;

    public int GetId(string? color)
    {
        if (color == null)
            return 0;

        if (_colorToId.TryGetValue(color, out var value))
            return value;

        value = ++_lastColorId;
        _colorToId[color] = value;
        return value;
    }

    public string? GetColor(int id)
    {
        return _colorToId.Keys.FirstOrDefault(color => _colorToId[color] == id);
    }

    public ICollection<string> GetColorMap()
    {
        return _colorToId.Keys;
    }
}

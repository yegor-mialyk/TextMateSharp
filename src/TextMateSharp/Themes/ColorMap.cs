namespace TextMateSharp.Themes;

public class ColorMap
{
    private readonly Dictionary<string /* color */, int? /* ID color */> _color2id;

    private int _lastColorId;

    public ColorMap()
    {
        _lastColorId = 0;
        _color2id = new();
    }

    public int GetId(string color)
    {
        if (color == null)
            return 0;
        color = color.ToUpper();
        int? value;
        _color2id.TryGetValue(color, out value);
        if (value != null)
            return value.Value;
        value = ++_lastColorId;
        _color2id[color] = value;
        return value.Value;
    }

    public string GetColor(int id)
    {
        foreach (var color in _color2id.Keys)
            if (_color2id[color].Value == id)
                return color;

        return null;
    }

    public ICollection<string> GetColorMap()
    {
        return _color2id.Keys;
    }

    public override int GetHashCode()
    {
        return _color2id.GetHashCode() + _lastColorId.GetHashCode();
    }

    public bool equals(object obj)
    {
        if (this == obj)
            return true;
        if (obj == null)
            return false;
        if (GetType() != obj.GetType())
            return false;
        var other = (ColorMap) obj;
        return Equals(_color2id, other._color2id) && _lastColorId == other._lastColorId;
    }
}
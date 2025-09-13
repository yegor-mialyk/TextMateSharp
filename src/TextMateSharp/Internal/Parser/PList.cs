using System.Text;
using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Internal.Themes;

namespace TextMateSharp.Internal.Parser;

public class PList<T>
{
    private readonly bool _theme;
    private PListObject? _currObject;
    private T _result;
    private StringBuilder? _text;

    public PList(bool theme)
    {
        _theme = theme;
    }

    public void StartElement(string tagName)
    {
        if ("dict".Equals(tagName))
            _currObject = Create(_currObject, false);
        else if ("array".Equals(tagName))
            _currObject = Create(_currObject, true);
        else if ("key".Equals(tagName))
            _currObject?.SetLastKey(null);

        _text ??= new();
        _text.Clear();
    }

    private PListObject Create(PListObject parent, bool valueAsArray)
    {
        if (_theme)
            return new PListTheme(parent, valueAsArray);
        return new PListGrammar(parent, valueAsArray);
    }

    public void EndElement(string tagName)
    {
        object? value = null;
        var s = _text?.ToString();
        if ("key".Equals(tagName))
        {
            if (_currObject == null || _currObject.IsValueAsArray())
                return;

            _currObject.SetLastKey(s);
            return;
        }

        if ("dict".Equals(tagName) || "array".Equals(tagName))
        {
            if (_currObject == null)
                return;

            value = _currObject.GetValue();
            _currObject = _currObject.parent;
        }
        else if ("string".Equals(tagName) || "data".Equals(tagName))
        {
            value = s;
        }
        else if ("date".Equals(tagName))
        {
            // TODO : parse date
        }
        else if ("integer".Equals(tagName))
        {
            try
            {
                value = int.Parse(s);
            }
            catch (Exception)
            {
                return;
            }
        }
        else if ("real".Equals(tagName))
        {
            try
            {
                value = float.Parse(s);
            }
            catch (Exception)
            {
                return;
            }
        }
        else if ("true".Equals(tagName))
        {
            value = true;
        }
        else if ("false".Equals(tagName))
        {
            value = false;
        }
        else if ("plist".Equals(tagName))
        {
            return;
        }
        else
        {
            return;
        }

        if (_currObject == null)
        {
            _result = (T) value;
        }
        else if (_currObject.IsValueAsArray())
        {
            _currObject.AddValue(value);
        }
        else
        {
            if (_currObject.GetLastKey() != null)
                _currObject.AddValue(value);
        }
    }

    public void AddString(string? str)
    {
        _text?.Append(str);
    }

    public T GetResult()
    {
        return _result;
    }
}

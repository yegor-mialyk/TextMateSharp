namespace TextMateSharp;

[Serializable]
public class TextMateException : Exception
{
    public TextMateException()
    {
    }

    public TextMateException(string message) : base(message)
    {
    }

    public TextMateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

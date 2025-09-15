namespace TextMateSharp.Model;

public class Range
{
    public Range(int lineNumber)
    {
        FromLineNumber = lineNumber;
        ToLineNumber = lineNumber;
    }

    public Range(int fromLineNumber, int toLineNumber)
    {
        FromLineNumber = fromLineNumber;
        ToLineNumber = toLineNumber;
    }

    public int FromLineNumber { get; set; }

    public int ToLineNumber { get; set; }
}

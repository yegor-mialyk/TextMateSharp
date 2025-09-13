namespace TextMateSharp.Internal.Types;

public interface IRawCaptures : IEnumerable<string>
{
    IRawRule? GetCapture(string captureId);
}
namespace TextMateSharp.Grammars;

public interface IGrammar
{
    bool IsCompiling { get; }
    string? GetName();
    string GetScopeName();
    ICollection<string> GetFileTypes();
    ITokenizeLineResult? TokenizeLine(string lineText, StateStack? prevState, TimeSpan timeLimit);
}

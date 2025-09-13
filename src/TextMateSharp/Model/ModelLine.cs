namespace TextMateSharp.Model;

public class ModelLine
{
    public ModelLine()
    {
        IsInvalid = true;
    }

    public bool IsInvalid { get; set; }
    public TMState State { get; set; }
    public List<TMToken> Tokens { get; set; }

    public void ResetTokenizationState()
    {
        State = null;
        Tokens = null;
    }
}
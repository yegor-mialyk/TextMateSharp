namespace TextMateSharp.Internal.Rules;

public static class RuleId
{
    public const int NO_INIT = -3;

    public const int NO_RULE = 0;

    /**
     * This is a special constant to indicate that the end regexp matched.
     */
    public const int END_RULE = -1;

    /**
     * This is a special constant to indicate that the while regexp matched.
     */
    public const int WHILE_RULE = -2;
}

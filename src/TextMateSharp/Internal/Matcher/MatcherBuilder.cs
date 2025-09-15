using System.Text.RegularExpressions;

namespace TextMateSharp.Internal.Matcher;

public class MatcherBuilder<T>
{
    private readonly IMatchesName<T> _matchesName;
    private readonly Tokenizer _tokenizer;
    private string? _token;

    public List<MatcherWithPriority<T>> Results { get; } = [];

    public MatcherBuilder(string expression, IMatchesName<T> matchesName)
    {
        _tokenizer = new(expression);
        _matchesName = matchesName;

        _token = _tokenizer.Next();
        while (_token != null)
        {
            var priority = 0;
            if (_token.Length == 2 && _token[1] == ':')
            {
                switch (_token[0])
                {
                    case 'R':
                        priority = 1;
                        break;
                    case 'L':
                        priority = -1;
                        break;
                }

                _token = _tokenizer.Next();
            }

            var matcher = ParseConjunction();
            //TODO !!! if (matcher != null)
            Results.Add(new(matcher, priority));
            if (!",".Equals(_token))
                break;
            _token = _tokenizer.Next();
        }
    }

    private Predicate<T> ParseInnerExpression()
    {
        var matchers = new List<Predicate<T>>();
        var matcher = ParseConjunction();
        while (true) //TODO !!! use to be (matcher != null)
        {
            matchers.Add(matcher);
            if ("|".Equals(_token) || ",".Equals(_token))
                do
                {
                    _token = _tokenizer.Next();
                } while ("|".Equals(_token) || ",".Equals(_token)); // ignore subsequent
            // commas
            else
                break;

            matcher = ParseConjunction();
        }

        // some (or)
        return matcherInput =>
        {
            foreach (var matcher1 in matchers)
                if (matcher1.Invoke(matcherInput))
                    return true;

            return false;
        };
    }

    private Predicate<T> ParseConjunction()
    {
        var matchers = new List<Predicate<T>>();
        var matcher = ParseOperand();
        while (matcher != null)
        {
            matchers.Add(matcher);
            matcher = ParseOperand();
        }

        // every (and)
        return matcherInput =>
        {
            foreach (var matcher1 in matchers)
                if (!matcher1.Invoke(matcherInput))
                    return false;

            return true;
        };
    }

    private Predicate<T>? ParseOperand()
    {
        if ("-".Equals(_token))
        {
            _token = _tokenizer.Next();
            var expressionToNegate = ParseOperand();
            return matcherInput =>
            {
                if (expressionToNegate == null)
                    return false;
                return !expressionToNegate.Invoke(matcherInput);
            };
        }

        if ("(".Equals(_token))
        {
            _token = _tokenizer.Next();
            var expressionInParents = ParseInnerExpression();
            if (")".Equals(_token))
                _token = _tokenizer.Next();
            return expressionInParents;
        }

        if (IsIdentifier(_token))
        {
            var identifiers = new List<string>();
            do
            {
                identifiers.Add(_token!);
                _token = _tokenizer.Next();
            } while (_token != null && IsIdentifier(_token));

            return matcherInput => _matchesName.Match(identifiers, matcherInput);
        }

        return null;
    }

    private static bool IsIdentifier(string? token)
    {
        return !string.IsNullOrEmpty(token) &&
            /* Aprox. 2-3 times faster than:
             * static final Pattern IDENTIFIER_REGEXP = Pattern.compile("[\\w\\.:]+");
             * IDENTIFIER_REGEXP.matcher(token).matches();
             *
             * Aprox. 10% faster than:
             * token.chars().allMatch(ch -> ... )
             */
            token.All(ch => ch is '.' or ':' or '_' or >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9');
    }
}

public partial class Tokenizer
{
    private static readonly Regex REGEX = MyRegex();

    private readonly string _input;
    private Match? _currentMatch;

    public Tokenizer(string input)
    {
        _input = input;
    }

    public string? Next()
    {
        _currentMatch = _currentMatch == null ? REGEX.Match(_input) : _currentMatch.NextMatch();

        return _currentMatch.Success ? _currentMatch.Value : null;
    }

    [GeneratedRegex("([LR]:|[\\w\\.:][\\w\\.:\\-]*|[\\,\\|\\-\\(\\)])")]
    private static partial Regex MyRegex();
}


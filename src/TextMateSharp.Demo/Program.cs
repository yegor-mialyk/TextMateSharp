using System.Globalization;
using Spectre.Console;
using TextMateSharp.Definitions;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Utils;
using TextMateSharp.Themes;

namespace TextMateSharp.Demo;

public class Program
{
    private static void Main(string[] args)
    {
        try
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage TextMateSharp.Demo <fileToParse.cs>");

                return;
            }

            var fileToParse = Path.GetFullPath(args[0]);

            if (!File.Exists(fileToParse))
            {
                Console.WriteLine("No such file to parse: {0}", args[0]);
                return;
            }

            var options = new RegistryOptions(@"D:\MY\TextMateSharp\schemas\Themes");

            options.LoadFromDirectories(@"D:\MY\TextMateSharp\schemas\Grammars");

            var rawTheme = options.LoadRawTheme("dark_plus.json");
            if (rawTheme == null)
                throw new ArgumentException("Error loading theme");

            var registry = new GrammarRepository(rawTheme, options);

            var theme = registry.GetTheme();

            var scopeName = options.GetScopeByExtension(Path.GetExtension(fileToParse));

            if (scopeName == null)
            {
                Console.WriteLine("No grammar found for file {0}", fileToParse);
                return;
            }

            var grammar = registry.LoadGrammar(scopeName);

            if (grammar == null)
            {
                Console.WriteLine(File.ReadAllText(fileToParse));
                return;
            }

            Console.WriteLine("""
                              Grammar loaded in {0} ms.", Environment.TickCount - ini);
                              """);

            var tokenizeIni = Environment.TickCount;

            IStateStack? ruleStack = null;

            using (var sr = new StreamReader(fileToParse))
            {
                while (sr.ReadLine() is { } line)
                {
                    var result = grammar.TokenizeLine(line, ruleStack, TimeSpan.MaxValue);

                    ruleStack = result?.RuleStack;

                    foreach (var token in result?.Tokens ?? [])
                    {
                        var startIndex = token.StartIndex > line.Length ? line.Length : token.StartIndex;
                        var endIndex = token.EndIndex > line.Length ? line.Length : token.EndIndex;

                        var foreground = -1;
                        var background = -1;
                        var fontStyle = FontStyle.NotSet;

                        foreach (var themeRule in theme.Match(token.Scopes))
                        {
                            if (foreground == -1 && themeRule.foreground > 0)
                                foreground = themeRule.foreground;

                            if (background == -1 && themeRule.background > 0)
                                background = themeRule.background;

                            if (fontStyle == FontStyle.NotSet && themeRule.fontStyle > 0)
                                fontStyle = themeRule.fontStyle;
                        }

                        WriteToken(line.SubstringAtIndexes(startIndex, endIndex), foreground, background, fontStyle,
                            theme);
                    }

                    Console.WriteLine();
                }
            }

            var colorDictionary = theme.GetGuiColorDictionary();
            if (colorDictionary is { Count: > 0 })
            {
                Console.WriteLine("Gui Control Colors");
                foreach (var kvp in colorDictionary)
                    Console.WriteLine($"  {kvp.Key}, {kvp.Value}");
            }

            Console.WriteLine("File {0} tokenized in {1}ms.",
                Path.GetFileName(fileToParse),
                Environment.TickCount - tokenizeIni);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private static void WriteToken(string text, int foreground, int background, FontStyle fontStyle, Theme theme)
    {
        if (foreground == -1)
        {
            Console.Write(text);
            return;
        }

        var decoration = GetDecoration(fontStyle);

        var backgroundColor = GetColor(background, theme);
        var foregroundColor = GetColor(foreground, theme);

        var style = new Style(foregroundColor, backgroundColor, decoration);
        var markup = new Markup(text.Replace("[", "[[").Replace("]", "]]"), style);

        AnsiConsole.Write(markup);
    }

    private static Color GetColor(int colorId, Theme theme)
    {
        if (colorId == -1)
            return Color.Default;

        var hexString = theme.GetColor(colorId);

        return hexString == null ? Color.Default : HexToColor(hexString);
    }

    private static Decoration GetDecoration(FontStyle fontStyle)
    {
        var result = Decoration.None;

        if (fontStyle == FontStyle.NotSet)
            return result;

        if ((fontStyle & FontStyle.Italic) != 0)
            result |= Decoration.Italic;

        if ((fontStyle & FontStyle.Underline) != 0)
            result |= Decoration.Underline;

        if ((fontStyle & FontStyle.Bold) != 0)
            result |= Decoration.Bold;

        return result;
    }

    private static Color HexToColor(string hexString)
    {
        //replace # occurences
        if (hexString.Contains('#'))
            hexString = hexString.Replace("#", "");

        var r = byte.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
        var g = byte.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
        var b = byte.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);

        return new(r, g, b);
    }
}

using System.Text.Json;
using TextMateSharp.Internal.Parser.Json;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace TextMateSharp.Definitions;

public class RegistryOptions : IRegistryOptions
{
    private readonly Dictionary<string, GrammarDefinition> _availableGrammars = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _themePath;

    public RegistryOptions(string themePath)
    {
        _themePath = themePath;
    }

    public ICollection<string>? GetInjections(string scopeName)
    {
        return null;
    }

    public IRawTheme? LoadRawTheme(string filePath)
    {
        var fileInfo = new FileInfo(Path.Combine(_themePath, filePath));

        if (!fileInfo.Exists)
            return null;

        using var fileStream = fileInfo.OpenRead();

        using var reader = new StreamReader(fileStream);

        var parser = new JsonpListParser<IRawTheme>(true);

        return parser.Parse(reader);
    }

    public IRawGrammar? LoadRawGrammar(string scopeName)
    {
        var fileName = GetRawGrammarFile(scopeName);

        if (fileName is null)
            return null;

        var fileInfo = new FileInfo(fileName);

        if (!fileInfo.Exists)
            return null;

        using var fileStream = fileInfo.OpenRead();

        using var reader = new StreamReader(fileStream);

        var parser = new JsonpListParser<IRawGrammar>(false);

        return parser.Parse(reader);
    }

    public void LoadFromDirectories(string path)
    {
        var directories = new DirectoryInfo(path);

        if (!directories.Exists)
            return;

        foreach (var directory in directories.GetDirectories())
            LoadFromDirectory(directory);
    }

    private void LoadFromDirectory(DirectoryInfo directory)
    {
        if (!directory.Exists)
            return;

        var packageFileInfos = directory.GetFiles("package*.json");

        var packageFileInfo = packageFileInfos.FirstOrDefault(f =>
            f.Name.Equals("package.json", StringComparison.OrdinalIgnoreCase));

        var packageNlsFileInfo = packageFileInfos.FirstOrDefault(f =>
            f.Name.Equals("package.nls.json", StringComparison.OrdinalIgnoreCase));

        LoadPackage(directory.FullName, packageFileInfo, packageNlsFileInfo);
    }

    private void LoadPackage(string folderPath, FileInfo? packageJsonFileInfo, FileInfo? packageNlsFileInfo)
    {
        if (packageJsonFileInfo is not { Exists: true })
            return;

        var baseDir = packageJsonFileInfo.Directory?.FullName ?? string.Empty;

        using var stream = packageJsonFileInfo.OpenRead();

        var definition = JsonSerializer.Deserialize(stream, GrammarDefinition.JsonContext.GrammarDefinition);

        if (definition?.Contributes == null)
            return;

        if (packageNlsFileInfo is { Exists: true })
        {
            using var nlsStream = packageNlsFileInfo.OpenRead();

            var nlsPackage = JsonSerializer.Deserialize(nlsStream, GrammarDefinition.JsonContext.GrammarNlsPackage);

            if (nlsPackage != null)
            {
                definition.DisplayName = nlsPackage.DisplayName;
                definition.Description = nlsPackage.Description;
            }
        }

        foreach (var language in definition.Contributes.Languages ?? [])
        {
            if (language.ConfigurationFile == null)
                continue;

            var path = Path.Combine(baseDir, language.ConfigurationFile);

            language.Configuration = LanguageConfiguration.LoadFromFile(path);
        }

        _availableGrammars.Add(folderPath, definition);
    }

    public string? GetScopeByExtension(string extension)
    {
        foreach (var definition in _availableGrammars.Values)
            foreach (var language in definition.Contributes?.Languages ?? [])
            {
                if (language.Extensions == null)
                    continue;

                foreach (var grammar in language.Extensions
                             .Where(languageExtension =>
                                 extension.Equals(languageExtension, StringComparison.OrdinalIgnoreCase))
                             .SelectMany(_ => definition.Contributes?.Grammars ?? []))
                    return grammar.ScopeName;
            }

        return null;
    }

    private string? GetRawGrammarFile(string scopeName)
    {
        foreach (var (grammarName, definition) in _availableGrammars)
        {
            var grammar = definition.Contributes?.Grammars?.FirstOrDefault(grammar =>
                scopeName.Equals(grammar.ScopeName, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(grammar.Path));

            if (grammar?.Path is not null)
                return Path.Combine(grammarName, grammar.Path);
        }

        return null;
    }
}

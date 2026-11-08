using System.Text.Json;
using System.Text.RegularExpressions;
using InvestmentLab.SharedKernel;

namespace InvestmentLab.ArchitectureTests;

public sealed partial class LocalizationTests
{
    private const string MigrationsFolder = "Migrations";

    private static readonly string[] BuildFolders = ["bin", "obj", MigrationsFolder];

    [Fact]
    public void Backend_code_holds_no_text_in_the_users_language()
    {
        var offenders = BackendSources().Where(file => CyrillicText().IsMatch(File.ReadAllText(file))).ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Every_code_the_backend_sends_has_a_text_in_the_catalog()
    {
        var known = Catalog();

        var missing = BackendSources()
            .SelectMany(CodesIn)
            .Distinct()
            .Where(code => !known.Contains(code))
            .Order()
            .ToList();

        missing.ShouldBeEmpty();
    }

    [Fact]
    public void Every_language_has_a_text_for_every_code()
    {
        var ukrainian = Catalog(Language.Ukrainian);

        var mismatched = Language.All
            .Where(language => !Catalog(language).SetEquals(ukrainian))
            .Select(language => language.Code)
            .ToList();

        mismatched.ShouldBeEmpty();
    }

    private static HashSet<string> Catalog() => Catalog(Language.Ukrainian);

    private static HashSet<string> Catalog(Language language)
    {
        var path = Path.Combine(RepositoryFolder.Backend().Parent!.FullName, "locales", $"{language.Code}.json");
        var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))!;

        return [.. entries.Keys];
    }

    private static IEnumerable<string> CodesIn(string file)
    {
        var text = File.ReadAllText(file);
        var matches = MessageCode().Matches(text).Concat(ChosenMessageCode().Matches(text)).Concat(ErrorCode().Matches(text));

        return matches.SelectMany(match => match.Groups["code"].Captures.Select(capture => capture.Value));
    }

    private static IEnumerable<string> BackendSources()
    {
        var modules = Path.Combine(RepositoryFolder.Backend().FullName, "src");

        return Directory.EnumerateFiles(modules, "*.cs", SearchOption.AllDirectories)
            .Where(file => !BuildFolders.Any(folder => file.Split(Path.DirectorySeparatorChar).Contains(folder)));
    }

    [GeneratedRegex(@"\p{IsCyrillic}")]
    private static partial Regex CyrillicText();

    [GeneratedRegex("""Message\.Of\(\s*"(?<code>[^"{]+)"[,)]""")]
    private static partial Regex MessageCode();

    [GeneratedRegex("""Message\.Of\([^;"]*\?\s*"(?<code>[^"{]+)"\s*:\s*"(?<code>[^"{]+)"\)""")]
    private static partial Regex ChosenMessageCode();

    [GeneratedRegex("""Error\.(?:Validation|NotFound|Conflict|Forbidden)\(\s*"(?<code>[^"{]+)",""")]
    private static partial Regex ErrorCode();
}

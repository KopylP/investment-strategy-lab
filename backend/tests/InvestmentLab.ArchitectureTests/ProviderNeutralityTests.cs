namespace InvestmentLab.ArchitectureTests;

public sealed class ProviderNeutralityTests
{
    private const string SolutionFile = "InvestmentLab.slnx";
    private const string QuoteProviderOwner = "InvestmentLab.MarketData";
    private const string RateSourceName = "nbp";

    private static readonly string[] RateSourceOwners = ["InvestmentLab.MarketData", "InvestmentLab.Taxation"];

    private static readonly string[] ProviderNames = ["yfinance", "yahoo"];
    private static readonly string[] BuildFolders = ["bin", "obj", "node_modules"];

    [Fact]
    public void Only_the_market_data_module_names_the_quote_provider()
    {
        var backend = BackendFolder();
        var modules = Path.Combine(backend.FullName, "src", "Modules");
        var frontend = Path.Combine(backend.Parent!.FullName, "frontend", "src");

        var sources = SourcesOf(modules, "*.cs")
            .Where(file => !IsInsideFolder(file, QuoteProviderOwner))
            .Concat(SourcesOf(frontend, "*.ts"))
            .Concat(SourcesOf(frontend, "*.html"));

        var offenders = sources.Where(NamesAProvider).ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Only_market_data_and_taxation_name_the_exchange_rate_source()
    {
        var backend = BackendFolder();
        var modules = Path.Combine(backend.FullName, "src", "Modules");
        var frontend = Path.Combine(backend.Parent!.FullName, "frontend", "src");

        var sources = SourcesOf(modules, "*.cs")
            .Where(file => !RateSourceOwners.Any(owner => IsInsideFolder(file, owner)))
            .Concat(SourcesOf(frontend, "*.ts"))
            .Concat(SourcesOf(frontend, "*.html"));

        var offenders = sources.Where(file => File.ReadAllText(file).Contains(RateSourceName, StringComparison.OrdinalIgnoreCase)).ToList();

        offenders.ShouldBeEmpty();
    }

    private static DirectoryInfo BackendFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);

        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, SolutionFile)))
            folder = folder.Parent;

        return folder ?? throw new InvalidOperationException($"{SolutionFile} was not found above the test run.");
    }

    private static IEnumerable<string> SourcesOf(string root, string pattern) =>
        Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories)
            .Where(file => !BuildFolders.Any(folder => IsInsideFolder(file, folder)));

    private static bool IsInsideFolder(string file, string folder) =>
        file.Split(Path.DirectorySeparatorChar).Contains(folder);

    private static bool NamesAProvider(string file)
    {
        var text = File.ReadAllText(file);

        return ProviderNames.Any(name => text.Contains(name, StringComparison.OrdinalIgnoreCase));
    }
}

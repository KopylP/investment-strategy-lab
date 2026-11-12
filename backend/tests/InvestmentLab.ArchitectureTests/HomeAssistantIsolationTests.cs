namespace InvestmentLab.ArchitectureTests;

public sealed class HomeAssistantIsolationTests
{
    private const string Owner = "home-assistant";
    private static readonly string[] HomeAssistantWords = ["hassio", "'home-assistant/"];
    private static readonly string[] BuildFolders = ["node_modules", "dist", ".angular"];

    [Fact]
    public void Only_the_home_assistant_folder_of_the_frontend_knows_the_panel_it_runs_in()
    {
        var frontend = Path.Combine(RepositoryFolder.Backend().Parent!.FullName, "frontend", "src", "app");

        var offenders = Directory.EnumerateFiles(frontend, "*.*", SearchOption.AllDirectories)
            .Where(IsSource)
            .Where(file => !IsInsideFolder(file, Owner))
            .Where(NamesHomeAssistant)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    private static bool IsSource(string file) =>
        (file.EndsWith(".ts", StringComparison.Ordinal) || file.EndsWith(".html", StringComparison.Ordinal))
        && !BuildFolders.Any(folder => IsInsideFolder(file, folder));

    private static bool IsInsideFolder(string file, string folder) =>
        file.Split(Path.DirectorySeparatorChar).Contains(folder);

    private static bool NamesHomeAssistant(string file)
    {
        var text = File.ReadAllText(file);

        return HomeAssistantWords.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}

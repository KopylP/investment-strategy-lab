namespace InvestmentLab.ArchitectureTests;

internal static class RepositoryFolder
{
    private const string SolutionFile = "InvestmentLab.slnx";

    public static DirectoryInfo Backend()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);

        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, SolutionFile)))
            folder = folder.Parent;

        return folder ?? throw new InvalidOperationException($"{SolutionFile} was not found above the test run.");
    }
}

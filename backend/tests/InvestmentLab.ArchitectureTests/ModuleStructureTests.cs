using Assembly = System.Reflection.Assembly;

namespace InvestmentLab.ArchitectureTests;

public sealed class ModuleStructureTests
{
    private const string SolutionFile = "InvestmentLab.slnx";

    [Fact]
    public void Application_folders_hold_only_folders()
    {
        var looseFiles = ModuleProjects()
            .SelectMany(project => Directory.GetDirectories(project))
            .Select(feature => Path.Combine(feature, "Application"))
            .Where(Directory.Exists)
            .SelectMany(application => Directory.GetFiles(application, "*.cs"));

        looseFiles.ShouldBeEmpty();
    }

    [Fact]
    public void Module_roots_hold_only_the_module_file()
    {
        var looseFiles = ModuleProjects()
            .SelectMany(project => Directory.GetFiles(project, "*.cs"))
            .Where(file => !Path.GetFileName(file).EndsWith("Module.cs", StringComparison.Ordinal));

        looseFiles.ShouldBeEmpty();
    }

    [Fact]
    public void Repository_interfaces_live_in_domain()
    {
        var misplaced = CodeLocation.Modules
            .SelectMany(module => Assembly.Load($"InvestmentLab.{module}").GetTypes())
            .Where(IsRepositoryInterface)
            .Where(type => CodeLocation.Parse(type.Namespace ?? "").Layer != "Domain")
            .Select(type => type.FullName);

        misplaced.ShouldBeEmpty();
    }

    private static bool IsRepositoryInterface(Type type) =>
        type.IsInterface && type.Name.StartsWith('I') && type.Name.EndsWith("Repository", StringComparison.Ordinal);

    private static IEnumerable<string> ModuleProjects()
    {
        var modules = Path.Combine(SolutionFolder(), "src", "Modules");

        return CodeLocation.Modules.Select(module => Path.Combine(modules, module, $"InvestmentLab.{module}"));
    }

    private static string SolutionFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);

        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, SolutionFile)))
            folder = folder.Parent;

        return folder?.FullName ?? throw new InvalidOperationException($"{SolutionFile} was not found above the test binaries.");
    }
}

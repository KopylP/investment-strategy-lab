namespace InvestmentLab.Taxation.Declarations.Infrastructure;

/// <summary>The official forms and fonts embedded in the module (Forms/…).</summary>
internal static class FormResources
{
    public static byte[] Load(string name)
    {
        using var stream = typeof(FormResources).Assembly.GetManifestResourceStream($"Forms/{name}")
            ?? throw new InvalidOperationException($"Form resource {name} is missing.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}

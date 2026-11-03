using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Definitions.Domain;

public static class StrategyReports
{
    private static readonly HashSet<string> KnownFrequencies = ["off", "hourly", "daily", "weekly", "monthly", "quarterly"];

    public static Error? Check(IReadOnlyList<ReportDefinition> declared, IReadOnlyList<VersionReport> own)
    {
        var declaredCodes = declared.Select(report => report.Code).ToHashSet();

        if (own.FirstOrDefault(report => !declaredCodes.Contains(report.Code)) is { } unknown)
            return StrategyErrors.ReportUnknown(unknown.Code);
        if (FirstRepeatedCode(own) is { } repeated)
            return StrategyErrors.ReportRepeated(repeated);
        if (own.FirstOrDefault(report => !KnownFrequencies.Contains(report.Schedule.Frequency)) is { } offSchedule)
            return StrategyErrors.ReportFrequencyUnknown(offSchedule.Schedule.Frequency);

        return null;
    }

    public static IReadOnlyList<EffectiveReport> Of(IReadOnlyList<ReportDefinition> declared, IReadOnlyList<VersionReport> own)
    {
        var ownByCode = own.ToDictionary(report => report.Code);

        return [.. declared.Select(report => Effective(report, ownByCode.GetValueOrDefault(report.Code)))];
    }

    private static EffectiveReport Effective(ReportDefinition declared, VersionReport? own) =>
        new(
            Code: declared.Code,
            Name: declared.Name,
            Description: declared.Description,
            Mode: declared.Mode,
            Schedule: own?.Schedule ?? declared.Schedule ?? ReportSchedule.Off,
            SignalKinds: own?.SignalKinds ?? declared.SignalKinds ?? [],
            SignalIds: own?.SignalIds ?? []);

    private static string? FirstRepeatedCode(IReadOnlyList<VersionReport> own) =>
        own.GroupBy(report => report.Code).FirstOrDefault(group => group.Count() > 1)?.Key;
}

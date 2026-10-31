namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

public sealed record EngineSeries(
    string Key,
    string Role,
    IReadOnlyList<DateOnly> Dates,
    IReadOnlyList<decimal?> AdjustedClose,
    IReadOnlyList<decimal?> Close);

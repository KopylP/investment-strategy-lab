namespace InvestmentLab.Performance.Valuation.Application.Shared;

public static class DashboardPeriod
{
    public static string KeyOf(string? period) => string.IsNullOrWhiteSpace(period) ? "ALL" : period.ToUpperInvariant();

    public static DateOnly StartOf(string period, DateOnly last) => period switch
    {
        "1M" => last.AddMonths(-1),
        "3M" => last.AddMonths(-3),
        "6M" => last.AddMonths(-6),
        "1Y" => last.AddYears(-1),
        "YTD" => new DateOnly(last.Year, 1, 1),
        _ => DateOnly.MinValue,
    };
}

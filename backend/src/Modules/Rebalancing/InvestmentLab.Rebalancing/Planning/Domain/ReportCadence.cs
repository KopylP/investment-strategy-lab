namespace InvestmentLab.Rebalancing.Planning.Domain;

public enum RebalanceFrequency
{
    Weekly,
    Monthly,
    Quarterly,
}

/// <summary>Reports a strategy asks for: how often it rebalances and whether it wants the weekly risk review.</summary>
public sealed record ReportCadence(RebalanceFrequency Rebalance, bool WeeklyRiskReview)
{
    public static ReportCadence Classic => new(RebalanceFrequency.Monthly, true);

    public static ReportCadence Of(string rebalance, string riskReview) => new(
        Enum.TryParse<RebalanceFrequency>(rebalance, ignoreCase: true, out var frequency) ? frequency : RebalanceFrequency.Monthly,
        !string.Equals(riskReview, "Off", StringComparison.OrdinalIgnoreCase));

    /// <summary>Reports are made on Sunday evening, after the markets closed on Friday; the plan is carried out on Monday.</summary>
    public bool IsRebalanceDay(DateOnly day) => day.DayOfWeek == DayOfWeek.Sunday && Rebalance switch
    {
        RebalanceFrequency.Weekly => true,
        RebalanceFrequency.Monthly => day.Day <= 7,
        RebalanceFrequency.Quarterly => day.Day <= 7 && day.Month % 3 == 1,
        _ => false,
    };

    public bool IsRiskReviewDay(DateOnly day) => WeeklyRiskReview && day.DayOfWeek == DayOfWeek.Sunday;

    /// <summary>The first rebalance day on or after <paramref name="from"/>.</summary>
    public DateOnly NextRebalance(DateOnly from)
    {
        var day = from;
        while (!IsRebalanceDay(day))
            day = day.AddDays(1);
        return day;
    }

    public DateOnly? NextRiskReview(DateOnly from)
    {
        if (!WeeklyRiskReview)
            return null;
        var day = from;
        while (day.DayOfWeek != DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day;
    }
}

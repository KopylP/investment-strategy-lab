namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed record SignalTarget(SignalTargetKind Kind, Guid? AccountId, Guid? SubPortfolioId, Guid? InstrumentId, string? GroupName = null)
{
    public static SignalTarget Instrument(Guid instrumentId) =>
        new(SignalTargetKind.Instrument, null, null, instrumentId);

    public static SignalTarget InstrumentGroup(string groupName) =>
        new(SignalTargetKind.InstrumentGroup, null, null, null, groupName);

    public static SignalTarget EachPosition(Guid accountId) =>
        new(SignalTargetKind.EachPosition, accountId, null, null);

    public static SignalTarget SubPortfolio(Guid accountId, Guid subPortfolioId) =>
        new(SignalTargetKind.SubPortfolio, accountId, subPortfolioId, null);

    public static SignalTarget Account(Guid accountId) =>
        new(SignalTargetKind.Account, accountId, null, null);

    public bool CanBeMeasuredBy(SignalMetric metric) => Kind switch
    {
        SignalTargetKind.Instrument => metric is not SignalMetric.LossFromCost,
        SignalTargetKind.EachPosition => metric is not SignalMetric.Level,
        _ => metric is SignalMetric.DrawdownFromPeak or SignalMetric.DayFall,
    };
}

namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record SectionRiskPosition(
    Guid InstrumentId,
    string Status,
    decimal Quantity,
    decimal AverageCost,
    decimal CurrentPrice,
    decimal Value,
    decimal PnlPct,
    decimal DrawdownFromHigh,
    decimal? ExitPrice,
    string ExitRule,
    decimal? ExitDistance,
    IReadOnlyList<string> Alerts,
    bool Sell);

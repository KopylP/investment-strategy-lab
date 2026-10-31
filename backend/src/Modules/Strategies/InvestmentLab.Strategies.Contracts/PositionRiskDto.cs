namespace InvestmentLab.Strategies.Contracts;

public sealed record PositionRiskDto(
    Guid InstrumentId,
    string Status,
    decimal Quantity,
    decimal AverageCost,
    decimal CurrentPrice,
    decimal Value,
    decimal PnlPct,
    decimal DrawdownFromHigh,
    bool AboveMovingAverage,
    decimal? MovingAverage,
    decimal? ExitPrice,
    string ExitRule,
    decimal? ExitDistance,
    decimal? StopPrice,
    decimal? MaExitPrice,
    decimal? TrailExitPrice,
    IReadOnlyList<string> Alerts,
    bool Sell);

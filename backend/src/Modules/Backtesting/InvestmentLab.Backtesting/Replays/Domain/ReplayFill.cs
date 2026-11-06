namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayFill(
    Guid InstrumentId,
    decimal Quantity,
    decimal Price,
    decimal ReferencePrice,
    decimal Commission,
    decimal SpreadCost,
    decimal SlippageCost,
    DateOnly DecisionDate);

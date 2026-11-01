namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record StrategyAsk(
    Guid StrategyId,
    int? Version,
    string Currency,
    decimal Cash,
    IReadOnlyList<IHeldPosition> Positions,
    decimal Contribution,
    DateOnly AsOf,
    Guid? CashInstrumentId)
{
    public static StrategyAsk Of(IPlanningAccount account, IPlanningSubPortfolio sub, IHeldSubPortfolio? held, decimal contribution, DateOnly asOf) => new(
        StrategyId: sub.StrategyId!.Value,
        Version: sub.PinnedStrategyVersion,
        Currency: account.BaseCurrency,
        Cash: Math.Max(0m, held?.Cash ?? 0m),
        Positions: held?.Positions ?? [],
        Contribution: contribution,
        AsOf: asOf,
        CashInstrumentId: sub.CashInstrumentId);
}

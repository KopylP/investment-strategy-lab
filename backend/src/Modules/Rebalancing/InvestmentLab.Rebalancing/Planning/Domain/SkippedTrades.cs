namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed class SkippedTrades
{
    public List<Guid> TrimsAtLoss { get; } = [];

    public List<Guid> TrimsBySetting { get; } = [];

    public List<Guid> SmallBuys { get; } = [];

    public List<Guid> BuysBelowOneShare { get; } = [];

    public List<Guid> UnpricedBuys { get; } = [];
}

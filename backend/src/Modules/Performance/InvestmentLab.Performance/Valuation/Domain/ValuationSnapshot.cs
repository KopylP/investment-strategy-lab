namespace InvestmentLab.Performance.Valuation.Domain;

public sealed class ValuationSnapshot
{
    public long Id { get; private set; }
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public SnapshotScenario Scenario { get; private set; }
    public Guid? SubPortfolioId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal Value { get; private set; }
    public decimal Cash { get; private set; }
    public decimal Flow { get; private set; }
    public decimal NetContributions { get; private set; }
    public decimal TwrIndex { get; private set; }
    public decimal CostBasis { get; private set; }

    private ValuationSnapshot() { }

    public static ValuationSnapshot From(Guid accountId, UserId ownerId, SnapshotScenario scenario, Snapshot snapshot) => new()
    {
        AccountId = accountId,
        OwnerId = ownerId,
        Scenario = scenario,
        SubPortfolioId = snapshot.SubPortfolioId,
        Date = snapshot.Date,
        Value = snapshot.Value,
        Cash = snapshot.Cash,
        Flow = snapshot.Flow,
        NetContributions = snapshot.NetContributions,
        TwrIndex = snapshot.TwrIndex,
        CostBasis = snapshot.CostBasis,
    };
}

namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IPlanningAccount
{
    Guid Id { get; }
    UserId OwnerId { get; }
    string Name { get; }
    string BaseCurrency { get; }
    string ContributionPolicy { get; }
    decimal DriftBand { get; }
    IReadOnlyList<IPlanningSubPortfolio> SubPortfolios { get; }
}

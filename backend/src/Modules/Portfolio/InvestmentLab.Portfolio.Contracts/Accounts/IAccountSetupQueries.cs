namespace InvestmentLab.Portfolio.Contracts.Accounts;

/// <summary>How accounts are set up: sub-portfolios, their strategies, target shares and limits.</summary>
public interface IAccountSetupQueries
{
    /// <summary>Within a request only the signed-in user's accounts are visible; background work sees all.</summary>
    Task<AccountSetupDto?> GetAccountSetup(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountSetupDto>> GetAccountSetups(CancellationToken cancellationToken);
}

/// <param name="ContributionPolicy">FillUnderweight or FixedSplit.</param>
/// <param name="DriftBand">Fraction 0..1.</param>
public sealed record AccountSetupDto(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Broker,
    string BaseCurrency,
    string TaxPolicyCode,
    string ContributionPolicy,
    decimal DriftBand,
    IReadOnlyList<SubPortfolioSetupDto> SubPortfolios);

/// <param name="TargetShare">Fraction 0..1.</param>
/// <param name="Status">Active or Retired.</param>
/// <param name="PinnedStrategyVersion">Null: follow the latest version of the strategy.</param>
public sealed record SubPortfolioSetupDto(
    Guid Id,
    string Name,
    decimal TargetShare,
    string Status,
    Guid? StrategyId,
    int? PinnedStrategyVersion,
    Guid? CashInstrumentId,
    decimal? MaxPositionWeight,
    decimal? MaxMonthlyTurnover);

public sealed record SubPortfolioAddedV1(Guid AccountId, Guid SubPortfolioId, string Name);

/// <param name="TargetShares">Sub-portfolio id → fraction 0..1, for every active sub-portfolio.</param>
public sealed record TargetSharesChangedV1(Guid AccountId, IReadOnlyDictionary<Guid, decimal> TargetShares, DateTimeOffset ChangedAt);

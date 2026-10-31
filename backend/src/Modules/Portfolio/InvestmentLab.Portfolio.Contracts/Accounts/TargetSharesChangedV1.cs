namespace InvestmentLab.Portfolio.Contracts.Accounts;

public sealed record TargetSharesChangedV1(Guid AccountId, IReadOnlyDictionary<Guid, decimal> TargetShares, DateTimeOffset ChangedAt);

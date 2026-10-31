namespace InvestmentLab.Portfolio.Accounts.Domain;

public sealed record TargetSharesChanged(
    AccountId AccountId,
    IReadOnlyDictionary<Guid,
    decimal> TargetShares,
    DateTimeOffset OccurredAt) : IDomainEvent;

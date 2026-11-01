namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record DeclaringAccount(Guid Id, UserId OwnerId, bool GeneratesReportsOnItsOwn, IReadOnlyList<AccountReport> Reports);

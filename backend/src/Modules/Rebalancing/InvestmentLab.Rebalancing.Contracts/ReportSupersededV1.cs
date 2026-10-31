namespace InvestmentLab.Rebalancing.Contracts;

public sealed record ReportSupersededV1(Guid ReportId, Guid AccountId, Guid SupersededBy, DateTimeOffset SupersededAt);

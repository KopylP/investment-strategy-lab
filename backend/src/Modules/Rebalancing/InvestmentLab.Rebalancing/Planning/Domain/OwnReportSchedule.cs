namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record OwnReportSchedule(string Code, Cadence Cadence, DateTimeOffset Since);

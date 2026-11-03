namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record DraftSetupDto(IReadOnlyList<Guid> Funds, Guid? BenchmarkId, Guid? CashId, Guid? VolatilityIndexId, IReadOnlyList<Guid> ExtraSeries);

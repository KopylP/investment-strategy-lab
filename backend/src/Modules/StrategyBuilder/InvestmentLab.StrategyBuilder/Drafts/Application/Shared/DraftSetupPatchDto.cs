using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record DraftSetupPatchDto(
    IReadOnlyList<Guid>? Funds = null,
    Guid? BenchmarkId = null,
    Guid? CashId = null,
    Guid? VolatilityIndexId = null,
    IReadOnlyList<Guid>? ExtraSeries = null)
{
    public DraftSetupPatch ToPatch() => new(Funds, BenchmarkId, CashId, VolatilityIndexId, ExtraSeries);
}

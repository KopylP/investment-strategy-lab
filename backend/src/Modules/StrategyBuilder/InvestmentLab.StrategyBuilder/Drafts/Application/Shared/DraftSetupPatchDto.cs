using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record DraftSetupPatchDto(IReadOnlyList<Guid>? Funds, Guid? BenchmarkId, Guid? CashId, Guid? VolatilityIndexId, IReadOnlyList<Guid>? ExtraSeries)
{
    public DraftSetupPatch ToPatch() => new(Funds, BenchmarkId, CashId, VolatilityIndexId, ExtraSeries);
}

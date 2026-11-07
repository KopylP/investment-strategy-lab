namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record DraftSetupPatch(IReadOnlyList<Guid>? Funds, Guid? BenchmarkId, Guid? CashId, Guid? VolatilityIndexId, IReadOnlyList<Guid>? ExtraSeries)
{
    public Result<DraftSetup> Over(DraftSetup current) =>
        DraftSetup.TryOf(
            funds: Funds ?? current.Funds,
            benchmarkId: BenchmarkId ?? current.BenchmarkId,
            cashId: CashId ?? current.CashId,
            volatilityIndexId: VolatilityIndexId ?? current.VolatilityIndexId,
            extraSeries: ExtraSeries ?? current.ExtraSeries);
}

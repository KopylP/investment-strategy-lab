namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record DraftSetup(IReadOnlyList<Guid> Funds, Guid? BenchmarkId, Guid? CashId, Guid? VolatilityIndexId, IReadOnlyList<Guid> ExtraSeries)
{
    public const int MaxFunds = 60;

    public static DraftSetup Empty { get; } = new([], null, null, null, []);

    public static Result<DraftSetup> TryOf(IReadOnlyList<Guid> funds, Guid? benchmarkId, Guid? cashId, Guid? volatilityIndexId, IReadOnlyList<Guid> extraSeries)
    {
        if (funds.Count > MaxFunds)
            return DraftErrors.TooManyFunds(MaxFunds);
        if (funds.Distinct().Count() != funds.Count)
            return DraftErrors.FundRepeated();

        return new DraftSetup(funds, benchmarkId, cashId, volatilityIndexId, [.. extraSeries.Distinct()]);
    }

    public int ExtraNumberOf(Guid instrumentId)
    {
        var index = ExtraSeries.ToList().IndexOf(instrumentId);

        return index < 0 ? 0 : index + 1;
    }
}

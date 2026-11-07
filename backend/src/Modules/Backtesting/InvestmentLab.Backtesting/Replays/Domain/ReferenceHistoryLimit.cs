namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReferenceHistoryLimit(Guid InstrumentId, DateOnly WarmedFrom) : HistoryLimit
{
    public override string Describe(Func<Guid, string> labelOf) =>
        Message.Of("research.history_limit.reference", new { name = labelOf(InstrumentId), from = WarmedFrom.ToString("yyyy-MM-dd") });
}

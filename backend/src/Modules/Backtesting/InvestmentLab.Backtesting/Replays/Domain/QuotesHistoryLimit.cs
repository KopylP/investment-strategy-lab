namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record QuotesHistoryLimit(DateOnly FirstQuote) : HistoryLimit
{
    public override string Describe(Func<Guid, string> labelOf) =>
        Message.Of("research.history_limit.quotes", new { from = FirstQuote.ToString("yyyy-MM-dd") });
}

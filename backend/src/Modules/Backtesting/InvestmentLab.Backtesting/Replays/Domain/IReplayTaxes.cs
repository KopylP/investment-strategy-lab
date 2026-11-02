namespace InvestmentLab.Backtesting.Replays.Domain;

public interface IReplayTaxes
{
    Task<IReadOnlyList<YearTax>> YearlyAsync(IReadOnlyList<BookTrade> trades, string currency, CancellationToken cancellationToken);
}

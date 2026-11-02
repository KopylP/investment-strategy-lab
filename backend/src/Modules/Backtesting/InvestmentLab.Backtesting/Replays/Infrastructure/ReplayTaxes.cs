using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.Taxation.Contracts;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class ReplayTaxes(ITaxSimulation simulation, IFxRates rates) : IReplayTaxes
{
    public async Task<IReadOnlyList<YearTax>> YearlyAsync(IReadOnlyList<BookTrade> trades, string currency, CancellationToken cancellationToken)
    {
        var simulated = await simulation.SimulateAsync([.. trades.Select(trade => TradeOf(trade, currency))], cancellationToken);
        var taxes = new List<YearTax>();

        foreach (var year in simulated.Years.Where(year => year.TaxDue > 0m))
        {
            var paidOn = LastTradeOf(trades, year.Year);
            var amount = await InReplayCurrencyAsync(year.TaxDue, simulated.TaxCurrency, currency, paidOn, cancellationToken);
            taxes.Add(new YearTax(year.Year, amount));
        }

        return taxes;
    }

    private static SimulatedTradeDto TradeOf(BookTrade trade, string currency) =>
        new(trade.InstrumentId, trade.On, trade.IsSale, trade.Quantity, trade.Price, trade.Fee, currency);

    private static DateOnly LastTradeOf(IReadOnlyList<BookTrade> trades, int year) =>
        trades.Where(trade => trade.On.Year == year).Max(trade => trade.On);

    private async Task<decimal> InReplayCurrencyAsync(decimal taxDue, string taxCurrency, string currency, DateOnly paidOn, CancellationToken cancellationToken)
    {
        if (string.Equals(taxCurrency, currency, StringComparison.OrdinalIgnoreCase))
            return taxDue;

        var rate = await rates.GetCrossRate(taxCurrency, currency, paidOn, cancellationToken)
            ?? throw new InvalidOperationException($"No {taxCurrency}/{currency} rate for {paidOn} to pay a replayed tax.");

        return taxDue * rate;
    }
}

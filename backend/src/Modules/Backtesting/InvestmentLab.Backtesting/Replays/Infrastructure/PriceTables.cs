using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.MarketData.Contracts.Quotes;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class PriceTables(IQuoteHistory quotes, TimeProvider clock) : IPriceTables
{
    private const int YearsOfHistory = 21;

    public async Task<PriceTable> LoadAsync(ReplaySetup setup, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var history = await quotes.GetQuoteHistory(setup.PricedInstruments, today.AddYears(-YearsOfHistory), today, cancellationToken);

        var byInstrument = history.ToDictionary(
            series => series.InstrumentId,
            series => (IReadOnlyList<PricedDay>)[.. series.Points.Select(point => new PricedDay(point.Date, point.AdjustedClose))]);

        return new PriceTable(byInstrument);
    }
}

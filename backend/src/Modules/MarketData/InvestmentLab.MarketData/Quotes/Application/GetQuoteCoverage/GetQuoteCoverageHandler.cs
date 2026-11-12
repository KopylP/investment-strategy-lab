using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Quotes.Application.GetQuoteCoverage;

public sealed record QuoteCoverageDto(Guid InstrumentId, string Label, DateOnly? First, DateOnly? Last, int Count, int LongestGapDays);

public sealed class GetQuoteCoverageHandler(IQuoteHistory history, IInstrumentRepository instruments, TimeProvider clock) : IHandler
{
    public async Task<IReadOnlyList<QuoteCoverageDto>> Handle(IReadOnlyCollection<Guid> instrumentIds, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var series = await history.GetQuoteHistoryWithArchive(instrumentIds, DateOnly.MinValue, today, cancellationToken);
        var labels = await LabelsAsync(cancellationToken);

        return [.. series.Select(quotes => CoverageOf(quotes, labels))];
    }

    private async Task<IReadOnlyDictionary<Guid, string>> LabelsAsync(CancellationToken cancellationToken)
    {
        var known = await instruments.ListAsync(cancellationToken);

        return known.ToDictionary(instrument => instrument.Id.Value, instrument => instrument.Ticker ?? instrument.Name);
    }

    private static QuoteCoverageDto CoverageOf(QuoteSeriesDto quotes, IReadOnlyDictionary<Guid, string> labels)
    {
        var days = quotes.Points.Select(point => point.Date).ToList();
        var coverage = QuoteCoverage.Of(days);

        return new QuoteCoverageDto(
            InstrumentId: quotes.InstrumentId,
            Label: labels.GetValueOrDefault(quotes.InstrumentId) ?? quotes.InstrumentId.ToString(),
            First: coverage.First,
            Last: coverage.Last,
            Count: coverage.Count,
            LongestGapDays: coverage.LongestGapDays);
    }
}

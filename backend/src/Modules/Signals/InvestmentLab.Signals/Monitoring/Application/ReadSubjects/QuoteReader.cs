using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed class QuoteReader(IQuoteHistory quotes, TimeProvider clock)
{
    private const string SpikeFlag = "Spike";

    public DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    public async Task<SubjectReading?> MeasureAsync(QuoteSubject subject, SignalMetric metric, DateOnly from, CancellationToken cancellationToken)
    {
        if (await LatestFreshPriceAsync(subject.InstrumentId, cancellationToken) is null)
            return null;

        var points = await UsablePointsAsync(subject.InstrumentId, from, cancellationToken);
        if (points.Count == 0)
            return null;

        return subject.ReadingOf(Measure(metric, points), points[^1].Date);
    }

    public async Task<FreshPrice?> LatestFreshPriceAsync(Guid instrumentId, CancellationToken cancellationToken)
    {
        var prices = await quotes.GetLatestPrices([instrumentId], Today, cancellationToken);
        if (prices.Count == 0 || prices[0].IsStale)
            return null;

        return new FreshPrice(prices[0].Close, prices[0].Currency, prices[0].Date);
    }

    public async Task<bool> IsSpikeOnAsync(Guid instrumentId, DateOnly date, CancellationToken cancellationToken)
    {
        var points = await PointsAsync(instrumentId, date, date, cancellationToken);

        return points.Any(IsSpike);
    }

    private async Task<List<QuotePointDto>> UsablePointsAsync(Guid instrumentId, DateOnly from, CancellationToken cancellationToken)
    {
        var points = await PointsAsync(instrumentId, from, Today, cancellationToken);
        if (points.Count == 0 || IsSpike(points[^1]))
            return [];

        return [.. points.Where(point => !IsSpike(point))];
    }

    private async Task<IReadOnlyList<QuotePointDto>> PointsAsync(Guid instrumentId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var history = await quotes.GetQuoteHistory([instrumentId], from, to, cancellationToken);

        return history.Count > 0 ? history[0].Points : [];
    }

    private static decimal? Measure(SignalMetric metric, List<QuotePointDto> points)
    {
        if (metric == SignalMetric.Level)
            return points[^1].Close;

        var adjusted = points.Select(point => point.AdjustedClose).ToList();

        return metric == SignalMetric.DayFall
            ? SignalMetrics.DayFall(adjusted)
            : SignalMetrics.DrawdownFromPeak(adjusted);
    }

    private static bool IsSpike(QuotePointDto point) => point.Flags.Contains(SpikeFlag);
}

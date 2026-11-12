using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.SharedKernel;
using InvestmentLab.Signals.Monitoring.Application.ReadSubjects;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Tests;

public sealed class InstrumentGroupSubjectReaderTests
{
    private static readonly GroupMember Fallen = new(Guid.NewGuid(), "FALLEN");
    private static readonly GroupMember Steady = new(Guid.NewGuid(), "STEADY");
    private static readonly GroupMember Unquoted = new(Guid.NewGuid(), "UNQUOTED");

    [Fact]
    public async Task Each_member_of_the_group_is_measured_on_its_own_and_one_without_quotes_stays_watched()
    {
        var quotes = new QuotesOf(new()
        {
            [Fallen.InstrumentId] = [100m, 75m],
            [Steady.InstrumentId] = [100m, 100m],
        });
        var reader = new InstrumentGroupSubjectReader(new GroupOf(Fallen, Steady, Unquoted), new QuoteReader(quotes, TimeProvider.System));

        var readings = await reader.ReadAsync(SignalOnGroup(), TestContext.Current.CancellationToken);

        var measured = readings.Measured.ToDictionary(reading => reading.Label, reading => reading.Value);
        measured.Count.ShouldBe(2);
        measured["FALLEN"].ShouldBe(0.25m);
        measured["STEADY"].ShouldBe(0m);
        readings.LiveSubjects.Count.ShouldBe(3);
    }

    private static Signal SignalOnGroup()
    {
        var condition = SignalCondition.TryOf(SignalMetric.DrawdownFromPeak, SignalDirection.Above, 0.20m, null, null).Value;
        var target = SignalTarget.InstrumentGroup("Японія");

        return Signal.Create(UserId.Of(Guid.NewGuid()), "Японія", target, condition, Cadence.Off).Value;
    }

    private sealed class GroupOf(params GroupMember[] members) : IInstrumentGroups
    {
        public Task<IReadOnlyList<GroupMember>> MembersOfAsync(string groupName, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GroupMember>>(members);
    }

    private sealed class QuotesOf(Dictionary<Guid, decimal[]> closes) : IQuoteHistory
    {
        public Task<IReadOnlyList<QuoteSeriesDto>> GetQuoteHistory(
            IReadOnlyCollection<Guid> instrumentIds, DateOnly from, DateOnly to, CancellationToken cancellationToken)
        {
            var known = instrumentIds.Where(closes.ContainsKey);
            IReadOnlyList<QuoteSeriesDto> series = [.. known.Select(id => new QuoteSeriesDto(id, PointsOf(closes[id], to)))];

            return Task.FromResult(series);
        }

        public Task<IReadOnlyList<QuoteSeriesDto>> GetQuoteHistoryWithArchive(
            IReadOnlyCollection<Guid> instrumentIds, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<LatestPriceDto>> GetLatestPrices(
            IReadOnlyCollection<Guid> instrumentIds, DateOnly asOf, CancellationToken cancellationToken)
        {
            var known = instrumentIds.Where(closes.ContainsKey);
            IReadOnlyList<LatestPriceDto> prices =
                [.. known.Select(id => new LatestPriceDto(id, asOf, closes[id][^1], closes[id][^1], "EUR", IsStale: false))];

            return Task.FromResult(prices);
        }

        private static List<QuotePointDto> PointsOf(decimal[] prices, DateOnly lastDate)
        {
            var firstDate = lastDate.AddDays(1 - prices.Length);

            return [.. prices.Select((price, index) => new QuotePointDto(firstDate.AddDays(index), null, null, null, price, price, []))];
        }
    }
}

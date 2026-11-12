using InvestmentLab.Backtesting.Replays.Infrastructure;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class ReplayedStrategiesTests
{
    private static readonly Guid Fund = Guid.NewGuid();

    [Fact]
    public async Task Schedule_days_and_history_requirements_survive_the_strategy_contract()
    {
        var source = new Directory("monthly", 15, "friday", "20:00");
        var setup = (await new ReplayedStrategies(source, new Catalog()).FindAsync(Guid.NewGuid(), 1, CancellationToken.None))!;
        setup.HistoryBars.ShouldBe(1009);
        setup.RebalanceSchedule!.Day.ShouldBe(15);
        setup.RebalanceSchedule.Weekday.ShouldBe(DayOfWeek.Friday);
        setup.UnsupportedSchedule.ShouldBeNull();
        setup.PricedInstruments.ShouldContain(source.Extra);
    }

    [Theory]
    [InlineData("hourly", "20:00")]
    [InlineData("daily", "09:30")]
    public async Task Intraday_frequency_or_time_is_never_silently_approximated(string frequency, string time)
    {
        var setup = (await new ReplayedStrategies(new Directory(frequency, 1, "sunday", time), new Catalog())
            .FindAsync(Guid.NewGuid(), 1, CancellationToken.None))!;
        setup.UnsupportedSchedule.ShouldNotBeNull();
    }

    private sealed class Catalog : IInstrumentCatalog
    {
        public Task<IReadOnlyList<InstrumentDto>> GetInstruments(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InstrumentDto>>([new(Fund, "ETF", null, "Fund", "XETRA", "EUR", null, true, [])]);
    }

    private sealed class Directory(string frequency, int day, string weekday, string time) : IStrategyDirectory
    {
        public Guid Extra { get; } = Guid.NewGuid();
        public Task<StrategyReplaySetupDto?> GetReplaySetup(Guid strategyId, int? version, CancellationToken cancellationToken) =>
            Task.FromResult<StrategyReplaySetupDto?>(new(strategyId, Guid.NewGuid(), 1, true, [Fund], null, Fund,
                [new StrategyReportDto("rebalance", "Rebalance", "", StrategyReportDto.Rebalance, new(frequency, day, weekday, time, 0), [], [])],
                [], HistoryBars: 1009, ExtraSeries: [Extra]));
        public Task<IReadOnlyList<StrategySummaryDto>> GetStrategies(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, string>> GetMessages(string languageCode, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

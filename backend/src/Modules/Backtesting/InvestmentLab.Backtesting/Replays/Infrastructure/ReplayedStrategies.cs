using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class ReplayedStrategies(IStrategyDirectory strategies, IInstrumentCatalog instruments) : IReplayedStrategies
{
    public async Task<ReplaySetup?> FindAsync(Guid strategyId, int? version, CancellationToken cancellationToken)
    {
        if (await strategies.GetReplaySetup(strategyId, version, cancellationToken) is not { } setup)
            return null;

        var catalog = await instruments.GetInstruments(cancellationToken);
        var currency = catalog.First(instrument => instrument.Id == setup.Universe[0]).TradingCurrency;
        var rebalance = CadenceOf(setup, StrategyReportDto.Rebalance);

        return new ReplaySetup(
            StrategyId: setup.StrategyId,
            OwnerId: UserId.Of(setup.OwnerId),
            Version: setup.Version,
            CanBeReplayed: setup.SupportsBacktest && rebalance is not null,
            Universe: setup.Universe,
            CashInstrumentId: setup.CashInstrumentId,
            BenchmarkInstrumentId: setup.BenchmarkInstrumentId,
            RebalanceCadence: rebalance ?? ReplayCadence.Monthly,
            RiskReviewCadence: CadenceOf(setup, StrategyReportDto.RiskReview),
            Currency: currency);
    }

    private static ReplayCadence? CadenceOf(StrategyReplaySetupDto setup, string mode)
    {
        var report = setup.Reports.FirstOrDefault(candidate => candidate.Mode == mode);

        return report?.Schedule.Frequency.ToUpperInvariant() switch
        {
            null or "OFF" => null,
            "HOURLY" or "DAILY" => ReplayCadence.Daily,
            "WEEKLY" => ReplayCadence.Weekly,
            "MONTHLY" => ReplayCadence.Monthly,
            "QUARTERLY" => ReplayCadence.Quarterly,
            var unknown => throw new InvalidOperationException($"Unknown report frequency '{unknown}'."),
        };
    }
}

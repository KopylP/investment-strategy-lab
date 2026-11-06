using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Backtesting.Replays.Domain.Comparisons;

namespace InvestmentLab.Backtesting.Replays.Application.RunBacktest;

public sealed class StrategyReplay(
    IPriceTables priceTables,
    IStrategyAnswers answers,
    ITargetTrades targetTrades,
    IReplaySignals signals,
    IReplayTaxes taxes,
    ReplayComparisonCatalog comparisonCatalog) : IResearchReplay
{
    public async Task<Result<ReplayResult>> RunAsync(ReplaySetup setup, CancellationToken cancellationToken)
    {
        if (ReplayReadiness.ScheduleProblem(setup) is { } scheduleProblem)
            return scheduleProblem;

        var comparisons = comparisonCatalog.Resolve(setup.Comparisons);
        if (comparisons.IsFailure)
            return comparisons.Error;

        var watches = await Watches(setup, cancellationToken);
        if (watches.IsFailure)
            return watches.Error;

        setup = ReplaySignalWatches.WithInstruments(setup, watches.Value);
        var history = await priceTables.LoadAsync(setup, cancellationToken);
        var prices = history.ForReplay(setup);
        var firstDay = ReplayReadiness.FirstDay(setup, prices);
        if (firstDay.IsFailure)
            return firstDay.Error;

        var portfolio = ReplayedPortfolio.Start(setup, prices, watches.Value);
        var replayed = await Replay(portfolio, firstDay.Value, cancellationToken);
        if (replayed.IsFailure)
            return replayed.Error;

        var series = await Series(portfolio, firstDay.Value, comparisons.Value, cancellationToken);

        return portfolio.ResultFrom(series);
    }

    private async Task<Result> Replay(ReplayedPortfolio portfolio, int firstDay, CancellationToken cancellationToken)
    {
        for (var index = firstDay; index < portfolio.Prices.Days.Count; index++)
        {
            var session = portfolio.Advance(index);
            if (session is null)
                continue;

            var rebalanced = await Rebalance(portfolio, session, cancellationToken);
            if (rebalanced.IsFailure)
                return rebalanced;

            var reviewed = await ReviewRisk(portfolio, session, cancellationToken);
            if (reviewed.IsFailure)
                return reviewed;
        }

        return Result.Success();
    }

    private async Task<Result<ReplaySignalWatches>> Watches(ReplaySetup setup, CancellationToken cancellationToken)
    {
        var builtIn = setup.Signals.AllKinds.ToDictionary(kind => kind, signals.Watch);
        var user = new Dictionary<Guid, IReplaySignalWatch>();
        var logicIdentifiers = setup.LogicSignalIds ?? [];
        var identifiers = logicIdentifiers.Concat(setup.Signals.AllSignalIds).Distinct();

        foreach (var id in identifiers)
        {
            var watch = await signals.WatchSignalAsync(id, cancellationToken);
            if (watch is not null)
                user.Add(id, watch);
        }

        return ReplaySignalWatches.Checked(builtIn, user, logicIdentifiers);
    }

    private async Task<Result> Rebalance(ReplayedPortfolio portfolio, ReplaySession session, CancellationToken cancellationToken)
    {
        if (!session.RebalanceRequested)
            return Result.Success();

        var decision = await answers.DecideAsync(portfolio.Setup, portfolio.Book, session.Date, session.SignalStates, cancellationToken);
        if (decision.IsFailure)
            return Result.Failure(decision.Error);

        var trades = targetTrades.TradesFor(decision.Value, portfolio.Setup, portfolio.Book, session.Prices);
        portfolio.Rebalance(session, decision.Value, trades);

        return Result.Success();
    }

    private async Task<Result> ReviewRisk(ReplayedPortfolio portfolio, ReplaySession session, CancellationToken cancellationToken)
    {
        if (!session.NeedsRiskAssessment(portfolio.HasPositions))
            return Result.Success();

        var assessment = await answers.AssessAsync(portfolio.Setup, portfolio.Book, session.Date, session.SignalStates, cancellationToken);
        if (assessment.IsFailure)
            return Result.Failure(assessment.Error);

        portfolio.ReviewRisk(session, assessment.Value);

        return Result.Success();
    }

    private async Task<IReadOnlyList<ReplaySeries>> Series(
        ReplayedPortfolio portfolio,
        int firstDay,
        IReadOnlyList<IReplayComparison> comparisons,
        CancellationToken cancellationToken)
    {
        var history = new ReplayedHistory(portfolio.Prices, firstDay, portfolio.Setup, portfolio.Growth);
        var strategy = await StrategySeries(portfolio, history.RiskFreeRate, cancellationToken);
        var series = new List<ReplaySeries> { strategy };

        foreach (var comparison in comparisons)
        {
            var line = comparison.LineOf(history);
            var compared = await ComparedSeries(line, portfolio, history.RiskFreeRate, cancellationToken);
            series.Add(compared);
        }

        return series;
    }

    private async Task<ReplaySeries> StrategySeries(ReplayedPortfolio portfolio, double riskFreeRate, CancellationToken cancellationToken)
    {
        var liquidation = portfolio.FinalLiquidation();
        var trades = portfolio.Book.TradeLog.Concat(liquidation).ToList();
        var yearlyTaxes = await taxes.YearlyAsync(trades, portfolio.Setup.Currency, cancellationToken);
        var afterTax = AfterTaxGrowth.FinalOf(portfolio.Growth, yearlyTaxes, portfolio.Capital);

        return ReplaySeries.Of(ReplaySeries.StrategyCode, Message.Of("backtest.series.strategy"), portfolio.Growth, riskFreeRate, afterTax);
    }

    private async Task<ReplaySeries> ComparedSeries(
        ComparedLine line,
        ReplayedPortfolio portfolio,
        double riskFreeRate,
        CancellationToken cancellationToken)
    {
        var trades = BookTrade.HeldThroughout(line.HeldAs, line.Growth, portfolio.Capital);
        var yearlyTaxes = await taxes.YearlyAsync(trades, portfolio.Setup.Currency, cancellationToken);
        var afterTax = AfterTaxGrowth.FinalOf(line.Growth, yearlyTaxes, portfolio.Capital);

        return ReplaySeries.Of(line.Code, line.Label, line.Growth, riskFreeRate, afterTax);
    }
}

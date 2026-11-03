using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class StrategyAnswers(IStrategyEvaluation evaluation) : IStrategyAnswers
{
    private const string ExitSignal = "exit";

    public async Task<Result<ReplayDecision>> DecideAsync(ReplaySetup setup, ReplayBook book, DateOnly day, IReadOnlyList<ReplaySignalState> states, CancellationToken cancellationToken)
    {
        var answer = await evaluation.EvaluateAsync(RequestOf(setup, book, day, states), cancellationToken);
        if (!answer.IsSuccess)
            return FailureOf(answer.ErrorCode, answer.ErrorMessage);

        var decided = answer.Value!;
        var targets = decided.Targets.Select(target => new ReplayTarget(target.InstrumentId, target.Weight)).ToList();

        return new ReplayDecision(decided.Market.IsFavorable, decided.InvestFraction, targets);
    }

    public async Task<Result<IReadOnlyList<Guid>>> ExitsAsync(ReplaySetup setup, ReplayBook book, DateOnly day, IReadOnlyList<ReplaySignalState> states, CancellationToken cancellationToken)
    {
        var answer = await evaluation.AssessRiskAsync(RequestOf(setup, book, day, states), cancellationToken);
        if (!answer.IsSuccess)
            return FailureOf(answer.ErrorCode, answer.ErrorMessage);

        var exits = answer.Value!.Signals.Where(signal => signal.Kind == ExitSignal).Select(signal => signal.InstrumentId).ToList();

        return exits;
    }

    private static EvaluateStrategyRequest RequestOf(ReplaySetup setup, ReplayBook book, DateOnly day, IReadOnlyList<ReplaySignalState> states)
    {
        var holdings = book.Positions.Select(position => new HoldingStateDto(
            position.InstrumentId,
            [.. position.Lots.Select(lot => new LotStateDto(lot.Quantity, lot.UnitCost, lot.AcquiredOn))]));
        var state = new SubPortfolioStateDto(setup.Currency, Math.Max(0m, book.Cash), [.. holdings]);

        var signalStates = states.Select(signal => new SignalStateDto(signal.SignalId, signal.ActiveFor)).ToList();

        return new EvaluateStrategyRequest(setup.StrategyId, setup.Version, state, 0m, day, SignalStates: signalStates);
    }

    private static Error FailureOf(string? code, string? message) =>
        Error.Validation("backtest.strategy_failed", $"{code}: {message}");
}

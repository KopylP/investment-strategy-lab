namespace InvestmentLab.Backtesting.Replays.Domain;

public interface IStrategyAnswers
{
    Task<Result<ReplayDecision>> DecideAsync(ReplaySetup setup, ReplayBook book, DateOnly day, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<Guid>>> ExitsAsync(ReplaySetup setup, ReplayBook book, DateOnly day, CancellationToken cancellationToken);
}

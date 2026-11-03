namespace InvestmentLab.Backtesting.Replays.Domain.Comparisons;

public interface IReplayComparison
{
    string Code { get; }

    ComparedLine LineOf(ReplayedHistory history);
}

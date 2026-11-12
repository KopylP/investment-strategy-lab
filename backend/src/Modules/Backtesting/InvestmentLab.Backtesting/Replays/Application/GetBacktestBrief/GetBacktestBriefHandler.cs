using InvestmentLab.Backtesting.Replays.Application.Shared;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.GetBacktestBrief;

public sealed record GetBacktestBriefQuery(Guid StrategyId, int Version, bool IncludePoints);

public sealed record BacktestBriefDto(
    Guid StrategyId,
    int StrategyVersion,
    string Status,
    string? FailureReason,
    ReplayResultBriefDto? Result);

public sealed class GetBacktestBriefHandler(IBacktestRepository backtests)
{
    public async Task<Result<BacktestBriefDto>> Handle(GetBacktestBriefQuery query, CancellationToken cancellationToken)
    {
        if (await backtests.FindAsync(query.StrategyId, query.Version, cancellationToken) is not { } backtest)
            return BacktestErrors.StrategyNotFound(query.StrategyId);

        var brief = ReplayResultBrief.Of(backtest.Result, query.IncludePoints);

        return new BacktestBriefDto(
            StrategyId: backtest.StrategyId,
            StrategyVersion: backtest.StrategyVersion,
            Status: backtest.Status.ToString(),
            FailureReason: backtest.FailureReason,
            Result: brief is null ? null : ReplayResultBriefDto.From(brief));
    }
}

using System.ComponentModel;
using InvestmentLab.Backtesting.Replays.Application.CompareBacktests;
using InvestmentLab.Backtesting.Replays.Application.EnsureBacktest;
using InvestmentLab.Backtesting.Replays.Application.GetBacktest;
using InvestmentLab.BuildingBlocks.Mcp;
using ModelContextProtocol.Server;

namespace InvestmentLab.Backtesting.Replays.Api;

[McpServerToolType]
internal static class BacktestTools
{
    [McpServerTool(Name = "get_backtest", ReadOnly = true)]
    [Description("The backtest of a strategy version: status, summary of the strategy and its comparisons, monthly points and its journal.")]
    public static async Task<BacktestDto> GetBacktest(
        Guid strategyId,
        int version,
        EnsureBacktestHandler ensure,
        GetBacktestHandler query,
        CancellationToken cancellationToken)
    {
        var ensured = await ensure.Handle(strategyId, version, cancellationToken);
        var ensuredVersion = ensured.ValueOrThrow();

        var backtest = await query.Handle(strategyId, ensuredVersion, cancellationToken);

        return backtest.ValueOrThrow();
    }

    [McpServerTool(Name = "compare_trials", ReadOnly = true)]
    [Description("Compares two backtests (strategy id, version and, for an experiment, its id) and tells whether they are comparable and how they differ.")]
    public static async Task<BacktestComparisonDto> CompareTrials(
        CompareBacktestsQuery trials,
        CompareBacktestsHandler handler,
        CancellationToken cancellationToken)
    {
        var comparison = await handler.Handle(trials, cancellationToken);

        return comparison.ValueOrThrow();
    }
}

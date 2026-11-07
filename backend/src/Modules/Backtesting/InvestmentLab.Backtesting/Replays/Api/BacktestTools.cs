using System.ComponentModel;
using InvestmentLab.Backtesting.Replays.Application.CompareBacktests;
using InvestmentLab.Backtesting.Replays.Application.EnsureBacktest;
using InvestmentLab.Backtesting.Replays.Application.GetBacktestBrief;
using InvestmentLab.BuildingBlocks.Mcp;
using ModelContextProtocol.Server;

namespace InvestmentLab.Backtesting.Replays.Api;

[McpServerToolType]
internal static class BacktestTools
{
    [McpServerTool(Name = "get_backtest", ReadOnly = true)]
    [Description("The backtest of a strategy version: status and the summary and horizon returns (last 1/3/5 years, calendar years) of the strategy and each comparison.")]
    public static async Task<BacktestBriefDto> GetBacktest(
        Guid strategyId,
        int version,
        EnsureBacktestHandler ensure,
        GetBacktestBriefHandler brief,
        [Description("True to add the monthly points of every series; false or null keeps the reply short.")] bool? includePoints = null,
        CancellationToken cancellationToken = default)
    {
        var ensured = await ensure.Handle(strategyId, version, cancellationToken);
        var ensuredVersion = ensured.ValueOrThrow();

        var query = new GetBacktestBriefQuery(strategyId, ensuredVersion, includePoints ?? false);
        var backtest = await brief.Handle(query, cancellationToken);

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

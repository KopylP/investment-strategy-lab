using System.ComponentModel;
using InvestmentLab.BuildingBlocks.Mcp;
using InvestmentLab.StrategyBuilder.Trials.Application.CancelTrial;
using InvestmentLab.StrategyBuilder.Trials.Application.ListTrialRows;
using InvestmentLab.StrategyBuilder.Trials.Application.RunTrial;
using InvestmentLab.StrategyBuilder.Trials.Application.Shared;
using ModelContextProtocol.Server;

namespace InvestmentLab.StrategyBuilder.Trials.Api;

[McpServerToolType]
internal static class TrialTools
{
    private const int DefaultRows = 30;

    [McpServerTool(Name = "run_trial")]
    [Description("Starts a backtest of a draft in the background. Without an experiment it runs with default conditions; with one it uses the given period, capital, costs and robustness studies. The same rules and conditions return the earlier trial unless rerun is true. Poll list_trials for the status.")]
    public static async Task<TrialRunDto> RunTrial(
        Guid draftId,
        [Description("A short label, the experiment's fixed conditions and robustness studies or null for a quick trial, and rerun to compute again instead of reusing an earlier trial with the same rules and conditions.")] RunTrialRequest trial,
        RunTrialHandler handler,
        CancellationToken cancellationToken)
    {
        var command = trial.CommandFor(draftId);

        var started = await handler.Handle(command, cancellationToken);

        return started.ValueOrThrow();
    }

    [McpServerTool(Name = "list_trials", ReadOnly = true)]
    [Description("A draft's trials, newest first, as compact rows: status, period, headline results, failure and queue position (0 = running). The strategy id and version open get_backtest, the experiment id get_experiment.")]
    public static async Task<IReadOnlyList<TrialRowDto>> ListTrials(
        Guid draftId,
        [Description("Only trials whose label contains this text, or null for all.")] string? labelContains,
        [Description("Only trials with this status (Waiting, Running, Succeeded, Failed or Cancelled), or null for all.")] string? status,
        [Description("How many rows to return at most, or null for 30.")] int? limit,
        ListTrialRowsHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new TrialRowsQuery(
            DraftId: draftId,
            LabelContains: labelContains,
            Status: status,
            Limit: limit ?? DefaultRows);

        var rows = await handler.Handle(query, cancellationToken);

        return rows.ValueOrThrow();
    }

    [McpServerTool(Name = "cancel_trial", Destructive = true)]
    [Description("Cancels a trial that still waits in the queue; a running or finished trial cannot be cancelled.")]
    public static async Task<string> CancelTrial(Guid draftId, int number, CancelTrialHandler handler, CancellationToken cancellationToken)
    {
        var cancelled = await handler.Handle(draftId, number, cancellationToken);
        cancelled.ThrowIfFailed();

        return ToolReplies.Done;
    }
}

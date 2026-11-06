using System.ComponentModel;
using InvestmentLab.BuildingBlocks.Mcp;
using InvestmentLab.StrategyBuilder.Trials.Application.ListTrials;
using InvestmentLab.StrategyBuilder.Trials.Application.RunTrial;
using InvestmentLab.StrategyBuilder.Trials.Application.Shared;
using ModelContextProtocol.Server;

namespace InvestmentLab.StrategyBuilder.Trials.Api;

[McpServerToolType]
internal static class TrialTools
{
    [McpServerTool(Name = "run_trial")]
    [Description("Starts a backtest of a draft in the background. Without an experiment it runs with default conditions; with one it uses the given period, capital, costs and robustness studies. Poll list_trials for the status.")]
    public static async Task<TrialRunDto> RunTrial(
        Guid draftId,
        [Description("A short label, and the experiment's fixed conditions and robustness studies or null for a quick trial.")] RunTrialRequest trial,
        RunTrialHandler handler,
        CancellationToken cancellationToken)
    {
        var command = trial.CommandFor(draftId);

        var started = await handler.Handle(command, cancellationToken);

        return started.ValueOrThrow();
    }

    [McpServerTool(Name = "list_trials", ReadOnly = true)]
    [Description("A draft's trials with their status and headline results; the strategy id and version open the full backtest in get_backtest, the experiment id in get_experiment.")]
    public static async Task<IReadOnlyList<TrialRunDto>> ListTrials(Guid draftId, ListTrialsHandler handler, CancellationToken cancellationToken)
    {
        var trials = await handler.Handle(draftId, cancellationToken);

        return trials.ValueOrThrow();
    }
}

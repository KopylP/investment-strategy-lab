using System.ComponentModel;
using InvestmentLab.Backtesting.Experiments.Application.GetExperimentBrief;
using InvestmentLab.Backtesting.Experiments.Application.GetExperimentHoldings;
using InvestmentLab.Backtesting.Experiments.Application.GetExperimentJournal;
using InvestmentLab.BuildingBlocks.Mcp;
using ModelContextProtocol.Server;

namespace InvestmentLab.Backtesting.Experiments.Api;

[McpServerToolType]
internal static class ExperimentTools
{
    private const int DefaultJournalPage = 50;

    [McpServerTool(Name = "get_experiment", ReadOnly = true)]
    [Description("A trial run as an experiment: its fixed conditions, status, the summary and horizon returns (last 1/3/5 years, calendar years) of the strategy and each comparison, and the robustness studies (out-of-sample, walk-forward, neighbouring parameter values).")]
    public static async Task<ExperimentBriefDto> GetExperiment(
        Guid experimentId,
        [Description("True to add the monthly points of every series; false or null keeps the reply short.")] bool? includePoints,
        GetExperimentBriefHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetExperimentBriefQuery(experimentId, includePoints ?? false);

        var experiment = await handler.Handle(query, cancellationToken);

        return experiment.ValueOrThrow();
    }

    [McpServerTool(Name = "get_experiment_journal", ReadOnly = true)]
    [Description("One page of an experiment's decision journal: what the strategy decided on which day and why. Candidates of each decision only with detail.")]
    public static async Task<ExperimentJournalDto> GetExperimentJournal(
        Guid experimentId,
        [Description("Only entries of this kind, or null for all.")] JournalEntryKind? kind,
        DateOnly? from,
        DateOnly? to,
        int? offset,
        int? limit,
        [Description("True to add the ranked candidates of every decision; false or null keeps entries short.")] bool? detail,
        GetExperimentJournalHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetExperimentJournalQuery(
            ExperimentId: experimentId,
            Kind: kind?.ToString().ToLowerInvariant(),
            From: from,
            To: to,
            Offset: offset ?? 0,
            Limit: limit ?? DefaultJournalPage,
            Detail: detail ?? false);

        var journal = await handler.Handle(query, cancellationToken);

        return journal.ValueOrThrow();
    }

    [McpServerTool(Name = "get_experiment_holdings", ReadOnly = true)]
    [Description("What an experiment's strategy held going into each rebalance: market view of that decision, funds with weights and the cash share (idle cash and the cash fund), plus diagnostics over the whole run: share of unfavourable rebalances, mean invested share, mean fund count and trades per year.")]
    public static async Task<ExperimentHoldingsDto> GetExperimentHoldings(
        Guid experimentId,
        [Description("First rebalance date to list, or null from the start.")] DateOnly? from,
        [Description("Last rebalance date to list, or null to the end.")] DateOnly? to,
        GetExperimentHoldingsHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetExperimentHoldingsQuery(experimentId, from, to);

        var holdings = await handler.Handle(query, cancellationToken);

        return holdings.ValueOrThrow();
    }
}

using System.ComponentModel;
using InvestmentLab.Backtesting.Experiments.Application.GetExperimentBrief;
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
    [Description("One page of an experiment's decision journal: what the strategy decided on which day and why.")]
    public static async Task<ExperimentJournalDto> GetExperimentJournal(
        Guid experimentId,
        [Description("Only entries of this kind, or null for all.")] string? kind,
        DateOnly? from,
        DateOnly? to,
        int? offset,
        int? limit,
        GetExperimentJournalHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetExperimentJournalQuery(
            ExperimentId: experimentId,
            Kind: kind,
            From: from,
            To: to,
            Offset: offset ?? 0,
            Limit: limit ?? DefaultJournalPage);

        var journal = await handler.Handle(query, cancellationToken);

        return journal.ValueOrThrow();
    }
}

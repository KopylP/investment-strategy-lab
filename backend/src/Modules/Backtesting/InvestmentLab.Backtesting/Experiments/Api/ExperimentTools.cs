using System.ComponentModel;
using InvestmentLab.Backtesting.Experiments.Application.GetExperiment;
using InvestmentLab.Backtesting.Experiments.Application.GetExperimentJournal;
using InvestmentLab.BuildingBlocks.Mcp;
using ModelContextProtocol.Server;

namespace InvestmentLab.Backtesting.Experiments.Api;

[McpServerToolType]
internal static class ExperimentTools
{
    private const int DefaultJournalPage = 50;

    [McpServerTool(Name = "get_experiment", ReadOnly = true)]
    [Description("A trial run as an experiment: its fixed conditions, status, result and robustness studies (out-of-sample, walk-forward, neighbouring parameter values).")]
    public static async Task<ExperimentDto> GetExperiment(Guid experimentId, GetExperimentHandler handler, CancellationToken cancellationToken)
    {
        var experiment = await handler.Handle(experimentId, cancellationToken);

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

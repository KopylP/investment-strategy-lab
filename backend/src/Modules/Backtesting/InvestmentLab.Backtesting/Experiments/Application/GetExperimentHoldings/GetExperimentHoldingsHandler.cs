using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperimentHoldings;

public sealed record GetExperimentHoldingsQuery(Guid ExperimentId, DateOnly? From, DateOnly? To);

public sealed record ExperimentHoldingsDto(IReadOnlyList<HoldingsRow> Rows, HoldingsDiagnostics? Diagnostics);

public sealed class GetExperimentHoldingsHandler(IExperimentRepository experiments)
{
    public async Task<Result<ExperimentHoldingsDto>> Handle(GetExperimentHoldingsQuery query, CancellationToken cancellationToken)
    {
        var experiment = await experiments.FindAsync(query.ExperimentId, cancellationToken);
        if (experiment is null)
            return ExperimentErrors.NotFound(query.ExperimentId);

        if (experiment.Result is not { } report)
            return new ExperimentHoldingsDto([], null);

        var labels = report.InstrumentLabels ?? new Dictionary<Guid, string>();
        var holdings = ExperimentHoldings.Of(report.Baseline, labels, report.CashInstrumentId);
        var window = holdings.Between(query.From, query.To);

        return new ExperimentHoldingsDto(window.Rows, window.Diagnostics);
    }
}

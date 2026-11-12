using InvestmentLab.Backtesting.Contracts;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.TrialTable;

public sealed record TrialTableQuery(IReadOnlyList<Guid> DraftIds, string? LabelContains, bool OnlySucceeded);

public sealed record TrialTableRowDto(
    string Draft,
    int Number,
    string? Label,
    string? Period,
    string? Status,
    string? FailureReason,
    StrategyFiguresDto? Strategy,
    HorizonsDto? Horizons,
    double? OutOfSampleSharpe,
    BenchmarkFiguresDto? Benchmark,
    double? BenchmarkAtEqualRiskSharpe);

public sealed record StrategyFiguresDto(double AnnualReturn, double Volatility, double Sharpe, double MaxDrawdown, double? AfterTax);

public sealed record BenchmarkFiguresDto(double AnnualReturn, double Volatility, double Sharpe, double MaxDrawdown);

public sealed record HorizonsDto(double? OneYear, double? ThreeYearsAnnual, double? FiveYearsAnnual, IReadOnlyList<YearReturnDto> Years);

public sealed record YearReturnDto(int Year, double Return, bool Partial);

public sealed class TrialTableHandler(IDraftRepository drafts, IBacktestExperiments outcomes, ITrialExperiments experiments) : IHandler
{
    public async Task<Result<IReadOnlyList<TrialTableRowDto>>> Handle(TrialTableQuery query, CancellationToken cancellationToken)
    {
        var rows = new List<TrialTableRowDto>();

        foreach (var draftId in query.DraftIds)
        {
            var draft = await drafts.FindAsync(draftId, cancellationToken);
            if (draft is null)
                return DraftErrors.NotFound(draftId);

            var trials = NewestExperimentsWithLabel(draft.Trials, query.LabelContains);
            foreach (var trial in trials)
            {
                if (await RowOfAsync(draft.Name, trial, query.OnlySucceeded, cancellationToken) is { } row)
                    rows.Add(row);
            }
        }

        return rows;
    }

    private static IEnumerable<TrialRun> NewestExperimentsWithLabel(IReadOnlyList<TrialRun> trials, string? labelContains)
    {
        var experiments = trials.Where(run => run.ExperimentId is not null).OrderByDescending(run => run.Number);
        if (string.IsNullOrWhiteSpace(labelContains))
            return experiments;

        return experiments.Where(run => run.Label?.Contains(labelContains, StringComparison.OrdinalIgnoreCase) == true);
    }

    private async Task<TrialTableRowDto?> RowOfAsync(string draftName, TrialRun trial, bool onlySucceeded, CancellationToken cancellationToken)
    {
        var experimentId = trial.ExperimentId!.Value;

        var outcome = await outcomes.GetOutcomeAsync(experimentId, cancellationToken);
        if (outcome is not null)
            return SucceededRow(draftName, trial, outcome);

        if (onlySucceeded)
            return null;

        var summary = await experiments.SummaryOfAsync(experimentId, cancellationToken);

        return UnfinishedRow(draftName, trial, summary);
    }

    private static TrialTableRowDto SucceededRow(string draftName, TrialRun trial, ExperimentOutcomeDto outcome) => new(
        Draft: draftName,
        Number: trial.Number,
        Label: trial.Label,
        Period: PeriodOf(outcome.Start, outcome.End),
        Status: null,
        FailureReason: null,
        Strategy: StrategyFiguresOf(outcome.Strategy),
        Horizons: HorizonsOf(outcome.StrategyHorizons),
        OutOfSampleSharpe: outcome.OutOfSampleSharpe,
        Benchmark: BenchmarkFiguresOf(outcome.Benchmark),
        BenchmarkAtEqualRiskSharpe: outcome.BenchmarkAtEqualRiskSharpe);

    private static TrialTableRowDto UnfinishedRow(string draftName, TrialRun trial, TrialSummary summary) => new(
        Draft: draftName,
        Number: trial.Number,
        Label: trial.Label,
        Period: trial.Experiment is { } experiment ? PeriodOf(experiment.Period.Start, experiment.Period.End) : null,
        Status: summary.Status.ToString(),
        FailureReason: summary.FailureReason,
        Strategy: null,
        Horizons: null,
        OutOfSampleSharpe: null,
        Benchmark: null,
        BenchmarkAtEqualRiskSharpe: null);

    private static string PeriodOf(DateOnly start, DateOnly end) => $"{start:yyyy-MM-dd}..{end:yyyy-MM-dd}";

    private static StrategyFiguresDto StrategyFiguresOf(BacktestSummaryDto summary) => new(
        AnnualReturn: summary.AnnualReturn,
        Volatility: summary.Volatility,
        Sharpe: summary.Sharpe,
        MaxDrawdown: summary.MaxDrawdown,
        AfterTax: summary.AnnualReturnAfterTax);

    private static BenchmarkFiguresDto? BenchmarkFiguresOf(BacktestSummaryDto? summary)
    {
        if (summary is null)
            return null;

        return new BenchmarkFiguresDto(summary.AnnualReturn, summary.Volatility, summary.Sharpe, summary.MaxDrawdown);
    }

    private static HorizonsDto HorizonsOf(SeriesHorizonsDto horizons)
    {
        var years = horizons.Years.Select(year => new YearReturnDto(year.Year, year.Return, year.Partial)).ToList();

        return new HorizonsDto(horizons.OneYear, horizons.ThreeYearsAnnual, horizons.FiveYearsAnnual, years);
    }
}

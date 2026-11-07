using System.Security.Cryptography;
using System.Text;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed class ResearchPlan(
    ResearchSource source,
    ExperimentConditions conditions,
    IReadOnlyList<ParameterVariant> variants,
    IReadOnlyList<int> requiredHistory)
{
    public ExperimentConditions Conditions => conditions;
    public IReadOnlyList<ParameterVariant> Variants => variants;
    public ParameterVariant Baseline => variants[0];
    public IReadOnlyList<ParameterVariant> SensitivityVariants =>
        conditions.Robustness.Neighborhood is null ? [] : variants;

    public IReadOnlyList<ResearchFold> IndependentTests =>
        conditions.Robustness.IndependentTest(conditions.Period) is { } fold ? [fold] : [];

    public IReadOnlyList<ResearchFold> WalkForwardTests =>
        conditions.Robustness.WalkForward?.FoldsFor(conditions.Period) ?? [];

    public ReplaySetup ReplayFor(ParameterVariant variant, ExperimentPeriod period, bool journal)
    {
        var history = requiredHistory.Append(source.Setup.HistoryBars).Max();
        var experiment = new ReplayExperiment(
            Start: period.Start,
            End: period.End,
            InitialCapital: conditions.InitialCapital,
            Costs: conditions.Costs,
            RecordJournal: journal);

        return source.Setup with
        {
            HistoryBars = history,
            ParametersOverride = variant.Parameters,
            Experiment = experiment,
        };
    }

    public Result<ResearchReport> ReportOf(ReplayResult baseline, IReadOnlyList<ResearchStudy> studies)
    {
        var parameters = Encoding.UTF8.GetBytes(source.Parameters);
        var hash = Convert.ToHexStringLower(SHA256.HashData(parameters));
        var engineVersion = EngineVersionOf(baseline);
        if (engineVersion is null)
            return ExperimentErrors.MissingEngineVersion();

        return new ResearchReport(
            Baseline: baseline,
            Studies: studies,
            ParametersHash: hash,
            EngineVersion: engineVersion,
            InstrumentLabels: source.Setup.InstrumentLabels,
            CashInstrumentId: source.Setup.CashInstrumentId);
    }

    private static string? EngineVersionOf(ReplayResult result)
    {
        var journal = result.Journal ?? [];
        var versions = journal.Select(entry => entry.Explanation?.EngineVersion);

        return versions.FirstOrDefault(version => !string.IsNullOrWhiteSpace(version));
    }
}

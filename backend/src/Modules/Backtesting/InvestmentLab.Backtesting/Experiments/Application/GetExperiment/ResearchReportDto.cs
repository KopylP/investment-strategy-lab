using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Application.Shared;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperiment;

public sealed record ResearchReportDto(
    ReplayResultDto Baseline,
    IReadOnlyList<ResearchStudyDto> Studies,
    string ParametersHash,
    string EngineVersion,
    string SelectionMetric,
    string ReplayModel,
    IReadOnlyDictionary<Guid, string>? InstrumentLabels,
    Guid? CashInstrumentId)
{
    public static ResearchReportDto From(ResearchReport researchReport) => new(
        Baseline: ReplayResultDto.From(researchReport.Baseline),
        Studies: [.. researchReport.Studies.Select(ResearchStudyDto.From)],
        ParametersHash: researchReport.ParametersHash,
        EngineVersion: researchReport.EngineVersion,
        SelectionMetric: researchReport.SelectionMetric,
        ReplayModel: researchReport.ReplayModel,
        InstrumentLabels: researchReport.InstrumentLabels,
        CashInstrumentId: researchReport.CashInstrumentId);
}

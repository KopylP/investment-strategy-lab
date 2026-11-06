namespace InvestmentLab.Strategies.Definitions.Domain;

public sealed record ParametersCheck(IReadOnlyList<ParameterProblem> Problems, IReadOnlyList<Guid> SignalIds, int HistoryBars = 260)
{
    public bool IsClean => Problems.Count == 0;
}

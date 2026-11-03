namespace InvestmentLab.Strategies.Definitions.Domain;

public sealed record ParametersCheck(IReadOnlyList<ParameterProblem> Problems, IReadOnlyList<Guid> SignalIds)
{
    public bool IsClean => Problems.Count == 0;
}

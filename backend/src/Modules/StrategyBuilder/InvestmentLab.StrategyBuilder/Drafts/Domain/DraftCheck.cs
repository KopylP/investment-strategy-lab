namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record DraftCheck(IReadOnlyList<DraftProblem> Problems, GeneratedRules? Rules, IReadOnlyList<Guid> SignalIds)
{
    public bool IsClean => Problems.All(problem => problem.Severity != ProblemSeverity.Error);

    public static bool HasErrors(IEnumerable<DraftProblem> problems) => problems.Any(problem => problem.Severity == ProblemSeverity.Error);
}

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class EngineProblems
{
    public static IReadOnlyList<DraftProblem> Placed(IReadOnlyList<EngineProblem> problems, RuleSourceMap map, StrategyGraph graph) =>
        [.. problems.Select(problem => PlacedOne(problem, map, graph))];

    private static DraftProblem PlacedOne(EngineProblem problem, RuleSourceMap map, StrategyGraph graph)
    {
        var node = map.NodeAt(problem.Path);
        var block = node is null ? null : graph.Node(node)?.Block;

        return new DraftProblem(problem.Code, null, node, block, null, ProblemSeverity.Error, problem.Message);
    }
}

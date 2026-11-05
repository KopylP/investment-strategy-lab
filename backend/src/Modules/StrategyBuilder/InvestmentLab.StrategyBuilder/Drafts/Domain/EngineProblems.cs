namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class EngineProblems
{
    public static IReadOnlyList<DraftProblem> Placed(IReadOnlyList<EngineProblem> problems, RuleSourceMap map, StrategyGraph graph) =>
        [.. problems.Select(problem => PlacedOne(problem, map, graph))];

    private static DraftProblem PlacedOne(EngineProblem problem, RuleSourceMap map, StrategyGraph graph)
    {
        var node = map.NodeAt(problem.Path);
        var found = node is null ? null : graph.Node(node);
        BlockKind? block = problem.Path.Split('.')[0] switch
        {
            "marketFilter" => BlockKind.MarketFilter,
            "entry" => BlockKind.Entry,
            "exit" => BlockKind.Exit,
            "selection" or "weights" => BlockKind.Selection,
            "risk" => BlockKind.Risk,
            _ => found is not null && NodeKinds.IsResult(found.Kind) ? found.Block : null,
        };

        return new DraftProblem(problem.Code, null, node, block, null, ProblemSeverity.Error, problem.Message);
    }
}

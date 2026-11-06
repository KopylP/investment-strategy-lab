namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

// The candidate stream is explicit. Bypassed gates and selectors do not participate in execution.
public static class PortfolioFlow
{
    public static readonly NodeId SelectionId = new("fund-selection");
    public static readonly NodeId PortfolioId = new("strategy-portfolio");
    public static readonly NodeId CashId = new("strategy-cash");

    public static StrategyGraph Upgrade(StrategyGraph graph)
    {
        var legacy = !graph.Nodes.Any(node => node.Kind is NodeKind.Cash or NodeKind.FundSelection);
        var existingWeights = graph.Node(StrategyGraph.SelectionResultId);
        var needsPortfolio = graph.Node(PortfolioId) is null;
        if (!legacy && !needsPortfolio && graph.Node(CashId) is not null &&
            !(existingWeights?.Parameters.Values.Keys.Any(ResultFields.Selection.Contains) ?? false) &&
            !graph.Links.Any(link => (link.From == StrategyGraph.SetupId && link.Output is "cashId" or "universe") || (link.To == StrategyGraph.SelectionResultId && link.Input == NodeSockets.RankBy)))
            return graph;
        var nodes = graph.Nodes.ToList();
        var links = graph.Links.Select(link => link.From == StrategyGraph.SetupId && link.Output == "cashId"
            ? link with { From = CashId, Output = "series" }
            : link.From == StrategyGraph.SetupId && link.Output == "universe" ? link with { Output = NodeSockets.Funds } : link).ToList();
        if (!nodes.Any(node => node.Id == CashId))
            nodes.Add(new GraphNode(CashId, NodeKind.Cash, BlockKind.Shared, NodeParameters.Empty));
        var weights = graph.Node(StrategyGraph.SelectionResultId);
        if (weights is not null)
        {
            var oldFields = weights.Parameters.Values.Where(pair => ResultFields.Selection.Contains(pair.Key)).ToList();
            var oldRanking = links.Where(link => link.To == weights.Id && link.Input == NodeSockets.RankBy).ToList();
            if (legacy || oldFields.Count > 0 || oldRanking.Count > 0)
            {
                var selector = nodes.FirstOrDefault(node => node.Id == SelectionId)
                    ?? new GraphNode(SelectionId, NodeKind.FundSelection, BlockKind.Shared, NodeParameters.Empty);
                foreach (var (key, value) in oldFields) selector = selector with { Parameters = selector.Parameters.With(key, value) };
                nodes.RemoveAll(node => node.Id == selector.Id);
                nodes.Add(selector);
                nodes[nodes.FindIndex(node => node.Id == weights.Id)] = weights with { Parameters = new NodeParameters(weights.Parameters.Values.Where(pair => !ResultFields.Selection.Contains(pair.Key)).ToDictionary()) };
                links.RemoveAll(link => oldRanking.Contains(link));
                links.AddRange(oldRanking.Select(link => link with { To = selector.Id }));
            }
        }
        if (legacy) links.AddRange(DefaultLinks);
        // Complete older graphs once; subsequent edits remain explicit and are validated.
        if (needsPortfolio)
        {
            nodes.Add(new GraphNode(PortfolioId, NodeKind.Portfolio, BlockKind.Shared, NodeParameters.Empty));
            foreach (var link in CompletionLinks.Where(link => !links.Contains(link))) links.Add(link);
        }
        return graph with { Nodes = nodes, Links = links };
    }

    public static IReadOnlyList<GraphLink> CompletionLinks =>
    [
        new(StrategyGraph.SelectionResultId, PortfolioId, NodeSockets.Funds),
        new(CashId, PortfolioId, NodeSockets.Cash),
        new(StrategyGraph.RiskResultId, PortfolioId, NodeSockets.Risk),
    ];

    public static IReadOnlyList<GraphLink> DefaultLinks =>
    [
        new(StrategyGraph.SetupId, StrategyGraph.ExitResultId, NodeSockets.Funds, NodeSockets.Funds),
        new(StrategyGraph.ExitResultId, StrategyGraph.EntryResultId, NodeSockets.Funds),
        new(StrategyGraph.EntryResultId, SelectionId, NodeSockets.Funds),
        new(SelectionId, StrategyGraph.SelectionResultId, NodeSockets.Funds),
        new(StrategyGraph.MarketResultId, StrategyGraph.SelectionResultId, NodeSockets.Budget),
        new(StrategyGraph.SelectionResultId, CashId, NodeSockets.Remainder, "cash"),
    ];

    public static IReadOnlyList<GraphNode> CandidatePath(StrategyGraph graph)
    {
        var path = new List<GraphNode>();
        var seen = new HashSet<NodeId>();
        var current = graph.Node(StrategyGraph.SelectionResultId);
        while (current is not null && seen.Add(current.Id))
        {
            path.Add(current);
            var source = graph.Into(current.Id, NodeSockets.Funds).FirstOrDefault();
            current = source is null ? null : graph.Node(source.From);
        }
        return path;
    }

    public static bool Uses(StrategyGraph graph, NodeId id) => CandidatePath(graph).Any(node => node.Id == id);

    public static GraphNode? Selector(StrategyGraph graph) => CandidatePath(graph).FirstOrDefault(node => node.Kind == NodeKind.FundSelection);
}

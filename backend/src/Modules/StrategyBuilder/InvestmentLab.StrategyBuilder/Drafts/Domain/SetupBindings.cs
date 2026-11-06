namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

// Bindings stay symbolic: changing setup must update all consumers, not copy stale instrument ids.
public static class SetupBindings
{
    public static StrategyGraph WithLegacyBindings(StrategyGraph graph, bool force = false)
    {
        if (!force && graph.Nodes.Any(node => node.Id == StrategyGraph.SetupId)) return graph;
        var nodes = graph.Nodes.Any(node => node.Id == StrategyGraph.SetupId) ? graph.Nodes : [.. graph.Nodes, new GraphNode(StrategyGraph.SetupId, NodeKind.Setup, BlockKind.Shared, NodeParameters.Empty)];
        var links = graph.Links.ToList();
        foreach (var node in nodes.Where(node => node.Kind == NodeKind.Indicator && !graph.Into(node.Id, NodeSockets.Of).Any()))
        {
            var series = SeriesChoice.Of(node.Parameters);
            var output = series?.Series switch
            {
                SeriesChoice.Fund => "funds",
                SeriesChoice.Benchmark => "benchmarkId",
                "cash" => "cashId",
                SeriesChoice.VolatilityIndex => "volatilityIndexId",
                SeriesChoice.Extra when series.InstrumentId is { } id => $"extra:{id}",
                _ => null,
            };
            if (output is not null) links.Add(new GraphLink(StrategyGraph.SetupId, node.Id, NodeSockets.Of, output));
        }
        return new StrategyGraph(nodes, links);
    }

    public static GraphNode Resolve(GraphNode node, StrategyGraph graph, DraftSetup setup)
    {
        if (node.Kind != NodeKind.Indicator) return node;
        var binding = graph.Into(node.Id, NodeSockets.Of).FirstOrDefault();
        if (binding is null || (binding.From != StrategyGraph.SetupId && binding.From != PortfolioFlow.CashId)) return node;
        var output = binding.From == PortfolioFlow.CashId && binding.Output == "series" ? "cashId" : binding.Output;
        object? value = output switch
        {
            "funds" => new { series = "fund" },
            "benchmarkId" => new { series = "benchmark" },
            "volatilityIndexId" => new { series = "volatilityIndex" },
            "cashId" => new { series = "extra", instrumentId = setup.CashId },
            _ when binding.Output.StartsWith("extra:", StringComparison.Ordinal) && Guid.TryParse(binding.Output[6..], out var id) => new { series = "extra", instrumentId = (Guid?)id },
            _ => null,
        };
        return value is null ? node : node with { Parameters = node.Parameters.WithObject("of", value) };
    }

    public static DraftSetup EvaluationSetup(StrategyDraft draft)
    {
        if (draft.Setup.CashId is not { } cash || !draft.Graph.Links.Any(link => (link.From == StrategyGraph.SetupId && link.Output == "cashId") || (link.From == PortfolioFlow.CashId && link.Output == "series"))) return draft.Setup;
        return draft.Setup with { ExtraSeries = [.. draft.Setup.ExtraSeries.Append(cash).Distinct()] };
    }
}

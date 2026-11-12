using System.Text.Json;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public static class DraftMapping
{
    public static DraftDto ToDto(StrategyDraft draft, IReadOnlyList<DraftProblem> problems, IReadOnlyList<BlockWords> words) =>
        new(
            Id: draft.Id,
            Name: draft.Name,
            Description: draft.Description,
            Revision: draft.Revision,
            UpdatedAt: draft.UpdatedAt,
            Setup: ToDto(draft.Setup),
            Graph: ToDto(draft.Graph),
            Layout: ToDto(draft.Layout),
            Reports: [.. draft.Reports.Select(ToDto)],
            OriginStrategyId: draft.Origin?.StrategyId,
            OriginVersion: draft.Origin?.Version,
            Problems: [.. problems.Select(ToDto)],
            Words: [.. words.Select(ToDto)]);

    public static DraftProblemDto ToDto(DraftProblem problem) =>
        new(problem.Message, problem.Severity.ToString(), problem.Node?.Value, problem.Block?.ToString(), problem.ReportCode);

    public static Result<DraftChange> ToChange(DraftChangeDto change)
    {
        var setup = change.Setup is null ? null : ToSetup(change.Setup);
        if (setup is { IsFailure: true })
            return setup.Error;

        var graph = change.Graph is null ? null : ToGraph(change.Graph);
        if (graph is { IsFailure: true })
            return graph.Error;

        var reports = change.Reports?.Select(ToReport).ToList();

        return new DraftChange(change.Revision, change.Name, change.Description, setup?.Value, graph?.Value, reports);
    }

    public static Result<DraftLayout> ToLayout(LayoutDto layout)
    {
        var collapsed = new List<BlockKind>();
        foreach (var block in layout.Collapsed)
        {
            if (!Enum.TryParse<BlockKind>(block, out var kind))
                return DraftErrors.GraphInvalid($"unknown block '{block}'");
            collapsed.Add(kind);
        }

        var positions = layout.Positions.ToDictionary(pair => pair.Key, pair => ToPosition(pair.Value));
        var routes = ToRoutes(layout.EdgeRoutes);
        if (routes.Values.Any(points => points.Count > 128 || points.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y))))
            return DraftErrors.GraphInvalid("edge routes must contain finite coordinates and at most 128 points");
        return new DraftLayout(positions, collapsed) { EdgeRoutes = routes };
    }

    private static Dictionary<string, IReadOnlyList<NodePosition>> ToRoutes(IReadOnlyDictionary<string, IReadOnlyList<NodePositionDto>>? routes)
    {
        var given = routes ?? new Dictionary<string, IReadOnlyList<NodePositionDto>>();

        return given.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<NodePosition>)[.. pair.Value.Select(ToPosition)]);
    }

    private static Result<DraftSetup> ToSetup(DraftSetupDto setup) =>
        DraftSetup.TryOf(setup.Funds ?? [], setup.BenchmarkId, setup.CashId, setup.VolatilityIndexId, setup.ExtraSeries ?? []);

    private static Result<StrategyGraph> ToGraph(GraphDto graph)
    {
        var nodes = new List<GraphNode>();
        foreach (var node in graph.Nodes)
        {
            if (!Enum.TryParse<NodeKind>(node.Kind, out var kind) || !Enum.TryParse<BlockKind>(node.Block, out var block))
                return DraftErrors.GraphInvalid($"node '{node.Id}' has kind '{node.Kind}' in block '{node.Block}'");
            if (NodeKinds.IsResult(kind) && block != NodeKinds.ResultBlock(kind))
                return DraftErrors.GraphInvalid($"result '{node.Id}' has an incompatible purpose");
            nodes.Add(new GraphNode(new NodeId(node.Id), kind, NodeKinds.IsResult(kind) ? block : BlockKind.Shared, new NodeParameters(node.Parameters ?? new Dictionary<string, JsonElement>())));
        }

        var links = graph.Links.Select(link => new GraphLink(new NodeId(link.From), new NodeId(link.To), link.Input, link.Output ?? "output")).ToList();

        if (nodes.Select(node => node.Id).Distinct().Count() != nodes.Count)
            return DraftErrors.GraphInvalid("node identifiers must be unique");
        if (nodes.Any(node => (node.Id == PortfolioFlow.PortfolioId) != (node.Kind == NodeKind.Portfolio)))
            return DraftErrors.GraphInvalid("the portfolio node has a permanent identifier");
        if (nodes.Any(node => (node.Id == PortfolioFlow.CashId) != (node.Kind == NodeKind.Cash)))
            return DraftErrors.GraphInvalid("the cash node has a permanent identifier");
        if (nodes.Any(node => (node.Id == StrategyGraph.SetupId) != (node.Kind == NodeKind.Setup)))
            return DraftErrors.GraphInvalid("the setup node has a permanent identifier");
        return WithAllResults(new StrategyGraph(nodes, links));
    }

    private static Result<StrategyGraph> WithAllResults(StrategyGraph graph)
    {
        var missing = StrategyGraph.Empty.Nodes.Where(result => (NodeKinds.IsResult(result.Kind) || result.Kind == NodeKind.Setup) && graph.Node(result.Id) is null);

        return PortfolioFlow.Upgrade(graph with { Nodes = [.. graph.Nodes, .. missing] });
    }

    private static DraftReport ToReport(DraftReportDto report) => new(report.Code, ToTiming(report.Schedule), report.SignalKinds ?? [], report.SignalIds ?? []);

    private static ReportTiming ToTiming(ReportTimingDto timing) => new(timing.Frequency, timing.Day, timing.Weekday, timing.Time, timing.Minute);

    private static NodePosition ToPosition(NodePositionDto position) => new(position.X, position.Y);

    private static DraftSetupDto ToDto(DraftSetup setup) => new(setup.Funds, setup.BenchmarkId, setup.CashId, setup.VolatilityIndexId, setup.ExtraSeries);

    private static GraphDto ToDto(StrategyGraph source)
    {
        var graph = PortfolioFlow.Upgrade(SetupBindings.WithLegacyBindings(source));
        return new(
            [.. graph.Nodes.Select(node => new NodeDto(node.Id.Value, node.Kind.ToString(), NodeKinds.IsResult(node.Kind) ? node.Block.ToString() : nameof(BlockKind.Shared), node.Parameters.Values))],
            [.. graph.Links.Select(link => new LinkDto(link.From.Value, link.To.Value, link.Input, link.Output))]);
    }

    private static LayoutDto ToDto(DraftLayout layout)
    {
        var positions = layout.Positions.ToDictionary(pair => pair.Key, pair => ToDto(pair.Value));
        var collapsed = layout.Collapsed.Select(block => block.ToString()).ToList();
        var routes = layout.EdgeRoutes.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<NodePositionDto>)[.. pair.Value.Select(ToDto)]);

        return new LayoutDto(positions, collapsed, routes);
    }

    private static NodePositionDto ToDto(NodePosition position) => new(position.X, position.Y);

    private static DraftReportDto ToDto(DraftReport report) => new(report.Code, ToDto(report.Schedule), report.SignalKinds, report.SignalIds);

    private static ReportTimingDto ToDto(ReportTiming timing) => new(timing.Frequency, timing.Day, timing.Weekday, timing.Time, timing.Minute);

    private static BlockWordsDto ToDto(BlockWords words) => new(words.Block.ToString(), words.Sentence);
}

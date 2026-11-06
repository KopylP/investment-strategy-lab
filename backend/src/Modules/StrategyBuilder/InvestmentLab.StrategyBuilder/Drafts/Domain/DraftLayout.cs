namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record DraftLayout(IReadOnlyDictionary<string, NodePosition> Positions, IReadOnlyList<BlockKind> Collapsed)
{
    public IReadOnlyDictionary<string, IReadOnlyList<NodePosition>> EdgeRoutes { get; init; } = new Dictionary<string, IReadOnlyList<NodePosition>>();

    public static DraftLayout Empty { get; } = new(new Dictionary<string, NodePosition>(), []);

    public DraftLayout Keeping(StrategyGraph graph)
    {
        var ids = graph.Nodes.Select(node => node.Id.Value).ToHashSet();
        var kept = Positions.Where(position => ids.Contains(position.Key)).ToDictionary();

        var links = graph.Links.Select(link => link.RouteKey()).ToHashSet();
        return this with { Positions = kept, EdgeRoutes = EdgeRoutes.Where(route => links.Contains(route.Key)).ToDictionary() };
    }

    public double YOf(NodeId node) => Positions.TryGetValue(node.Value, out var position) ? position.Y : double.MaxValue;
}

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record DraftLayout(IReadOnlyDictionary<string, NodePosition> Positions, IReadOnlyList<BlockKind> Collapsed)
{
    public static DraftLayout Empty { get; } = new(new Dictionary<string, NodePosition>(), []);

    public DraftLayout Keeping(StrategyGraph graph)
    {
        var ids = graph.Nodes.Select(node => node.Id.Value).ToHashSet();
        var kept = Positions.Where(position => ids.Contains(position.Key)).ToDictionary();

        return this with { Positions = kept };
    }

    public double YOf(NodeId node) => Positions.TryGetValue(node.Value, out var position) ? position.Y : double.MaxValue;
}

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record StrategyGraph(IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphLink> Links)
{
    public static readonly NodeId MarketResultId = new("result-market");
    public static readonly NodeId ExitResultId = new("result-exit");
    public static readonly NodeId EntryResultId = new("result-entry");
    public static readonly NodeId SelectionResultId = new("result-selection");
    public static readonly NodeId RiskResultId = new("result-risk");

    public static StrategyGraph Empty { get; } = new(
        [
            new GraphNode(MarketResultId, NodeKind.MarketResult, BlockKind.MarketFilter, NodeParameters.Empty),
            new GraphNode(ExitResultId, NodeKind.ExitResult, BlockKind.Exit, NodeParameters.Empty),
            new GraphNode(EntryResultId, NodeKind.EntryResult, BlockKind.Entry, NodeParameters.Empty),
            new GraphNode(SelectionResultId, NodeKind.SelectionResult, BlockKind.Selection, DefaultSelection()),
            new GraphNode(RiskResultId, NodeKind.RiskResult, BlockKind.Risk, NodeParameters.Empty),
        ],
        []);

    public GraphNode? Node(NodeId id) => Nodes.FirstOrDefault(node => node.Id == id);

    public IEnumerable<GraphLink> Into(NodeId id) => Links.Where(link => link.To == id);

    public IEnumerable<GraphLink> Into(NodeId id, string input) => Links.Where(link => link.To == id && link.Input == input);

    public IEnumerable<GraphLink> OutOf(NodeId id) => Links.Where(link => link.From == id);

    public GraphNode ResultOf(BlockKind block) => Nodes.First(node => node.Block == block && NodeKinds.IsResult(node.Kind));

    // The result defines a branch's purpose; intermediate nodes can feed several results.
    public IReadOnlyList<GraphNode> ResultsUsing(NodeId id)
    {
        var visited = new HashSet<NodeId>();
        var pending = new Stack<NodeId>();
        var results = new List<GraphNode>();
        pending.Push(id);
        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current)) continue;
            if (Node(current) is { } node && NodeKinds.IsResult(node.Kind)) results.Add(node);
            foreach (var link in OutOf(current)) pending.Push(link.To);
        }
        return results;
    }

    public IReadOnlySet<NodeId> DependenciesOf(NodeId id)
    {
        var visited = new HashSet<NodeId>();
        var pending = new Stack<NodeId>();
        pending.Push(id);
        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current)) continue;
            foreach (var link in Into(current)) pending.Push(link.From);
        }
        return visited;
    }

    public StrategyGraph WithNode(GraphNode node) => this with { Nodes = [.. Nodes.Where(existing => existing.Id != node.Id), node] };

    private static NodeParameters DefaultSelection() =>
        NodeParameters.Empty.With("top", 3).With("order", "desc").With("method", "equal");
}

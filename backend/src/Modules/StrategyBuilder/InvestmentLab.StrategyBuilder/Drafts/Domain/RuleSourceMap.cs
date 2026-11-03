namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed class RuleSourceMap
{
    private readonly Dictionary<string, NodeId> _nodes = [];
    private readonly Dictionary<string, List<NodeId>> _indicatorNodes = [];

    public NodeId? NodeAt(string path)
    {
        var segments = path.Split('.');

        for (var length = segments.Length; length > 0; length--)
        {
            var prefix = string.Join('.', segments.Take(length));
            if (_nodes.TryGetValue(prefix, out var node))
                return node;
        }

        return null;
    }

    public IReadOnlyList<NodeId> NodesOfIndicator(string name) => _indicatorNodes.GetValueOrDefault(name) ?? [];

    public string? PathOf(NodeId node) => _nodes.Where(entry => entry.Value == node).Select(entry => entry.Key).Order(StringComparer.Ordinal).FirstOrDefault();

    internal void Place(string path, NodeId node) => _nodes.TryAdd(path, node);

    internal void UseIndicator(string name, NodeId node)
    {
        if (!_indicatorNodes.TryGetValue(name, out var nodes))
            _indicatorNodes[name] = nodes = [];
        if (!nodes.Contains(node))
            nodes.Add(node);

        Place($"indicators.{name}", nodes[0]);
    }
}

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class ExitFromEntry
{
    public static bool AcceptsMargin(double margin) => margin is >= 0 and <= 0.5;

    public static StrategyGraph Of(StrategyGraph graph, RuleVocabulary vocabulary, double margin) =>
        new Builder(graph, vocabulary, margin).Build();

    private sealed class Builder(StrategyGraph source, RuleVocabulary vocabulary, double margin)
    {
        private readonly List<GraphNode> _newNodes = [];
        private readonly List<GraphLink> _newLinks = [];
        private int _nextId = 1;

        public StrategyGraph Build()
        {
            var withoutExit = RemoveExitBlock();
            var entryLink = source.Into(StrategyGraph.EntryResultId, NodeSockets.Condition).SingleOrDefault();
            if (entryLink is null)
                return withoutExit;

            var condition = Negate(entryLink.From);
            _newLinks.Add(new GraphLink(condition, StrategyGraph.ExitResultId, NodeSockets.Condition));

            return withoutExit with
            {
                Nodes = [.. withoutExit.Nodes, .. _newNodes],
                Links = [.. withoutExit.Links, .. _newLinks],
            };
        }

        private StrategyGraph RemoveExitBlock()
        {
            var exitDependencies = source.DependenciesOf(StrategyGraph.ExitResultId);
            var otherDependencies = source.Nodes.Where(node => NodeKinds.IsResult(node.Kind) && node.Id != StrategyGraph.ExitResultId)
                .SelectMany(node => source.DependenciesOf(node.Id)).ToHashSet();
            var removed = exitDependencies.Where(id => id != StrategyGraph.ExitResultId && !otherDependencies.Contains(id)).ToHashSet();
            var nodes = source.Nodes.Where(node => !removed.Contains(node.Id)).ToList();
            var links = source.Links.Where(link =>
                !removed.Contains(link.From) &&
                !removed.Contains(link.To) &&
                link.To != StrategyGraph.ExitResultId).ToList();

            return new StrategyGraph(nodes, links);
        }

        private NodeId Negate(NodeId originalId)
        {
            var original = RequiredNode(originalId);

            return original.Kind switch
            {
                NodeKind.Compare => OppositeCompare(original),
                NodeKind.Signal => OppositeSignal(original),
                NodeKind.All => OppositeLogic(original, NodeKind.Any),
                NodeKind.Any => OppositeLogic(original, NodeKind.All),
                NodeKind.Not => CopyCondition(RequiredInput(original.Id, NodeSockets.Condition)),
                NodeKind.Between or NodeKind.Crossed => WrapInNot(CopyCondition(original.Id)),
                _ => throw new InvalidOperationException($"Node '{original.Id}' is not a condition."),
            };
        }

        private NodeId OppositeCompare(GraphNode original)
        {
            var operation = original.Parameters.Text("operator") ?? "";
            var opposite = operation switch
            {
                "gt" or "gte" => "lt",
                "lt" or "lte" => "gt",
                _ => operation,
            };
            var parameters = original.Parameters.With("operator", opposite);
            var value = original.Parameters.Number(NodeSockets.Value);
            if (value is { } number)
                parameters = parameters.With(NodeSockets.Value, number + MarginAdjustment(original, operation));

            var copied = Add(NodeKind.Compare, parameters);
            CopyNumericInputs(original, copied);

            return copied;
        }

        private double MarginAdjustment(GraphNode compare, string operation)
        {
            if (source.Into(compare.Id, NodeSockets.Right).Any() || !LeftIndicatorUsesPercent(compare))
                return 0;

            return operation switch
            {
                "gt" or "gte" => -margin,
                "lt" or "lte" => margin,
                _ => 0,
            };
        }

        private bool LeftIndicatorUsesPercent(GraphNode compare)
        {
            var left = source.Into(compare.Id, NodeSockets.Left).SingleOrDefault();
            var indicator = left is null ? null : source.Node(left.From);
            if (indicator?.Kind != NodeKind.Indicator)
                return false;

            var code = indicator.Parameters.Text("indicator");
            var format = code is null ? null : vocabulary.Indicator(code)?.ColumnFormat;

            return format is "percent" or "signedPercent";
        }

        private NodeId OppositeSignal(GraphNode original)
        {
            var state = original.Parameters.Text("state");
            var opposite = state == "active" ? "inactive" : "active";

            return Add(NodeKind.Signal, original.Parameters.With("state", opposite));
        }

        private NodeId OppositeLogic(GraphNode original, NodeKind oppositeKind)
        {
            var opposite = Add(oppositeKind, original.Parameters);
            foreach (var child in source.Into(original.Id, NodeSockets.Conditions))
                _newLinks.Add(new GraphLink(Negate(child.From), opposite, NodeSockets.Conditions));

            return opposite;
        }

        private NodeId WrapInNot(NodeId condition)
        {
            var not = Add(NodeKind.Not, NodeParameters.Empty);
            _newLinks.Add(new GraphLink(condition, not, NodeSockets.Condition));

            return not;
        }

        private NodeId CopyCondition(NodeId originalId)
        {
            var original = RequiredNode(originalId);
            var copied = Add(original.Kind, original.Parameters);

            foreach (var link in source.Into(original.Id))
            {
                var input = RequiredNode(link.From);
                var copiedInput = NodeKinds.IsCondition(input.Kind) ? CopyCondition(input.Id) : CopyNumber(input.Id);
                _newLinks.Add(new GraphLink(copiedInput, copied, link.Input));
            }

            return copied;
        }

        private void CopyNumericInputs(GraphNode original, NodeId copied)
        {
            foreach (var link in source.Into(original.Id))
                _newLinks.Add(new GraphLink(CopyNumber(link.From), copied, link.Input));
        }

        private NodeId CopyNumber(NodeId originalId)
        {
            // Indicators are immutable definitions and can feed both entry and exit.
            return originalId;
        }

        private NodeId RequiredInput(NodeId nodeId, string input) =>
            source.Into(nodeId, input).Single().From;

        private GraphNode RequiredNode(NodeId id) =>
            source.Node(id) ?? throw new InvalidOperationException($"Node '{id}' does not exist.");

        private NodeId Add(NodeKind kind, NodeParameters parameters)
        {
            var id = NextId();
            _newNodes.Add(new GraphNode(id, kind, BlockKind.Shared, parameters));

            return id;
        }

        private NodeId NextId()
        {
            NodeId id;
            do
                id = new NodeId($"x-{_nextId++}");
            while (source.Node(id) is not null || _newNodes.Any(node => node.Id == id));

            return id;
        }
    }
}

using System.Text.Json.Nodes;

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class RulesGenerator
{
    private const int FormatVersion = 1;

    public static GeneratedRules Generate(StrategyDraft draft)
    {
        var writing = new RulesWriting(draft);

        var rules = new JsonObject { ["formatVersion"] = FormatVersion };
        writing.AddGate(rules, "marketFilter", BlockKind.MarketFilter);
        writing.AddGate(rules, "exit", BlockKind.Exit);
        writing.AddGate(rules, "entry", BlockKind.Entry);
        writing.AddSelection(rules);
        writing.AddRisk(rules);
        rules["indicators"] = writing.Indicators;

        return new GeneratedRules(CanonicalJson.Write(rules), writing.SourceMap);
    }

    private sealed class RulesWriting(StrategyDraft draft)
    {
        private readonly StrategyGraph _graph = draft.Graph;
        private readonly DraftSetup _setup = SetupBindings.EvaluationSetup(draft);

        public JsonObject Indicators { get; } = [];

        public RuleSourceMap SourceMap { get; } = new();

        public void AddGate(JsonObject rules, string section, BlockKind block)
        {
            var result = _graph.ResultOf(block);
            if (block is BlockKind.Entry or BlockKind.Exit && !PortfolioFlow.Uses(_graph, result.Id))
                return;
            if (block == BlockKind.MarketFilter && !_graph.Into(StrategyGraph.SelectionResultId, NodeSockets.Budget).Any(link => link.From == result.Id))
                return;
            if (InputNode(result, NodeSockets.Condition) is not { } condition)
                return;

            SourceMap.Place(section, result.Id);
            var gate = new JsonObject { ["condition"] = Condition(condition, $"{section}.condition") };
            if (block == BlockKind.MarketFilter)
                AddMarketFields(gate, result);

            rules[section] = gate;
        }

        private static void AddMarketFields(JsonObject gate, GraphNode result)
        {
            gate[ResultFields.InvestWhenUnfavorable] = Value(result, ResultFields.InvestWhenUnfavorable);
            if (result.Parameters.Has(ResultFields.WhenUnfavorable))
                gate[ResultFields.WhenUnfavorable] = Value(result, ResultFields.WhenUnfavorable);
        }

        public void AddSelection(JsonObject rules)
        {
            var result = _graph.ResultOf(BlockKind.Selection);
            var selector = PortfolioFlow.Selector(_graph);
            var selection = selector is null
                ? new JsonObject { ["top"] = Math.Max(1, _setup.Funds.Count), ["order"] = "desc" }
                : Fields(selector, ResultFields.Selection);
            if (selector is not null && InputNode(selector, NodeSockets.RankBy) is { } ranking)
                selection["rankBy"] = IndicatorName(ranking, "selection.rankBy");

            SourceMap.Place("selection", selector?.Id ?? result.Id);
            SourceMap.Place("weights", result.Id);
            rules["selection"] = selection;
            rules["weights"] = Fields(result, ResultFields.Weights);
        }

        public void AddRisk(JsonObject rules)
        {
            var result = _graph.ResultOf(BlockKind.Risk);
            var risk = Fields(result, ResultFields.Risk);
            if (risk.Count == 0)
                return;

            SourceMap.Place("risk", result.Id);
            rules["risk"] = risk;
        }

        private JsonObject Condition(GraphNode node, string path)
        {
            SourceMap.Place(path, node.Id);
            var condition = new JsonObject { ["kind"] = NodeKinds.ConditionCode(node.Kind) };

            foreach (var (name, value) in node.Parameters.Values.Where(entry => IsOwnField(node, entry.Key)))
                condition[name] = JsonNode.Parse(value.GetRawText());

            switch (node.Kind)
            {
                case NodeKind.Compare:
                    condition["left"] = Operand(InputNode(node, NodeSockets.Left)!, $"{path}.left");
                    condition["right"] = InputNode(node, NodeSockets.Right) is { } right
                        ? Operand(right, $"{path}.right")
                        : new JsonObject { ["value"] = Value(node, NodeSockets.Value) };
                    break;
                case NodeKind.Between:
                    condition["left"] = Operand(InputNode(node, NodeSockets.Value)!, $"{path}.left");
                    break;
                case NodeKind.Crossed:
                    condition["indicator"] = IndicatorName(InputNode(node, NodeSockets.Indicator)!, $"{path}.indicator");
                    if (InputNode(node, NodeSockets.Other) is { } other)
                        condition["other"] = IndicatorName(other, $"{path}.other");
                    break;
                case NodeKind.All or NodeKind.Any:
                    condition["conditions"] = Children(node, path);
                    break;
                case NodeKind.Not:
                    condition["condition"] = Condition(InputNode(node, NodeSockets.Condition)!, $"{path}.condition");
                    break;
            }

            return condition;
        }

        private static bool IsOwnField(GraphNode node, string name) => !(node.Kind == NodeKind.Compare && name == NodeSockets.Value);

        private JsonArray Children(GraphNode node, string path)
        {
            var children = _graph.Into(node.Id, NodeSockets.Conditions)
                .Select(link => _graph.Node(link.From)!)
                .OrderBy(child => draft.Layout.YOf(child.Id))
                .ThenBy(child => child.Id.Value, StringComparer.Ordinal)
                .ToList();

            return [.. children.Select((child, index) => (JsonNode)Condition(child, $"{path}.conditions.{index}"))];
        }

        private JsonObject Operand(GraphNode indicator, string path) =>
            new() { ["indicator"] = IndicatorName(indicator, $"{path}.indicator") };

        private string IndicatorName(GraphNode indicator, string path)
        {
            indicator = SetupBindings.Resolve(indicator, _graph, _setup);
            var name = IndicatorNaming.NameOf(indicator, _setup);
            SourceMap.Place(path, indicator.Id);
            SourceMap.UseIndicator(name, indicator.Id);
            Indicators[name] = IndicatorDefinition(indicator);

            return name;
        }

        private static JsonObject IndicatorDefinition(GraphNode indicator)
        {
            var definition = new JsonObject { ["kind"] = indicator.Parameters.Text("indicator") };
            foreach (var (name, value) in indicator.Parameters.Values.Where(entry => entry.Key != "indicator"))
                definition[name] = JsonNode.Parse(value.GetRawText());

            return definition;
        }

        private static JsonObject Fields(GraphNode node, IEnumerable<string> names)
        {
            var fields = new JsonObject();
            foreach (var name in names.Where(node.Parameters.Has))
                fields[name] = Value(node, name);

            return fields;
        }

        private static JsonNode? Value(GraphNode node, string name) =>
            node.Parameters.Element(name) is { } value ? JsonNode.Parse(value.GetRawText()) : null;

        private GraphNode? InputNode(GraphNode node, string input) =>
            _graph.Into(node.Id, input).Select(link => _graph.Node(link.From)).FirstOrDefault();
    }
}

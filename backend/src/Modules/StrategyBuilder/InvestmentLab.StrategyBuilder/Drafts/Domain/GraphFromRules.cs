using System.Text.Json;

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class GraphFromRules
{
    private const int KnownFormatVersion = 1;

    private static readonly string[] TopFields = ["formatVersion", "indicators", "marketFilter", "exit", "entry", "selection", "weights", "risk"];
    private static readonly string[] StructuralFields = ["kind", "left", "right", "conditions", "condition"];

    public static Result<ImportedRules> Build(string json, RuleVocabulary vocabulary)
    {
        if (Parsed(json) is not { ValueKind: JsonValueKind.Object } rules)
            return ImportErrors.NotJson();
        if (FormatVersionOf(rules) is { } version)
            return ImportErrors.FormatVersion(version);

        try
        {
            var reading = new RulesReading(rules, vocabulary);

            return reading.Read();
        }
        catch (ImportRefused refused)
        {
            return refused.Error;
        }
    }

    private static JsonElement? Parsed(string json)
    {
        try
        {
            return JsonDocument.Parse(json).RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FormatVersionOf(JsonElement rules)
    {
        if (rules.TryGetProperty("formatVersion", out var version) && version.ValueKind == JsonValueKind.Number && version.GetDouble() == KnownFormatVersion)
            return null;

        return rules.TryGetProperty("formatVersion", out var given) ? given.GetRawText() : "null";
    }

    private sealed class ImportRefused(Error error) : Exception(error.Message)
    {
        public Error Error { get; } = error;
    }

    private sealed class RulesReading(JsonElement rules, RuleVocabulary vocabulary)
    {
        private readonly Dictionary<string, JsonElement> _indicators = [];
        private readonly List<Guid> _extraSeries = [];
        private StrategyGraph _graph = StrategyGraph.Empty;
        private int _nextNode = 1;

        public ImportedRules Read()
        {
            RefuseUnknown(rules, TopFields, "");
            ReadIndicators();

            ReadGate("marketFilter", BlockKind.MarketFilter, [ResultFields.InvestWhenUnfavorable]);
            ReadGate("exit", BlockKind.Exit, []);
            ReadGate("entry", BlockKind.Entry, []);
            ReadSelection();
            ReadResultFields("risk", BlockKind.Risk, ResultFields.Risk);

            return new ImportedRules(_graph, _extraSeries);
        }

        private void ReadIndicators()
        {
            if (!rules.TryGetProperty("indicators", out var indicators))
                return;

            foreach (var indicator in indicators.EnumerateObject())
            {
                var path = $"indicators.{indicator.Name}";
                var kind = indicator.Value.TryGetProperty("kind", out var given) ? given.GetString() ?? "" : "";
                if (vocabulary.Indicator(kind) is not { } known)
                    throw new ImportRefused(ImportErrors.IndicatorUnknown(kind, path));

                RefuseUnknown(indicator.Value, ["kind", .. known.Fields.Select(field => field.Name)], path);
                _indicators[indicator.Name] = indicator.Value;
                NoteExtraSeries(indicator.Value);
            }
        }

        private void NoteExtraSeries(JsonElement indicator)
        {
            if (!indicator.TryGetProperty("of", out var of) || !of.TryGetProperty("instrumentId", out var id))
                return;

            if (Guid.TryParse(id.GetString(), out var instrument) && !_extraSeries.Contains(instrument))
                _extraSeries.Add(instrument);
        }

        private void ReadGate(string section, BlockKind block, string[] resultFields)
        {
            if (!rules.TryGetProperty(section, out var gate))
                return;

            RefuseUnknown(gate, ["condition", .. resultFields], section);
            var result = _graph.ResultOf(block);
            var condition = ReadCondition(gate.GetProperty("condition"), block, $"{section}.condition");
            Link(condition, result.Id, NodeSockets.Condition);
            SetResultFields(result, gate, resultFields);
        }

        private void ReadSelection()
        {
            var result = _graph.ResultOf(BlockKind.Selection);
            if (rules.TryGetProperty("selection", out var selection))
            {
                RefuseUnknown(selection, [.. ResultFields.Selection, "rankBy"], "selection");
                if (selection.TryGetProperty("rankBy", out var rankBy))
                    Link(IndicatorNode(rankBy.GetString()!, BlockKind.Selection, "selection.rankBy"), result.Id, NodeSockets.RankBy);
            }

            ReadResultFields("selection", BlockKind.Selection, ResultFields.Selection, replace: true);
            ReadResultFields("weights", BlockKind.Selection, ResultFields.Weights);
        }

        private void ReadResultFields(string section, BlockKind block, string[] fields, bool replace = false)
        {
            var result = _graph.ResultOf(block);
            if (replace)
                result = Replace(result with { Parameters = NodeParameters.Empty });
            if (!rules.TryGetProperty(section, out var values))
                return;

            if (section != "selection")
                RefuseUnknown(values, fields, section);
            SetResultFields(result, values, fields);
        }

        private void SetResultFields(GraphNode result, JsonElement values, IEnumerable<string> fields)
        {
            var parameters = _graph.Node(result.Id)!.Parameters;
            foreach (var field in fields)
            {
                if (values.TryGetProperty(field, out var value) && value.ValueKind != JsonValueKind.Null)
                    parameters = parameters.With(field, value);
            }

            Replace(result with { Parameters = parameters });
        }

        private NodeId ReadCondition(JsonElement condition, BlockKind block, string path)
        {
            var code = condition.TryGetProperty("kind", out var kind) ? kind.GetString() ?? "" : "";
            if (vocabulary.Condition(code) is not { } known)
                throw new ImportRefused(ImportErrors.ConditionUnknown(code, path));

            RefuseUnknown(condition, [.. StructuralFields, .. known.Fields.Select(field => field.Name)], path);
            var node = Add(NodeKinds.OfConditionCode(code), block, OwnParameters(condition, code));

            switch (node.Kind)
            {
                case NodeKind.Compare:
                    ReadOperand(condition.GetProperty("left"), node, NodeSockets.Left, $"{path}.left");
                    ReadRightSide(condition.GetProperty("right"), node, $"{path}.right");
                    break;
                case NodeKind.Between:
                    ReadOperand(condition.GetProperty("left"), node, NodeSockets.Value, $"{path}.left");
                    break;
                case NodeKind.Crossed:
                    Link(IndicatorNode(condition.GetProperty("indicator").GetString()!, block, $"{path}.indicator"), node.Id, NodeSockets.Indicator);
                    if (condition.TryGetProperty("other", out var other) && other.ValueKind == JsonValueKind.String)
                        Link(IndicatorNode(other.GetString()!, block, $"{path}.other"), node.Id, NodeSockets.Other);
                    break;
                case NodeKind.All or NodeKind.Any:
                    var index = 0;
                    foreach (var child in condition.GetProperty("conditions").EnumerateArray())
                        Link(ReadCondition(child, block, $"{path}.conditions.{index++}"), node.Id, NodeSockets.Conditions);
                    break;
                case NodeKind.Not:
                    Link(ReadCondition(condition.GetProperty("condition"), block, $"{path}.condition"), node.Id, NodeSockets.Condition);
                    break;
            }

            return node.Id;
        }

        private static NodeParameters OwnParameters(JsonElement condition, string code)
        {
            var parameters = NodeParameters.Empty;
            var linked = code == "crossed" ? ["kind", "indicator", "other"] : StructuralFields;
            foreach (var property in condition.EnumerateObject().Where(property => !linked.Contains(property.Name)))
                parameters = parameters.With(property.Name, property.Value);

            return parameters;
        }

        private void ReadOperand(JsonElement operand, GraphNode node, string input, string path)
        {
            RefuseUnknown(operand, ["indicator"], path);
            Link(IndicatorNode(operand.GetProperty("indicator").GetString()!, node.Block, $"{path}.indicator"), node.Id, input);
        }

        private void ReadRightSide(JsonElement right, GraphNode node, string path)
        {
            if (right.TryGetProperty("indicator", out _))
            {
                ReadOperand(right, node, NodeSockets.Right, path);
                return;
            }

            RefuseUnknown(right, [NodeSockets.Value], path);
            Replace(_graph.Node(node.Id)! with { Parameters = _graph.Node(node.Id)!.Parameters.With(NodeSockets.Value, right.GetProperty(NodeSockets.Value)) });
        }

        private NodeId IndicatorNode(string name, BlockKind block, string path)
        {
            if (!_indicators.TryGetValue(name, out var definition))
                throw new ImportRefused(ImportErrors.ReferenceUnknown(name, path));

            var parameters = NodeParameters.Empty.With("indicator", definition.GetProperty("kind").GetString()!);
            foreach (var property in definition.EnumerateObject().Where(property => property.Name != "kind"))
                parameters = parameters.With(property.Name, property.Value);

            return Add(NodeKind.Indicator, block, parameters).Id;
        }

        private GraphNode Add(NodeKind kind, BlockKind block, NodeParameters parameters)
        {
            var node = new GraphNode(new NodeId($"n-{_nextNode++:D3}"), kind, block, parameters);
            _graph = _graph with { Nodes = [.. _graph.Nodes, node] };

            return node;
        }

        private GraphNode Replace(GraphNode node)
        {
            _graph = _graph.WithNode(node);

            return node;
        }

        private void Link(NodeId from, NodeId to, string input) =>
            _graph = _graph with { Links = [.. _graph.Links, new GraphLink(from, to, input)] };

        private static void RefuseUnknown(JsonElement value, IReadOnlyCollection<string> known, string path)
        {
            if (value.ValueKind != JsonValueKind.Object)
                return;

            var unknown = value.EnumerateObject().FirstOrDefault(property => !known.Contains(property.Name));
            if (unknown.Value.ValueKind != JsonValueKind.Undefined)
                throw new ImportRefused(ImportErrors.FieldUnknown(path.Length == 0 ? unknown.Name : $"{path}.{unknown.Name}"));
        }
    }
}

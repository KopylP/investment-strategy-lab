using System.Text.Json;

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class StrategyInWords
{
    public static IReadOnlyList<BlockWords> Of(StrategyDraft draft, RuleVocabulary vocabulary, InstrumentLabels labels)
    {
        var speaking = new Speaking(draft, vocabulary, labels);

        return [.. new[] { speaking.Market(), speaking.Gate(BlockKind.Exit, "builder.words.exit"), speaking.Gate(BlockKind.Entry, "builder.words.entry"),
            speaking.Selection(), speaking.Risk() }.OfType<BlockWords>()];
    }

    private sealed class Speaking(StrategyDraft draft, RuleVocabulary vocabulary, InstrumentLabels labels)
    {
        private readonly StrategyGraph _graph = draft.Graph;

        public BlockWords? Market()
        {
            var result = _graph.ResultOf(BlockKind.MarketFilter);
            if (InputNode(result, NodeSockets.Condition) is not { } condition)
                return null;

            var cash = draft.Setup.CashId is { } id ? labels.Of(id) : "";
            var share = result.Parameters.Number(ResultFields.InvestWhenUnfavorable);
            var sentence = Message.Of("builder.words.market", new { condition = Condition(condition), share, cash });

            return new BlockWords(BlockKind.MarketFilter, sentence);
        }

        public BlockWords? Gate(BlockKind block, string code)
        {
            var result = _graph.ResultOf(block);
            if (InputNode(result, NodeSockets.Condition) is not { } condition)
                return null;

            return new BlockWords(block, Message.Of(code, new { condition = Condition(condition) }));
        }

        public BlockWords Selection()
        {
            var result = _graph.ResultOf(BlockKind.Selection);
            var parameters = result.Parameters;
            var rank = InputNode(result, NodeSockets.RankBy) is { } ranking ? Indicator(ranking) : Message.Of("builder.words.universe_order");
            var sentence = Message.Of("builder.words.selection", new
            {
                top = parameters.Integer("top"),
                rank,
                groupLimit = parameters.Integer("maxPerGroup"),
                correlation = parameters.Number("maxCorrelation"),
                weights = ChoiceLabel(vocabulary.BlockFields.GetValueOrDefault("method"), parameters.Text("method")),
            });

            return new BlockWords(BlockKind.Selection, sentence);
        }

        public BlockWords? Risk()
        {
            var parameters = _graph.ResultOf(BlockKind.Risk).Parameters;
            var limits = RiskLimits(parameters).ToList();

            return limits.Count == 0 ? null : new BlockWords(BlockKind.Risk, Message.Of("builder.words.risk", new { limits }));
        }

        private static IEnumerable<string> RiskLimits(NodeParameters parameters)
        {
            if (parameters.Number("stopLossFromCost") is { } stop)
                yield return Message.Of("builder.words.risk.stop", new { limit = stop });
            if (parameters.Number("drawdownFromPeak") is { } drawdown)
                yield return Message.Of("builder.words.risk.drawdown", new { limit = drawdown });
        }

        private string Condition(GraphNode node) => node.Kind switch
        {
            NodeKind.All => Message.Of("builder.words.all", new { items = Children(node) }),
            NodeKind.Any => Message.Of("builder.words.any", new { items = Children(node) }),
            NodeKind.Not => Message.Of("builder.words.not", new { condition = InputNode(node, NodeSockets.Condition) is { } inner ? Condition(inner) : "" }),
            _ => Phrase(node),
        };

        private List<string> Children(GraphNode node) =>
            [.. _graph.Into(node.Id, NodeSockets.Conditions)
                .Select(link => _graph.Node(link.From)!)
                .OrderBy(child => draft.Layout.YOf(child.Id))
                .ThenBy(child => child.Id.Value, StringComparer.Ordinal)
                .Select(Condition)];

        private string Phrase(GraphNode node)
        {
            var kind = vocabulary.Condition(NodeKinds.ConditionCode(node.Kind));
            var parameters = PlainParameters(node.Parameters);
            foreach (var field in kind?.Fields.Where(field => field.Kind == FieldKind.Choice) ?? [])
                parameters[field.Name] = ChoiceLabel(field, node.Parameters.Text(field.Name));

            AddLinkedWords(node, parameters);

            return Message.Of(CodeOf(kind?.Phrase), parameters);
        }

        private void AddLinkedWords(GraphNode node, Dictionary<string, object?> parameters)
        {
            switch (node.Kind)
            {
                case NodeKind.Compare:
                    parameters["left"] = LinkedIndicator(node, NodeSockets.Left);
                    parameters["right"] = InputNode(node, NodeSockets.Right) is { } right ? Indicator(right) : parameters.GetValueOrDefault(NodeSockets.Value);
                    break;
                case NodeKind.Between:
                    parameters["left"] = LinkedIndicator(node, NodeSockets.Value);
                    break;
                case NodeKind.Crossed:
                    parameters["indicator"] = LinkedIndicator(node, NodeSockets.Indicator);
                    parameters["other"] = InputNode(node, NodeSockets.Other) is { } other ? Indicator(other) : null;
                    break;
            }
        }

        private string? LinkedIndicator(GraphNode node, string input) => InputNode(node, input) is { } indicator ? Indicator(indicator) : null;

        private string Indicator(GraphNode node)
        {
            var kind = vocabulary.Indicator(node.Parameters.Text("indicator") ?? "");
            var parameters = PlainParameters(node.Parameters);
            parameters["of"] = SeriesWords(SeriesChoice.Of(node.Parameters));

            return Message.Of(CodeOf(kind?.Phrase), parameters);
        }

        private string SeriesWords(SeriesChoice? series) => series?.Series switch
        {
            SeriesChoice.Fund => Message.Of("builder.words.series.fund"),
            SeriesChoice.Benchmark => Message.Of("builder.words.series.benchmark", new { label = LabelOf(draft.Setup.BenchmarkId) }),
            SeriesChoice.VolatilityIndex => Message.Of("builder.words.series.vix", new { label = LabelOf(draft.Setup.VolatilityIndexId) }),
            SeriesChoice.Extra => Message.Of("builder.words.series.extra", new { label = LabelOf(series.InstrumentId) }),
            _ => "",
        };

        private string LabelOf(Guid? instrument) => instrument is { } id ? labels.Of(id) : "";

        private static Dictionary<string, object?> PlainParameters(NodeParameters parameters) =>
            parameters.Values.Where(entry => entry.Key != "indicator").ToDictionary(entry => entry.Key, entry => (object?)entry.Value);

        private static string? ChoiceLabel(VocabularyField? field, string? value) =>
            value is not null && field?.ChoiceLabels.TryGetValue(value, out var label) == true ? label : value;

        private static string CodeOf(string? phrase) => (phrase ?? "@builder.words.unknown").TrimStart(Message.Prefix);

        private GraphNode? InputNode(GraphNode node, string input) =>
            _graph.Into(node.Id, input).Select(link => _graph.Node(link.From)).FirstOrDefault();
    }
}

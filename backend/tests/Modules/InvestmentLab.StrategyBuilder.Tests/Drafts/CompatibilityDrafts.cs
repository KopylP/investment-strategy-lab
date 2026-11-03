using InvestmentLab.StrategyBuilder.Drafts.Domain;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

internal static class CompatibilityDrafts
{
    private const string FirstSignal = "0199f000-0000-7000-8000-0000000051a1";
    private const string SecondSignal = "0199f000-0000-7000-8000-0000000051a2";

    public static IReadOnlyDictionary<string, StrategyDraft> All()
    {
        var drafts = DraftTemplates.Codes.ToDictionary(code => code, code => Of(DraftTemplates.GraphOf(code)));
        drafts["every-indicator"] = Of(EveryIndicator());
        drafts["every-condition"] = Of(EveryCondition());
        drafts["extra-series"] = Of(ExtraSeries());
        drafts["risk-only-limits"] = Of(RiskOnlyLimits());

        return drafts;
    }

    private static StrategyGraph EveryIndicator()
    {
        var nodes = new List<GraphNode>();
        var links = new List<GraphLink>();

        foreach (var indicator in EngineSchema.Vocabulary.Indicators)
        {
            var id = $"i-{indicator.Code.ToLowerInvariant()}";
            nodes.Add(IndicatorWithSmallestFields(id, indicator));
            nodes.Add(Compare($"{id}-gt", BlockKind.Entry, "gt", 0));
            links.Add(Link(id, $"{id}-gt", "left"));
            links.Add(Link($"{id}-gt", "e-all", "conditions"));
        }

        nodes.Add(Logic("e-all", BlockKind.Entry, NodeKind.All));
        links.Add(Link("e-all", "result-entry", "condition"));

        return Graph(nodes, links);
    }

    private static GraphNode IndicatorWithSmallestFields(string id, IndicatorKind indicator)
    {
        var node = Indicator(id, BlockKind.Entry, indicator.Code);
        var numbers = indicator.Fields.Where(field => field.Kind == FieldKind.Integer && field.Minimum is not null);

        return numbers.Aggregate(node, (current, field) => current with { Parameters = current.Parameters.With(field.Name, field.Minimum!.Value) });
    }

    private static StrategyGraph EveryCondition()
    {
        var crossedLevel = new GraphNode(new NodeId("c-level"), NodeKind.Crossed, BlockKind.Entry,
            NodeParameters.Empty.With("direction", "down").With("level", -0.03).With("withinDays", 21));
        var crossedOther = new GraphNode(new NodeId("c-other"), NodeKind.Crossed, BlockKind.Entry,
            NodeParameters.Empty.With("direction", "up").With("withinDays", 10));
        var between = new GraphNode(new NodeId("c-between"), NodeKind.Between, BlockKind.Entry, NodeParameters.Empty.With("low", -0.1).With("high", 0.2));

        return Graph(
            [
                Indicator("i-trend", BlockKind.Entry, "priceVsSma", days: 50),
                Indicator("i-price", BlockKind.Entry, "price"),
                Indicator("i-sma", BlockKind.Entry, "sma", days: 20),
                between, crossedLevel, crossedOther,
                Signal("s-any", FirstSignal, "active", "anyInstrument"),
                Signal("s-fund", SecondSignal, "inactive", "thisFund"),
                Logic("c-not", BlockKind.Entry, NodeKind.Not),
                Logic("c-any", BlockKind.Entry, NodeKind.Any),
            ],
            [
                Link("i-trend", "c-between", "value"),
                Link("i-trend", "c-level", "indicator"),
                Link("i-price", "c-other", "indicator"),
                Link("i-sma", "c-other", "other"),
                Link("s-fund", "c-not", "condition"),
                Link("c-between", "c-any", "conditions"),
                Link("c-level", "c-any", "conditions"),
                Link("c-other", "c-any", "conditions"),
                Link("s-any", "c-any", "conditions"),
                Link("c-not", "c-any", "conditions"),
                Link("c-any", "result-entry", "condition"),
            ]);
    }

    private static StrategyGraph ExtraSeries()
    {
        static GraphNode Extra(string id, string kind, int? days = null)
        {
            var node = Indicator(id, BlockKind.MarketFilter, kind, days: days);
            return node with { Parameters = node.Parameters.WithObject("of", new { series = "extra", instrumentId = Gold }) };
        }

        return Graph(
            [Extra("m-gold", "price"), Extra("m-gold-sma", "sma", 50), Compare("m-above", BlockKind.MarketFilter, "gt")],
            [Link("m-gold", "m-above", "left"), Link("m-gold-sma", "m-above", "right"), Link("m-above", "result-market", "condition")],
            NodeParameters.Empty.With("investWhenUnfavorable", 0.5));
    }

    private static StrategyGraph RiskOnlyLimits()
    {
        var graph = DraftTemplates.GraphOf(DraftTemplates.IndexHold);
        var risk = graph.ResultOf(BlockKind.Risk) with { Parameters = NodeParameters.Empty.With("stopLossFromCost", -0.1).With("drawdownFromPeak", -0.2) };

        return graph.WithNode(risk);
    }

    private static GraphNode Signal(string id, string signalId, string state, string scope) =>
        new(new NodeId(id), NodeKind.Signal, BlockKind.Entry, NodeParameters.Empty.With("signalId", signalId).With("state", state).With("scope", scope));
}

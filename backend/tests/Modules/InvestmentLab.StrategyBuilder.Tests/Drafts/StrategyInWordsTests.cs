using System.Text.Json;
using System.Text.RegularExpressions;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed partial class StrategyInWordsTests
{
    private static readonly InstrumentLabels Labels = new(new Dictionary<Guid, string> { [Cash] = "IB01", [Benchmark] = "SPY" });

    private static IReadOnlyList<BlockWords> Words(StrategyGraph graph) => StrategyInWords.Of(Of(graph), EngineSchema.Vocabulary, Labels);

    private static JsonElement ParametersOf(string token) => JsonDocument.Parse(token[(token.IndexOf(' ') + 1)..]).RootElement;

    [Fact]
    public void The_market_filter_is_one_sentence_with_its_share_and_cash_fund()
    {
        var market = Words(DraftTemplates.GraphOf(DraftTemplates.MomentumTrendFilter)).First(words => words.Block == BlockKind.MarketFilter);

        market.Sentence.ShouldStartWith("@builder.words.market ");
        ParametersOf(market.Sentence).GetProperty("share").GetDouble().ShouldBe(0.3);
        ParametersOf(market.Sentence).GetProperty("cash").GetString().ShouldBe("IB01");
    }

    [Fact]
    public void Nested_conditions_become_nested_messages()
    {
        var signal = new GraphNode(new NodeId("sig"), NodeKind.Signal, BlockKind.Entry,
            NodeParameters.Empty.With("signalId", Guid.CreateVersion7().ToString()).With("state", "active").With("scope", "anyInstrument"));
        var graph = Graph(
            [Indicator("mom", BlockKind.Entry, "blendedMomentum"), Compare("positive", BlockKind.Entry, "gt", 0), signal,
             Logic("no", BlockKind.Entry, NodeKind.Not), Logic("and", BlockKind.Entry, NodeKind.All)],
            [Link("mom", "positive", "left"), Link("sig", "no", "condition"), Link("positive", "and", "conditions"), Link("no", "and", "conditions"),
             Link("and", "result-entry", "condition")]);

        var entry = Words(graph).Single(words => words.Block == BlockKind.Entry);

        var condition = ParametersOf(entry.Sentence).GetProperty("condition").GetString()!;
        condition.ShouldStartWith("@builder.words.all ");
        ParametersOf(condition).GetProperty("items").EnumerateArray().Select(item => item.GetString()!).ShouldContain(item => item.StartsWith("@builder.words.not"));
    }

    [Fact]
    public void Empty_blocks_say_nothing() =>
        Words(DraftTemplates.GraphOf(DraftTemplates.IndexHold)).Select(words => words.Block).ShouldBe([BlockKind.Selection]);

    [Fact]
    public void Every_builder_code_the_sentences_use_exists_in_the_catalog()
    {
        var catalog = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(EngineSchema.CatalogPath))!;
        var sentences = DraftTemplates.Codes.SelectMany(code => Words(DraftTemplates.GraphOf(code))).Select(words => words.Sentence);

        var codes = sentences.SelectMany(sentence => BuilderCode().Matches(sentence).Select(match => match.Groups[1].Value)).Distinct();

        codes.Where(code => !catalog.ContainsKey(code)).ShouldBeEmpty();
    }

    [GeneratedRegex(@"@(builder\.[a-z_.]+[a-z])")]
    private static partial Regex BuilderCode();
}

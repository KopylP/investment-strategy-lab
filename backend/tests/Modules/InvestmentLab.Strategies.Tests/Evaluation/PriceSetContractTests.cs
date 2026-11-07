using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.Shared;
using InvestmentLab.Strategies.Evaluation.Infrastructure;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;

namespace InvestmentLab.Strategies.Tests.Evaluation;

public sealed class PriceSetContractTests
{
    private static readonly DateOnly Day = new(2026, 10, 28);
    private static readonly Guid Fund = Guid.CreateVersion7();

    [Fact]
    public async Task A_call_naming_a_price_set_sends_its_id_and_window_instead_of_series()
    {
        var engine = new Recording(HttpStatusCode.NotFound, """{"detail":"price_set.unknown"}""");
        var context = ContextOf() with { PriceSetId = "set-1", HistoryFrom = Day.AddDays(-760) };

        var decided = await EngineOf(engine).DecideAsync("rule-based", context, CancellationToken.None);

        var sent = engine.Body!;
        sent["priceSetId"]!.GetValue<string>().ShouldBe("set-1");
        sent["historyFrom"]!.GetValue<string>().ShouldBe("2024-09-28");
        sent["marketSeries"]!.AsArray().ShouldBeEmpty();
        decided.Error.Code.ShouldBe(StrategyErrorCodes.PriceSetUnknown);
    }

    [Fact]
    public async Task A_full_call_does_not_mention_a_price_set()
    {
        var engine = new Recording(HttpStatusCode.UnprocessableEntity, """{"detail":"bad"}""");

        var decided = await EngineOf(engine).DecideAsync("rule-based", ContextOf(), CancellationToken.None);

        engine.Body!.AsObject().ContainsKey("priceSetId").ShouldBeFalse();
        decided.Error.Code.ShouldBe("strategy.engine_rejected");
    }

    [Fact]
    public async Task An_uploaded_price_set_carries_the_series_the_engine_reads()
    {
        var engine = new Recording(HttpStatusCode.Created, """{"id":"set-7"}""");
        var points = new[] { new MarketPoint(Day, 10m, 9m, 40m) };
        MarketSeries[] series = [new(Fund, SeriesRole.Instrument, points)];

        var uploaded = await EngineOf(engine).UploadPriceSetAsync("rule-based", series, CancellationToken.None);

        uploaded.Value.ShouldBe("set-7");
        engine.Path.ShouldBe("/v1/strategies/rule-based/price-sets");
        var first = engine.Body!["marketSeries"]![0]!;
        first["key"]!.GetValue<string>().ShouldBe(Fund.ToString());
        first["role"]!.GetValue<string>().ShouldBe("instrument");
        first["adjustedClose"]![0]!.GetValue<decimal>().ShouldBe(9m);
        first["valuationClose"]![0]!.GetValue<decimal>().ShouldBe(40m);
    }

    private static PythonStrategyEngine EngineOf(Recording engine) =>
        new(new StrategyEngineClient(new HttpClient(engine) { BaseAddress = new Uri("http://engine") }));

    private static StrategyEvaluationContext ContextOf() => new(
        AsOf: Day,
        Parameters: "{}",
        Universe: [new UniverseMember(Fund, "Fund", null)],
        CashInstrumentId: null,
        MarketSeries: [],
        SubPortfolio: new SubPortfolioStateDto("EUR", 0m, []),
        Contribution: 0m);

    private sealed class Recording(HttpStatusCode status, string answer) : HttpMessageHandler
    {
        public JsonNode? Body { get; private set; }
        public string? Path { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath;
            Body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));

            return new HttpResponseMessage(status) { Content = new StringContent(answer, Encoding.UTF8, "application/json") };
        }
    }
}

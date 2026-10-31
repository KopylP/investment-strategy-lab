using System.Net;
using System.Text;
using System.Text.Json;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;

namespace InvestmentLab.Strategies.Tests.Evaluation;

public sealed class DecisionContractTests
{
    private static readonly Guid Ko = Guid.CreateVersion7();

    private sealed class Answer(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }

    private static async Task<StrategyDecisionDto> Decide(string body)
    {
        var client = new StrategyEngineClient(new HttpClient(new Answer(body)) { BaseAddress = new Uri("http://engine") });
        var context = new EngineContext(new DateOnly(2026, 10, 28), JsonDocument.Parse("{}").RootElement, [], null, [], new EngineSubPortfolio("EUR", 0m, 0m, []));
        var (answer, _, _) = await client.PostContextAsync("value", "decide", context, CancellationToken.None);

        return answer!.Deserialize<StrategyDecisionDto>(StrategyEngineClient.Json)!;
    }

    [Fact]
    public async Task A_strategy_without_momentum_metrics_sends_its_own_columns_and_values()
    {
        var decision = await Decide($$"""
            {"strategyCode":"value","engineVersion":"value/1","asOf":"2026-10-28","investFraction":1,
             "targets":[{"key":"{{Ko}}","weight":1}],
             "market":{"isFavorable":true,"benchmarkPrice":null,"benchmarkMovingAverage":null,"volatilityIndex":null,"reasons":[]},
             "rationale":{"summary":"Обрано 1","title":"Чому саме ці акції","instrumentLabel":"Акція","riskFreeRate":null,
               "columns":[{"id":"yield","label":"Дохідність","format":"percent","hint":null,"compact":true,"trueHint":null,"falseHint":null}],
               "scores":[{"key":"{{Ko}}","name":"Coca-Cola","group":"Staples","status":"selected","reason":"найвища дохідність","rank":1,
                          "momentum":null,"weight":1,"price":null,"values":{"yield":0.031,"sector":"Staples"},
                          "return1m":null,"return3m":null,"return12m":null,"volatility":null,"sharpe":null,"drawdown":null,
                          "aboveMovingAverage":null,"lastDate":null}]},
             "state":null,"warnings":[]}
            """);

        decision.Targets.ShouldHaveSingleItem().InstrumentId.ShouldBe(Ko);
        decision.Rationale.Title.ShouldBe("Чому саме ці акції");
        decision.Rationale.Columns.ShouldHaveSingleItem().Id.ShouldBe("yield");
        var score = decision.Rationale.Scores.ShouldHaveSingleItem();
        score.InstrumentId.ShouldBe(Ko);
        score.AboveMovingAverage.ShouldBeNull();
        score.Values!["yield"].GetDecimal().ShouldBe(0.031m);
        score.Values!["sector"].GetString().ShouldBe("Staples");
    }
}

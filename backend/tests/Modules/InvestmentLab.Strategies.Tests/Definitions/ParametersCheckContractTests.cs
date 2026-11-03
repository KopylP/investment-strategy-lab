using System.Net;
using System.Text;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Infrastructure;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;

namespace InvestmentLab.Strategies.Tests.Definitions;

public sealed class ParametersCheckContractTests
{
    private sealed class Answer(string body) : HttpMessageHandler
    {
        public string? Path { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private sealed class Silence : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("connection refused");
    }

    private static EngineParameterChecks Checks(HttpMessageHandler handler) =>
        new(new StrategyEngineClient(new HttpClient(handler) { BaseAddress = new Uri("http://engine") }));

    [Fact]
    public async Task The_engine_problems_arrive_with_their_paths_and_codes()
    {
        var answer = new Answer("""
            {"problems":[{"path":["entry","condition",0],"code":"rule_based.check.indicator_unknown","message":"@rule_based.check.indicator_unknown {\"name\":\"x\"}"}],"signalIds":[]}
            """);

        var check = (await Checks(answer).CheckAsync("rule-based", "{}", CancellationToken.None)).Value;

        answer.Path.ShouldBe("/v1/strategies/rule-based/parameters/check");
        var problem = check.Problems.ShouldHaveSingleItem();
        problem.Path.ShouldBe("entry.condition.0");
        problem.Code.ShouldBe("rule_based.check.indicator_unknown");
        problem.Message.ShouldStartWith("@rule_based.check.indicator_unknown");
        check.IsClean.ShouldBeFalse();
    }

    [Fact]
    public async Task The_signals_the_parameters_name_are_kept()
    {
        var signal = Guid.CreateVersion7();
        var answer = new Answer($$"""{"problems":[],"signalIds":["{{signal}}"]}""");

        var check = (await Checks(answer).CheckAsync("rule-based", "{}", CancellationToken.None)).Value;

        check.SignalIds.ShouldBe([signal]);
        check.IsClean.ShouldBeTrue();
    }

    [Fact]
    public async Task An_engine_that_does_not_answer_refuses_the_setup()
    {
        var check = await Checks(new Silence()).CheckAsync("rule-based", "{}", CancellationToken.None);

        check.Error.Code.ShouldBe(StrategyErrorCodes.EngineUnavailable);
    }
}

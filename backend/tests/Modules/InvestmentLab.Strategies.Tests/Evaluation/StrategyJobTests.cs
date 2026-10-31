using System.Net;
using System.Text;
using System.Text.Json;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.Shared;
using InvestmentLab.Strategies.Evaluation.Infrastructure;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;

namespace InvestmentLab.Strategies.Tests.Evaluation;

public sealed class StrategyJobTests
{
    private static readonly Guid Ko = Guid.CreateVersion7();

    private sealed class Answer(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? Path { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath;

            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private static PythonStrategyEngine Engine(Answer answer) =>
        new(new StrategyEngineClient(new HttpClient(answer) { BaseAddress = new Uri("http://engine") }));

    private static StrategyEvaluationContext Context() => new(
        new DateOnly(2026, 10, 28), "{}", [], null, [], new SubPortfolioStateDto("EUR", 0m, []), 0m);

    [Fact]
    public async Task A_submitted_job_is_known_by_the_id_the_engine_gives()
    {
        var answer = new Answer(HttpStatusCode.Accepted, """{"jobId":"abc","code":"agent","action":"risk","status":"running","result":null,"error":null}""");

        var jobId = await Engine(answer).SubmitAsync("agent", StrategyTask.AssessRisk, Context(), CancellationToken.None);

        jobId.Value.ShouldBe("abc");
        answer.Path.ShouldBe("/v1/strategies/agent/risk/jobs");
    }

    [Fact]
    public async Task A_refused_context_fails_the_submission()
    {
        var answer = new Answer(HttpStatusCode.UnprocessableEntity, """{"detail":"no benchmark"}""");

        var jobId = await Engine(answer).SubmitAsync("agent", StrategyTask.Decide, Context(), CancellationToken.None);

        jobId.Error.Code.ShouldBe("strategy.engine_rejected");
    }

    [Fact]
    public async Task A_running_job_is_polled_again_later()
    {
        var answer = new Answer(HttpStatusCode.OK, """{"jobId":"abc","code":"agent","action":"decide","status":"running","result":null,"error":null}""");

        var poll = await Engine(answer).PollAsync<StrategyDecisionDto>("abc", CancellationToken.None);

        poll.State.ShouldBe(StrategyJobState.Running);
        poll.Result.ShouldBeNull();
        answer.Path.ShouldBe("/v1/jobs/abc");
    }

    [Fact]
    public async Task A_finished_job_carries_the_decision_with_instrument_ids()
    {
        var answer = new Answer(HttpStatusCode.OK, $$$"""
            {"jobId":"abc","code":"agent","action":"decide","status":"succeeded","error":null,
             "result":{"strategyCode":"agent","engineVersion":"agent/1","asOf":"2026-10-28","investFraction":1,
               "targets":[{"key":"{{{Ko}}}","weight":1}],
               "market":{"isFavorable":true,"benchmarkPrice":null,"benchmarkMovingAverage":null,"volatilityIndex":null,"reasons":[]},
               "rationale":{"summary":"Обрано 1","riskFreeRate":null,"scores":[]},"state":null,"warnings":[]}}
            """);

        var poll = await Engine(answer).PollAsync<StrategyDecisionDto>("abc", CancellationToken.None);

        poll.State.ShouldBe(StrategyJobState.Finished);
        poll.Result!.Value.Targets.ShouldHaveSingleItem().InstrumentId.ShouldBe(Ko);
    }

    [Fact]
    public async Task A_failed_job_says_why()
    {
        var answer = new Answer(HttpStatusCode.OK, """
            {"jobId":"abc","code":"agent","action":"decide","status":"failed","result":null,"error":{"statusCode":500,"detail":"RuntimeError: model timed out"}}
            """);

        var poll = await Engine(answer).PollAsync<StrategyDecisionDto>("abc", CancellationToken.None);

        poll.State.ShouldBe(StrategyJobState.Finished);
        poll.Result!.Error.Code.ShouldBe("strategy.failed");
        poll.Result.Error.Message.ShouldContain("model timed out");
    }

    [Fact]
    public async Task A_job_the_engine_no_longer_knows_is_lost()
    {
        var poll = await Engine(new Answer(HttpStatusCode.NotFound, """{"detail":"Unknown job"}""")).PollAsync<StrategyDecisionDto>("abc", CancellationToken.None);

        poll.State.ShouldBe(StrategyJobState.Lost);
    }
}

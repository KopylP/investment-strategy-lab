using System.Text.Json;
using System.Text.Json.Nodes;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Evaluation.Application;
using InvestmentLab.Strategies.Evaluation.Application.EvaluateStrategy;
using InvestmentLab.Strategies.Shared.Infrastructure;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Strategies.Evaluation.Infrastructure;

/// <summary>ACL to the Python engine: our context → its v1 context, its answer → our DTOs.</summary>
internal sealed class PythonStrategyEngine(StrategyEngineClient client) : IStrategyEngine
{
    public Task<Result<StrategyDecisionDto>> DecideAsync(string typeCode, StrategyEvaluationContext context, CancellationToken cancellationToken) =>
        PostAsync<StrategyDecisionDto>(typeCode, "decide", context, cancellationToken);

    public Task<Result<RiskAssessmentDto>> AssessRiskAsync(string typeCode, StrategyEvaluationContext context, CancellationToken cancellationToken) =>
        PostAsync<RiskAssessmentDto>(typeCode, "risk", context, cancellationToken);

    public async Task<Result<string>> SubmitAsync(string typeCode, StrategyTask task, StrategyEvaluationContext context, CancellationToken cancellationToken)
    {
        try
        {
            var (jobId, statusCode, error) = await client.SubmitJobAsync(typeCode, ActionOf(task), ToEngineContext(context), cancellationToken);
            if (jobId is null)
                return statusCode is 404 or 422 ? EvaluationErrors.EngineRejected(error ?? "") : EvaluationErrors.EngineUnavailable($"{statusCode}: {error}");

            return jobId;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return EvaluationErrors.EngineUnavailable(exception.Message);
        }
    }

    public async Task<EngineJobPoll<T>> PollAsync<T>(string jobId, CancellationToken cancellationToken) where T : class
    {
        EngineJob? job;
        try
        {
            job = await client.GetJobAsync(jobId, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            // The engine may be restarting: the job is either still there or will be reported lost.
            return new EngineJobPoll<T>(StrategyJobState.Running);
        }

        return job switch
        {
            null => new EngineJobPoll<T>(StrategyJobState.Lost),
            { Status: "running" } => new EngineJobPoll<T>(StrategyJobState.Running),
            { Status: "succeeded", Result: { } result } => new EngineJobPoll<T>(StrategyJobState.Finished, result.Deserialize<T>(StrategyEngineClient.Json)!),
            _ => new EngineJobPoll<T>(StrategyJobState.Finished, job.ErrorStatusCode is 422
                ? EvaluationErrors.EngineRejected(job.ErrorDetail ?? "")
                : EvaluationErrors.StrategyFailed(job.ErrorDetail ?? job.Status)),
        };
    }

    private static string ActionOf(StrategyTask task) => task == StrategyTask.AssessRisk ? "risk" : "decide";

    internal static EngineContext ToEngineContext(StrategyEvaluationContext context) => new(
        context.AsOf,
        JsonDocument.Parse(context.Parameters).RootElement,
        [.. context.Universe.Select(member => new EngineUniverseMember(member.InstrumentId.ToString(), member.Name, member.Group))],
        context.CashInstrumentId?.ToString(),
        [.. context.MarketSeries.Select(series => new EngineSeries(
            KeyOf(series),
            series.Role switch
            {
                SeriesRole.Benchmark => "benchmark",
                SeriesRole.VolatilityIndex => "volatilityIndex",
                _ => "instrument",
            },
            [.. series.Points.Select(point => point.Date)],
            [.. series.Points.Select(point => (decimal?)point.AdjustedClose)],
            [.. series.Points.Select(point => (decimal?)point.Close)]))],
        new EngineSubPortfolio(
            context.SubPortfolio.Currency,
            Math.Max(0m, context.SubPortfolio.Cash),
            Math.Max(0m, context.Contribution),
            [.. context.SubPortfolio.Holdings
                .Where(holding => holding.Lots.Any(lot => lot.Quantity > 0m))
                .Select(holding => new EngineHolding(
                    holding.InstrumentId.ToString(),
                    [.. holding.Lots.Where(lot => lot.Quantity > 0m && lot.UnitCost > 0m)
                        .Select(lot => new EngineLot(lot.Quantity, lot.UnitCost, lot.AcquiredOn))]))]));

    // The same instrument can be both in the universe and the benchmark (SPY in the US strategy); keys must stay unique.
    private static string KeyOf(MarketSeries series) => series.Role switch
    {
        SeriesRole.Benchmark => $"{series.InstrumentId}:benchmark",
        SeriesRole.VolatilityIndex => $"{series.InstrumentId}:volatility",
        _ => series.InstrumentId.ToString(),
    };

    private async Task<Result<T>> PostAsync<T>(string typeCode, string action, StrategyEvaluationContext context, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var (answer, statusCode, error) = await client.PostContextAsync(typeCode, action, ToEngineContext(context), cancellationToken);
            if (answer is null)
                return statusCode is 404 or 422 ? EvaluationErrors.EngineRejected(error ?? "") : EvaluationErrors.EngineUnavailable($"{statusCode}: {error}");

            return answer.Deserialize<T>(StrategyEngineClient.Json)!;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return EvaluationErrors.EngineUnavailable(exception.Message);
        }
    }
}

internal sealed class StrategyReader(StrategiesDbContext db) : IStrategyReader
{
    public Task<Strategy?> FindAsync(StrategyId strategyId, CancellationToken cancellationToken) =>
        db.Strategies.AsNoTracking().SingleOrDefaultAsync(strategy => strategy.Id == strategyId, cancellationToken);
}

internal sealed class StrategyEvaluation(EvaluateStrategyHandler evaluate) : IStrategyEvaluation
{
    public async Task<StrategyEvaluationResult<StrategyDecisionDto>> EvaluateAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
        ToContract(await evaluate.Decide(request, cancellationToken));

    public async Task<StrategyEvaluationResult<RiskAssessmentDto>> AssessRiskAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
        ToContract(await evaluate.AssessRisk(request, cancellationToken));

    public async Task<StrategyEvaluationResult<StrategyJobTicket>> SubmitAsync(StrategyTask task, EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
        ToContract(await evaluate.Submit(task, request, cancellationToken));

    public async Task<StrategyJobPoll<StrategyDecisionDto>> PollDecisionAsync(StrategyJobTicket ticket, CancellationToken cancellationToken) =>
        ToContract(await evaluate.PollDecision(ticket, cancellationToken));

    public async Task<StrategyJobPoll<RiskAssessmentDto>> PollRiskAsync(StrategyJobTicket ticket, CancellationToken cancellationToken) =>
        ToContract(await evaluate.PollRisk(ticket, cancellationToken));

    private static StrategyJobPoll<T> ToContract<T>(EngineJobPoll<T> poll) where T : class =>
        new(poll.State, poll.Result is { } result ? ToContract(result) : null);

    private static StrategyEvaluationResult<T> ToContract<T>(Result<T> result) where T : class =>
        result.IsSuccess ? new(result.Value) : new(null, result.Error.Code, result.Error.Message);
}

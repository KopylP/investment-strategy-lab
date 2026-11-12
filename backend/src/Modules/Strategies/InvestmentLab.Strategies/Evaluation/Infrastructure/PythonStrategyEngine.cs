using System.Text.Json;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.Shared;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;
using Microsoft.AspNetCore.Http;

namespace InvestmentLab.Strategies.Evaluation.Infrastructure;

internal sealed class PythonStrategyEngine(StrategyEngineClient client) : IStrategyEngine
{
    private const string UnknownPriceSet = "price_set.unknown";

    public Task<Result<StrategyDecisionDto>> DecideAsync(string typeCode, StrategyEvaluationContext context, CancellationToken cancellationToken) =>
        PostAsync<StrategyDecisionDto>(typeCode, "decide", context, cancellationToken);

    public Task<Result<RiskAssessmentDto>> AssessRiskAsync(string typeCode, StrategyEvaluationContext context, CancellationToken cancellationToken) =>
        PostAsync<RiskAssessmentDto>(typeCode, "risk", context, cancellationToken);

    public async Task<Result<string>> UploadPriceSetAsync(string typeCode, IReadOnlyList<MarketSeries> series, CancellationToken cancellationToken)
    {
        try
        {
            var engineSeries = series.Select(EngineSeriesOf).ToList();
            var (id, statusCode, error) = await client.PostPriceSetAsync(typeCode, engineSeries, cancellationToken);
            if (id is null)
                return EvaluationErrors.EngineUnavailable($"{statusCode}: {error}");

            return id;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return EvaluationErrors.EngineUnavailable(exception.Message);
        }
    }

    public async Task<Result<string>> SubmitAsync(
        string typeCode,
        StrategyTask task,
        StrategyEvaluationContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var (jobId, statusCode, error) = await client.SubmitJobAsync(typeCode, ActionOf(task), ToEngineContext(context), cancellationToken);

            if (jobId is null)
                return FailureOf(statusCode, error);

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
            return new EngineJobPoll<T>(StrategyJobState.Running);
        }

        return job switch
        {
            null => new EngineJobPoll<T>(StrategyJobState.Lost),
            { Status: "running" } => new EngineJobPoll<T>(StrategyJobState.Running),
            { Status: "succeeded", Result: { } result } =>
                new EngineJobPoll<T>(StrategyJobState.Finished, result.Deserialize<T>(StrategyEngineClient.Json)!),
            _ => new EngineJobPoll<T>(StrategyJobState.Finished, job.ErrorStatusCode is 422
                ? EvaluationErrors.EngineRejected(job.ErrorDetail ?? "")
                : EvaluationErrors.StrategyFailed(job.ErrorDetail ?? job.Status)),
        };
    }

    private static Error FailureOf(int statusCode, string? error)
    {
        if (statusCode == StatusCodes.Status404NotFound && error == UnknownPriceSet)
            return EvaluationErrors.PriceSetUnknown();

        return statusCode is StatusCodes.Status404NotFound or StatusCodes.Status422UnprocessableEntity
            ? EvaluationErrors.EngineRejected(error ?? "")
            : EvaluationErrors.EngineUnavailable($"{statusCode}: {error}");
    }

    private static string ActionOf(StrategyTask task) => task == StrategyTask.AssessRisk ? "risk" : "decide";

    internal static EngineContext ToEngineContext(StrategyEvaluationContext context) => new(
        context.AsOf,
        JsonDocument.Parse(context.Parameters).RootElement,
        [.. context.Universe.Select(member => new EngineUniverseMember(member.InstrumentId.ToString(), member.Name, member.Group))],
        context.CashInstrumentId?.ToString(),
        [.. context.MarketSeries.Select(EngineSeriesOf)],
        new EngineSubPortfolio(
            context.SubPortfolio.Currency,
            Math.Max(0m, context.SubPortfolio.Cash),
            Math.Max(0m, context.Contribution),
            [.. context.SubPortfolio.Holdings
                .Where(holding => holding.Lots.Any(lot => lot.Quantity > 0m))
                .Select(holding => new EngineHolding(
                    holding.InstrumentId.ToString(),
                    [.. holding.Lots.Where(lot => lot.Quantity > 0m && lot.UnitCost > 0m)
                        .Select(lot => new EngineLot(lot.Quantity, lot.UnitCost, lot.AcquiredOn))]))]),
        SignalsOf(context),
        context.PriceSetId,
        context.HistoryFrom);

    private static EngineSeries EngineSeriesOf(MarketSeries series) => new(
        KeyOf(series),
        series.Role switch
        {
            SeriesRole.Benchmark => "benchmark",
            SeriesRole.VolatilityIndex => "volatilityIndex",
            SeriesRole.Extra => "extra",
            _ => "instrument",
        },
        [.. series.Points.Select(point => point.Date)],
        [.. series.Points.Select(point => (decimal?)point.AdjustedClose)],
        [.. series.Points.Select(point => (decimal?)point.Close)],
        series.Points.Any(point => point.ValuationClose is not null) ? [.. series.Points.Select(point => point.ValuationClose)] : null);

    private static IReadOnlyList<EngineSignalState>? SignalsOf(StrategyEvaluationContext context)
    {
        if (context.Signals is not { Count: > 0 } signals)
            return null;

        return [.. signals.Select(ToEngineSignalState)];
    }

    private static EngineSignalState ToEngineSignalState(InvestmentLab.Strategies.Evaluation.Domain.SignalState state)
    {
        var activeFor = state.ActiveFor.Select(id => id.ToString()).ToList();

        return new EngineSignalState(state.SignalId.ToString(), activeFor);
    }

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
                return FailureOf(statusCode, error);

            return answer.Deserialize<T>(StrategyEngineClient.Json)!;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return EvaluationErrors.EngineUnavailable(exception.Message);
        }
    }
}

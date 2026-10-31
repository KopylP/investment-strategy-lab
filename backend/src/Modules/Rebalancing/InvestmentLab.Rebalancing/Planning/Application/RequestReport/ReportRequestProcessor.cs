using System.Text.Json;
using InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;
using InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application.RequestReport;

public sealed class ReportRequestProcessor(
    IReportRequestRepository requests,
    IStrategyEvaluation strategies,
    GenerateMonthlyReportHandler rebalance,
    MonthlyPlanPreparation rebalancePreparation,
    GenerateRiskReviewHandler riskReview,
    RiskReviewStrategyRequests riskReviewRequests,
    TimeProvider clock)
{
    public const int MaxSubmissions = 5;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task AdvanceAsync(ReportRequest request, CancellationToken cancellationToken)
    {
        if (!request.IsPending)
            return;

        if (!request.RunsPlanned || request.Runs.Any(run => run is { IsDone: false, Job: null }))
        {
            var asked = await StrategyRequestsAsync(request, cancellationToken);
            if (asked.IsFailure)
            {
                Fail(request, asked.Error.Message);
                await requests.SaveChangesAsync(cancellationToken);

                return;
            }

            request.PlanRuns(asked.Value.Keys);
            await SubmitAsync(request, asked.Value, cancellationToken);
        }

        await PollAsync(request, cancellationToken);
        request.TimeOutWhenDue(clock.GetUtcNow());

        if (request.IsReady)
        {
            var made = await GenerateAsync(request, cancellationToken);
            if (made.IsSuccess)
                return;

            Fail(request, made.Error.Message);
        }

        await requests.SaveChangesAsync(cancellationToken);
    }

    private void Fail(ReportRequest request, string error)
    {
        request.Fail(error, clock.GetUtcNow());
    }

    private async Task SubmitAsync(ReportRequest request, IReadOnlyDictionary<Guid, EvaluateStrategyRequest> asked, CancellationToken cancellationToken)
    {
        var task = TaskOf(request);

        foreach (var run in request.Runs.Where(run => run is { IsDone: false, Job: null }).ToList())
        {
            if (!asked.TryGetValue(run.SubPortfolioId, out var ask))
            {
                request.RunFailed(run.SubPortfolioId, "strategy.not_asked", "під-портфель більше не входить у звіт");
                continue;
            }

            if (run.Submissions >= MaxSubmissions)
            {
                request.RunFailed(run.SubPortfolioId, "strategy.job_lost", "рушій стратегій кілька разів втратив її задачу");
                continue;
            }

            var submitted = await strategies.SubmitAsync(task, ask, cancellationToken);
            if (submitted.IsSuccess)
            {
                var ticket = submitted.Value!;
                request.Submitted(run.SubPortfolioId, new StrategyJob(ticket.JobId, ticket.Task.ToString(), ticket.StrategyId, ticket.StrategyVersion, ticket.CashInstrumentId));
            }
            else if (submitted.ErrorCode != StrategyErrorCodes.EngineUnavailable)
                request.RunFailed(run.SubPortfolioId, submitted.ErrorCode!, submitted.ErrorMessage ?? submitted.ErrorCode!);
        }
    }

    private async Task PollAsync(ReportRequest request, CancellationToken cancellationToken)
    {
        foreach (var run in request.Runs.Where(run => run is { IsDone: false, Job: not null }).ToList())
        {
            var job = run.Job!;
            var ticket = new StrategyJobTicket(job.JobId, Enum.Parse<StrategyTask>(job.Task), job.StrategyId, job.StrategyVersion, job.CashInstrumentId);
            var (state, answer, errorCode, errorMessage) = ticket.Task == StrategyTask.AssessRisk
                ? Read(await strategies.PollRiskAsync(ticket, cancellationToken))
                : Read(await strategies.PollDecisionAsync(ticket, cancellationToken));

            switch (state)
            {
                case StrategyJobState.Lost:
                    request.JobLost(run.SubPortfolioId);
                    break;
                case StrategyJobState.Finished when answer is not null:
                    request.Answered(run.SubPortfolioId, answer);
                    break;
                case StrategyJobState.Finished:
                    request.RunFailed(run.SubPortfolioId, errorCode ?? "strategy.failed", errorMessage ?? "стратегія не відповіла");
                    break;
            }
        }
    }

    private static (StrategyJobState State, string? Answer, string? ErrorCode, string? ErrorMessage) Read<T>(StrategyJobPoll<T> poll) where T : class =>
        poll.Result switch
        {
            { IsSuccess: true, Value: { } value } => (poll.State, JsonSerializer.Serialize(value, Json), null, null),
            { } failed => (poll.State, null, failed.ErrorCode, failed.ErrorMessage),
            null => (poll.State, null, null, null),
        };

    private Task<Result<IReadOnlyDictionary<Guid, EvaluateStrategyRequest>>> StrategyRequestsAsync(ReportRequest request, CancellationToken cancellationToken) =>
        request.Kind == ReportKind.RiskReview
            ? riskReviewRequests.BuildAsync(RiskReviewOf(request), cancellationToken)
            : rebalancePreparation.StrategyRequestsAsync(RebalanceOf(request), cancellationToken);

    private async Task<Result<ReportId>> GenerateAsync(ReportRequest request, CancellationToken cancellationToken)
    {
        var made = request.Kind == ReportKind.RiskReview
            ? await riskReview.Handle(RiskReviewOf(request), AnswersOf<RiskAssessmentDto>(request), request, cancellationToken)
            : await rebalance.Handle(RebalanceOf(request), AnswersOf<StrategyDecisionDto>(request), request, cancellationToken);
        return made;
    }

    private static Dictionary<Guid, StrategyEvaluationResult<T>> AnswersOf<T>(ReportRequest request) where T : class =>
        request.Runs.ToDictionary(
            run => run.SubPortfolioId,
            run => run.Answer is { } answer
                ? new StrategyEvaluationResult<T>(JsonSerializer.Deserialize<T>(answer, Json))
                : new StrategyEvaluationResult<T>(null, run.ErrorCode ?? "strategy.no_answer", run.ErrorMessage ?? "стратегія не відповіла"));

    private static StrategyTask TaskOf(ReportRequest request) => request.Kind == ReportKind.RiskReview ? StrategyTask.AssessRisk : StrategyTask.Decide;

    private static ReportScope ScopeOf(ReportRequest request) => new(request.Code, request.Title, request.SubPortfolioIds.ToHashSet());

    private static GenerateMonthlyReportCommand RebalanceOf(ReportRequest request) => new(request.AccountId, request.Contribution, request.AsOf, ScopeOf(request));

    private static GenerateRiskReviewCommand RiskReviewOf(ReportRequest request) => new(request.AccountId, request.AsOf, ScopeOf(request));
}

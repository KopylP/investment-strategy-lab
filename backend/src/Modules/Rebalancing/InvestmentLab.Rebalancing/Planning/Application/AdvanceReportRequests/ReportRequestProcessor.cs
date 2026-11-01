using InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;
using InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;
using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.AdvanceReportRequests;

public sealed class ReportRequestProcessor(
    IReportRequestRepository requests,
    IStrategyRuns strategies,
    GenerateMonthlyReportHandler rebalance,
    MonthlyPlanPreparation rebalancePreparation,
    GenerateRiskReviewHandler riskReview,
    RiskReviewStrategyRequests riskReviewRequests,
    TimeProvider clock)
{
    public const int MaxSubmissions = 5;

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

    private async Task SubmitAsync(
        ReportRequest request,
        IReadOnlyDictionary<Guid, StrategyAsk> asked,
        CancellationToken cancellationToken)
    {
        foreach (var run in request.Runs.Where(run => run is { IsDone: false, Job: null }).ToList())
        {
            if (!asked.TryGetValue(run.SubPortfolioId, out var ask))
            {
                request.RunFailed(run.SubPortfolioId, "strategy.not_asked", Message.Of("report.run.not_asked"));
                continue;
            }

            if (run.Submissions >= MaxSubmissions)
            {
                request.RunFailed(run.SubPortfolioId, "strategy.job_lost", Message.Of("report.run.job_lost"));
                continue;
            }

            var submitted = await strategies.SubmitAsync(request.Kind, ask, cancellationToken);
            if (submitted.Job is { } job)
                request.Submitted(run.SubPortfolioId, job);
            else if (!submitted.EngineUnavailable)
                request.RunFailed(run.SubPortfolioId, submitted.ErrorCode!, submitted.ErrorMessage!);
        }
    }

    private async Task PollAsync(ReportRequest request, CancellationToken cancellationToken)
    {
        foreach (var run in request.Runs.Where(run => run is { IsDone: false, Job: not null }).ToList())
        {
            var polled = await strategies.PollAsync(run.Job!, cancellationToken);

            switch (polled.State)
            {
                case StrategyRunState.Lost:
                    request.JobLost(run.SubPortfolioId);
                    break;
                case StrategyRunState.Finished when polled.Answer is not null:
                    request.Answered(run.SubPortfolioId, polled.Answer);
                    break;
                case StrategyRunState.Finished:
                    request.RunFailed(run.SubPortfolioId, polled.ErrorCode ?? "strategy.failed", polled.ErrorMessage ?? Message.Of("report.run.no_answer"));
                    break;
            }
        }
    }

    private Task<Result<IReadOnlyDictionary<Guid, StrategyAsk>>> StrategyRequestsAsync(
        ReportRequest request,
        CancellationToken cancellationToken) =>
        request.Kind == ReportKind.RiskReview
            ? riskReviewRequests.BuildAsync(RiskReviewOf(request), cancellationToken)
            : rebalancePreparation.StrategyRequestsAsync(RebalanceOf(request), cancellationToken);

    private async Task<Result<ReportId>> GenerateAsync(ReportRequest request, CancellationToken cancellationToken)
    {
        var made = request.Kind == ReportKind.RiskReview
            ? await riskReview.Handle(RiskReviewOf(request), AnswersOf(request, strategies.RiskIn), request, cancellationToken)
            : await rebalance.Handle(RebalanceOf(request), AnswersOf(request, strategies.DecisionIn), request, cancellationToken);

        return made;
    }

    private static Dictionary<Guid, StrategyAnswer<T>> AnswersOf<T>(ReportRequest request, Func<string, T> read) where T : class =>
        request.Runs.ToDictionary(run => run.SubPortfolioId, run => AnswerOf(run, read));

    private static StrategyAnswer<T> AnswerOf<T>(StrategyRun run, Func<string, T> read) where T : class =>
        run.Answer is { } answer
            ? StrategyAnswer<T>.Given(read(answer))
            : StrategyAnswer<T>.Failed(run.ErrorMessage ?? Message.Of("report.run.no_answer"));

    private static ReportScope ScopeOf(ReportRequest request) => new(request.Code, request.Title, request.SubPortfolioIds.ToHashSet());

    private static GenerateMonthlyReportCommand RebalanceOf(ReportRequest request) =>
        new(request.AccountId, ScopeOf(request), request.Contribution, request.AsOf);

    private static GenerateRiskReviewCommand RiskReviewOf(ReportRequest request) => new(request.AccountId, ScopeOf(request), request.AsOf);
}

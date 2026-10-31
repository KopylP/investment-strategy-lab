using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.RequestReport;

public sealed record RequestReportCommand(
    Guid AccountId, string? Code = null, decimal? Contribution = null, DateOnly? AsOf = null, IReadOnlySet<Guid>? SubPortfolioIds = null, ReportCause? Cause = null);

public sealed class RequestReportHandler(PlanningInputs inputs, IReportRequestRepository requests, TimeProvider clock)
{
    public async Task<Result<ReportRequestId>> Handle(RequestReportCommand command, ReportTrigger trigger, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");

        var reports = await inputs.ReportsAsync(account, settings: null, cancellationToken);
        var report = command.Code is { } code
            ? reports.FirstOrDefault(each => each.Code == code)
            : reports.FirstOrDefault(each => each.Kind == ReportKind.Monthly);
        if (report is null)
            return command.Code is null
                ? ReportErrors.NoActiveSubPortfolios(account.Id)
                : Error.NotFound("report.definition_not_found", $"No strategy of account {account.Id} makes the report '{command.Code}'.");
        if (command.Contribution is < 0m)
            return ReportErrors.ContributionNegative();

        var subPortfolios = report.SubPortfolios.Select(sub => sub.SubPortfolioId)
            .Where(id => command.SubPortfolioIds?.Contains(id) ?? true)
            .ToList();
        if (subPortfolios.Count == 0)
            return ReportErrors.NoActiveSubPortfolios(account.Id);

        if (await requests.PendingAsync(account.Id, report.Code, cancellationToken) is { } pending)
            return pending.Id;

        var now = clock.GetUtcNow();
        var request = ReportRequest.Create(
            account.Id, UserId.Of(account.OwnerId), report.Code, report.Name, report.Kind, trigger,
            report.Kind == ReportKind.Monthly ? command.Contribution : null,
            command.AsOf ?? DateOnly.FromDateTime(now.UtcDateTime), subPortfolios, now, command.Cause);
        requests.Add(request);
        await requests.SaveChangesAsync(cancellationToken);

        return request.Id;
    }
}

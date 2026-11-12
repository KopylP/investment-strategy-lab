using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.RequestReport;

public sealed record RequestReportCommand(
    Guid AccountId, string? Code =
        null, decimal? Contribution = null, DateOnly? AsOf = null, IReadOnlySet<Guid>? SubPortfolioIds = null, ReportCauseDto? Cause = null);

public sealed record ReportCauseDto(Guid SignalTriggerId, string Text);

public sealed class RequestReportHandler(IAccountSetupQueries accounts, IAccountReports accountReports, IReportRequestRepository requests, TimeProvider clock)
{
    public async Task<Result<Guid>> Handle(RequestReportCommand command, string trigger, CancellationToken cancellationToken)
    {
        var reportTrigger = EnumText.TryParse<ReportTrigger>(trigger, "report.trigger_unknown");
        if (reportTrigger.IsFailure)
            return reportTrigger.Error;

        if (await accounts.GetAccountSetup(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");

        var reports = await accountReports.OfAsync(account.Id, settings: null, cancellationToken);
        if (AccountReport.Choose(reports, command.Code) is not { } report)
            return ReportErrors.NoSuchReport(account.Id, command.Code);

        if (command.Contribution is < 0m)
            return ReportErrors.ContributionNegative();

        var subPortfolios = report.SubPortfoliosWithin(command.SubPortfolioIds);
        if (subPortfolios.Count == 0)
            return ReportErrors.NoActiveSubPortfolios(account.Id);

        if (await requests.PendingAsync(account.Id, report.Code, cancellationToken) is { } pending)
            return pending.Id.Value;

        var now = clock.GetUtcNow();
        var request = ReportRequest.Create(
            account.Id, UserId.Of(account.OwnerId), report.Code, report.Name, report.Kind, reportTrigger.Value,
            report.ContributionOf(command.Contribution),
            command.AsOf ?? DateOnly.FromDateTime(now.UtcDateTime), subPortfolios, now, CauseOf(command.Cause));
        requests.Add(request);
        await requests.SaveChangesAsync(cancellationToken);

        return request.Id.Value;
    }

    private static ReportCause? CauseOf(ReportCauseDto? cause) => cause is null ? null : new ReportCause(cause.SignalTriggerId, cause.Text);
}

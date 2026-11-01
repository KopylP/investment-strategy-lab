using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Signals.Contracts;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Rebalancing.Planning.Application.Integration;

internal sealed class RequestReportsOnSignal(
    ISignalReportRuleRepository rules,
    IReportRepository reports,
    IReportRequestRepository requests,
    IAccountSetupQueries accounts,
    IAccountReports accountReports,
    RequestReportHandler requestReport,
    ILogger<RequestReportsOnSignal> logger) : IIntegrationEventHandler<SignalTriggeredV1>
{
    private const decimal NoNewMoney = 0m;

    public async Task Handle(SignalTriggeredV1 triggered, CancellationToken cancellationToken)
    {
        var attached = await rules.OfOwnedSignalAsync(triggered.SignalId, triggered.OwnerId, cancellationToken);
        if (attached.Count == 0)
            return;

        foreach (var rule in attached)
            await ActOnceAsync(rule, triggered, cancellationToken);

        await rules.SaveChangesAsync(cancellationToken);
    }

    private async Task ActOnceAsync(SignalReportRule rule, SignalTriggeredV1 triggered, CancellationToken cancellationToken)
    {
        var alreadyAsked = await requests.AnsweredSignalAsync(triggered.TriggerId, rule.AccountId, rule.ReportCode, cancellationToken);
        if (!alreadyAsked)
            await ActAsync(rule, triggered, cancellationToken);
    }

    private async Task ActAsync(SignalReportRule rule, SignalTriggeredV1 triggered, CancellationToken cancellationToken)
    {
        var account = await accounts.GetAccountSetup(rule.AccountId, cancellationToken);
        if (account is null)
        {
            rule.CouldNotAct(Message.Of("signal_rule.account_gone"));

            return;
        }

        var declared = await accountReports.OfAsync(account.Id, settings: null, cancellationToken);
        var report = declared.FirstOrDefault(each => each.Code == rule.ReportCode);
        if (report is null)
        {
            rule.CouldNotAct(Message.Of("signal_rule.report_gone", new { account = account.Name }));

            return;
        }

        var today = DateOnly.FromDateTime(ReportSchedules.Local(triggered.TriggeredAt).DateTime);
        if (await IsMonthlyReportOpenAsync(report, account.Id, today, cancellationToken))
        {
            rule.Acted();

            return;
        }

        await RequestAsync(rule, report, account.Id, today, triggered, cancellationToken);
    }

    private async Task<bool> IsMonthlyReportOpenAsync(AccountReport report, Guid accountId, DateOnly today, CancellationToken cancellationToken)
    {
        if (report.Kind != ReportKind.Monthly)
            return false;

        var ofMonth = await reports.ForPeriodAsync(accountId, YearMonth.Containing(today), cancellationToken);

        return ofMonth.Any(each => each.Code == report.Code && each.IsOpen);
    }

    private async Task RequestAsync(
        SignalReportRule rule, AccountReport report, Guid accountId, DateOnly today, SignalTriggeredV1 triggered, CancellationToken cancellationToken)
    {
        var command = new RequestReportCommand(
            AccountId: accountId,
            Code: report.Code,
            Contribution: report.Kind == ReportKind.Monthly ? NoNewMoney : null,
            AsOf: today,
            Cause: CauseOf(triggered));

        var requested = await requestReport.Handle(command, ReportTrigger.Signal, cancellationToken);
        if (requested.IsSuccess)
        {
            rule.Acted();

            return;
        }

        logger.LogWarning("Signal {SignalId} could not request report {Code}: {Error}", triggered.SignalId, report.Code, requested.Error.Message);
        rule.CouldNotAct(Message.Of("signal_rule.no_sub_portfolios"));
    }

    private static ReportCause CauseOf(SignalTriggeredV1 triggered)
    {
        var subjects = triggered.Subjects.Select(subject => subject.Text).ToList();

        return new ReportCause(triggered.TriggerId, Message.Of("report.cause.signal", new { signal = triggered.Name, subjects }));
    }
}

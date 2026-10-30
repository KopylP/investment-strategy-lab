using InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;
using InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateReport;

/// <param name="Code">One of the account's reports (<see cref="PlanningInputs.ReportsAsync"/>); null — its first rebalance report.</param>
/// <param name="Contribution">Rebalance reports only; null — the scope's part of the plan's monthly contribution.</param>
/// <param name="SubPortfolioIds">Null: every sub-portfolio whose strategy makes the report.</param>
public sealed record GenerateReportCommand(
    Guid AccountId, string? Code = null, decimal? Contribution = null, DateOnly? AsOf = null, IReadOnlySet<Guid>? SubPortfolioIds = null);

/// <summary>Makes one of the reports the account's strategies declare, the way its mode says: on demand and on schedule alike.</summary>
public sealed class GenerateReportHandler(PlanningInputs inputs, GenerateMonthlyReportHandler rebalance, GenerateRiskReviewHandler riskReview)
{
    public async Task<Result<ReportId>> Handle(GenerateReportCommand command, ReportTrigger trigger, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");

        var reports = await inputs.ReportsAsync(account, cancellationToken);
        var report = command.Code is { } code
            ? reports.FirstOrDefault(each => each.Code == code)
            : reports.FirstOrDefault(each => each.Kind == ReportKind.Monthly);
        if (report is null)
            return command.Code is null
                ? ReportErrors.NoActiveSubPortfolios(account.Id)
                : Error.NotFound("report.definition_not_found", $"No strategy of account {account.Id} makes the report '{command.Code}'.");

        var subPortfolios = report.SubPortfolios.Select(sub => sub.SubPortfolioId)
            .Where(id => command.SubPortfolioIds?.Contains(id) ?? true)
            .ToHashSet();
        var scope = new ReportScope(report.Code, report.Name, subPortfolios);
        return report.Kind == ReportKind.RiskReview
            ? await riskReview.Handle(new GenerateRiskReviewCommand(account.Id, command.AsOf, scope), trigger, cancellationToken)
            : await rebalance.Handle(new GenerateMonthlyReportCommand(account.Id, command.Contribution, command.AsOf, scope), trigger, cancellationToken);
    }
}

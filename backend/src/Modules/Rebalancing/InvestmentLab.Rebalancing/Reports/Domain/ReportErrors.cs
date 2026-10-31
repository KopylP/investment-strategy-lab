namespace InvestmentLab.Rebalancing.Reports.Domain;

public static class ReportErrors
{
    public static Error NoSuchReport(Guid accountId, string? code) =>
        code is null
            ? NoActiveSubPortfolios(accountId)
            : Error.NotFound("report.definition_not_found", $"No strategy of account {accountId} makes the report '{code}'.");

    public static Error NotFound(Guid reportId) => Error.NotFound("report.not_found", $"Report {reportId} does not exist.");

    public static Error ActionNotFound(Guid actionId) => Error.NotFound("report.action_not_found", $"The report has no action {actionId}.");

    public static Error ReportIsNotOpen(Guid reportId, ReportStatus status) =>
        Error.Conflict("report.not_open", $"Report {reportId} is {status}: actions can no longer be marked.");

    public static Error ActionAlreadyResolved(Guid actionId, ActionStatus status) =>
        Error.Conflict("report.action_resolved", $"Action {actionId} is already {status}; undo the mark first.");

    public static Error OnlyPendingCanBeSkipped(Guid actionId, ActionStatus status) =>
        Error.Conflict("report.action_not_pending", $"Action {actionId} is {status}: only a pending action can be skipped.");

    public static Error NothingToUndo(Guid actionId) => Error.Conflict("report.nothing_to_undo", $"Action {actionId} has no mark to undo.");

    public static Error PartialFillNotSupported(ActionKind kind) =>
        Error.Validation("report.partial_fill_not_supported", $"A {kind} action is marked as a whole; change the amount instead.");

    public static Error FillQuantityNotPositive() => Error.Validation("report.fill_quantity_not_positive", "Quantity must be above zero.");

    public static Error FillPriceNotPositive() => Error.Validation("report.fill_price_not_positive", "Price must be above zero.");

    public static Error FillFeeNegative() => Error.Validation("report.fill_fee_negative", "Fee cannot be negative.");

    public static Error NoActiveSubPortfolios(Guid accountId) =>
        Error.Validation("report.no_sub_portfolios", $"Account {accountId} has no active sub-portfolio with a strategy.");

    public static Error NoStrategyAnswered(string details) =>
        Error.Validation("report.no_strategy_answered", $"No strategy could evaluate the account (refresh quotes?): {details}");

    public static Error ContributionNegative() => Error.Validation("report.contribution_negative", "Contribution cannot be negative.");
}

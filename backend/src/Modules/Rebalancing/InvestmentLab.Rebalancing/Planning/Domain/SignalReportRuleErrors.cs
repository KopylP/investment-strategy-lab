namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class SignalReportRuleErrors
{
    public static Error AccountNotFound() =>
        Error.NotFound("signal_report_rule.account", "The account does not exist.");

    public static Error ReportNotDeclared() =>
        Error.NotFound("signal_report_rule.report", "The strategies of the account do not make this report.");

    public static Error AlreadyExists() =>
        Error.Conflict("signal_report_rule.exists", "This signal already starts this report on the account.");

    public static Error NotFound() =>
        Error.NotFound("signal_report_rule.not_found", "The rule does not exist.");
}

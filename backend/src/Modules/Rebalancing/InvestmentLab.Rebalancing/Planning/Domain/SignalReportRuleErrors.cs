namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class SignalReportRuleErrors
{
    public static Error AccountNotFound() =>
        Error.NotFound("signal_report_rule.account", "Такого рахунку немає.");

    public static Error ReportNotDeclared() =>
        Error.NotFound("signal_report_rule.report", "Стратегії цього рахунку не складають такого рапорту.");

    public static Error AlreadyExists() =>
        Error.Conflict("signal_report_rule.exists", "Цей сигнал уже запускає такий рапорт на цьому рахунку.");

    public static Error NotFound() =>
        Error.NotFound("signal_report_rule.not_found", "Такого правила немає.");
}

namespace InvestmentLab.Rebalancing.Reports.Domain;

public static class ReportDefaults
{
    public static string CodeOf(ReportKind kind) => kind == ReportKind.RiskReview ? "risk-review" : "rebalance";

    public static string TitleOf(ReportKind kind) => kind == ReportKind.RiskReview ? "Перевірка ризику" : "Ребаланс";
}

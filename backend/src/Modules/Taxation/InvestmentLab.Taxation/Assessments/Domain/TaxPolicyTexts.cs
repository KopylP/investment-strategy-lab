namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record TaxPolicyTexts(
    string Name,
    string Hint,
    string? Summary = null,
    string? EstimateNote = null,
    string? TaxedAccountsLabel = null,
    string? OverviewHint = null,
    string? ProgressNote = null,
    string? LossesNote = null);

namespace InvestmentLab.Rebalancing.Reviews.Domain;

public enum ReviewVerdict
{
    Agree,
    AgreeWithRemarks,
    Disagree,
}

public enum FindingSeverity
{
    Info,
    Warning,
    Critical,
}

public sealed record ReviewFinding(FindingSeverity Severity, string Message);

/// <summary>A second opinion on a plan (reserved for the AI reviewer, D10). Never changes the plan by itself.</summary>
public sealed record PlanReview(string Reviewer, ReviewVerdict Verdict, string Summary, IReadOnlyList<ReviewFinding> Findings);

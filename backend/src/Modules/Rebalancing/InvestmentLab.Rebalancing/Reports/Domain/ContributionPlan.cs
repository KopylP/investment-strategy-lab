namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record ContributionPlan(decimal Total, string Split, IReadOnlyList<ContributionPart> Parts, TransferHint? Transfer)
{
    public static ContributionPlan None => new(0m, "None", [], null);
}

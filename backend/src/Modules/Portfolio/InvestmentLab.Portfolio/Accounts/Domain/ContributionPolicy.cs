namespace InvestmentLab.Portfolio.Accounts.Domain;

public sealed record ContributionPolicy(ContributionPolicyKind Kind, Percentage DriftBand)
{
    public static readonly Percentage DefaultDriftBand = Percentage.FromPercent(5m);

    public static ContributionPolicy Default => new(ContributionPolicyKind.FillUnderweight, DefaultDriftBand);
}

namespace InvestmentLab.Portfolio.Accounts.Domain;

public sealed record ContributionPolicy(ContributionPolicyKind Kind, Percentage DriftBand)
{
    public static readonly Percentage DefaultDriftBand = Percentage.FromPercent(5m);

    public static ContributionPolicy Default => new(ContributionPolicyKind.FillUnderweight, DefaultDriftBand);

    public static Result<ContributionPolicy> TryOf(string kindName, decimal? driftBandPercent)
    {
        if (!Enum.TryParse<ContributionPolicyKind>(kindName, ignoreCase: true, out var kind) || !Enum.IsDefined(kind))
            return Error.Validation("account.contribution_policy_unknown", $"'{kindName}' is not FillUnderweight or FixedSplit.");

        var band = Percentage.TryFromOptionalPercent(driftBandPercent);
        if (band.IsFailure)
            return band.Error;

        return new ContributionPolicy(kind, band.Value ?? DefaultDriftBand);
    }
}

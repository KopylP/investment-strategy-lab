namespace InvestmentLab.Taxation.Assessments.Domain;

public static class TaxPolicies
{
    public const string Poland = "PL";

    public static ITaxPolicy Default { get; } = new BelkaTaxPolicy();

    public static bool IsKnown(string? code) => Find(code) is not null;

    public static ITaxPolicy For(string? code) =>
        Find(code) ?? throw new ArgumentException($"'{code}' is not a known tax policy.", nameof(code));

    private static ITaxPolicy? Find(string? code) => code?.Trim().ToUpperInvariant() switch
    {
        BelkaTaxPolicy.PolicyCode => Default,
        "PL-IKE" => new NoTaxPolicy("PL-IKE"),
        "PL-IKZE" => new NoTaxPolicy("PL-IKZE"),
        _ => null,
    };
}

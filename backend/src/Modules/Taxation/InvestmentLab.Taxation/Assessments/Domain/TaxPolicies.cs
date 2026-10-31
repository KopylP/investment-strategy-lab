namespace InvestmentLab.Taxation.Assessments.Domain;

public static class TaxPolicies
{
    public const string Poland = "PL";

    public static ITaxPolicy For(string? code) => code?.ToUpperInvariant() switch
    {
        "PL-IKE" or "PL-IKZE" => new NoTaxPolicy(code.ToUpperInvariant()),
        _ => new BelkaTaxPolicy(),
    };
}

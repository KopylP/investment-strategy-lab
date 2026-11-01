using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.ListTaxPolicies;

public sealed record TaxPolicyDto(string Code, string Name, string Hint, bool IsTaxed, bool IsDefault);

public sealed class ListTaxPoliciesHandler
{
    public IReadOnlyList<TaxPolicyDto> Handle() => [.. TaxPolicies.All.Select(ToDto)];

    private static TaxPolicyDto ToDto(ITaxPolicy policy) => new(
        Code: policy.Code,
        Name: policy.Texts.Name,
        Hint: policy.Texts.Hint,
        IsTaxed: policy.IsTaxed,
        IsDefault: policy.Code == TaxPolicies.Default.Code);
}

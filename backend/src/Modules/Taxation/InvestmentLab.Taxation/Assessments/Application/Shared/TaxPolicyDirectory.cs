using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Contracts;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed class TaxPolicyDirectory : ITaxPolicyDirectory
{
    public string DefaultCode => TaxPolicies.Default.Code;

    public bool IsKnown(string code) => TaxPolicies.IsKnown(code);
}

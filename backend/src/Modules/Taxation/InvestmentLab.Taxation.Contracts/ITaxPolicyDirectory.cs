namespace InvestmentLab.Taxation.Contracts;

public interface ITaxPolicyDirectory
{
    string DefaultCode { get; }

    bool IsKnown(string code);
}

namespace InvestmentLab.Taxation.Contracts;

public interface ITaxPolicyDirectory
{
    bool IsKnown(string code);
}

namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IForeignCash
{
    string Currency { get; }
    decimal Amount { get; }
}

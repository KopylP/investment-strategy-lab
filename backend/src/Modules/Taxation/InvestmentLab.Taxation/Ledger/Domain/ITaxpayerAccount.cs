namespace InvestmentLab.Taxation.Ledger.Domain;

public interface ITaxpayerAccount
{
    Guid Id { get; }
    UserId OwnerId { get; }
    string Name { get; }
    string Broker { get; }
    string Currency { get; }
    string PolicyCode { get; }
}

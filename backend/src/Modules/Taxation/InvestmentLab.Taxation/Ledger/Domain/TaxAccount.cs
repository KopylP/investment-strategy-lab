namespace InvestmentLab.Taxation.Ledger.Domain;

public sealed class TaxAccount : IUserOwned
{
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public string Currency { get; private set; }
    public string PolicyCode { get; private set; }

    private TaxAccount(Guid accountId, UserId ownerId, string currency, string policyCode) =>
        (AccountId, OwnerId, Currency, PolicyCode) = (accountId, ownerId, currency, policyCode);

    private TaxAccount() => (Currency, PolicyCode) = (null!, null!);

    public static TaxAccount Of(Guid accountId, UserId ownerId, string currency, string policyCode) => new(accountId, ownerId, currency, policyCode);

    public void Update(string currency, string policyCode) => (Currency, PolicyCode) = (currency, policyCode);
}

namespace InvestmentLab.Taxation.Ledger.Domain;

public interface ITaxpayerAccounts
{
    Task<ITaxpayerAccount?> FindAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ITaxpayerAccount>> OfOwnerAsync(UserId ownerId, CancellationToken cancellationToken);
}
